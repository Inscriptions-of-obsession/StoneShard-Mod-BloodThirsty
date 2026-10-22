# 内部版（internal）— 需要 BloodThirstPatcher

内测/开发用版本，**不发布 Release**。面向作者本人与能够运行后处理工具的测试者。

- 模组版本：`1.2.2`
- 目标版本：0.9.4.25-vm
- 依赖：MSL-Enhanced + `BloodThirstPatcher`

---

## 为什么需要 patcher

饮血的伤害结算需要写在 `gml_GlobalScript_scr_damage_calculation` 内（与暴击、抗性、战斗日志同一条管线）。MSLE 基于 UndertaleModLib 0.6.1.0，该函数的「反编译 → 再编译」存在库级缺陷（补全分号、大括号、脱离 `self.` 前缀后仍会在语义阶段抛 NullRef），因此无法通过 sml 注入。

`BloodThirstPatcher` 改用 UndertaleModLib 0.8.3.0 的 `CodeImportGroup` 直接改写 `data.win`，注入后：

- 溢出邪术伤害并入 `_damage` / `_unholy`，进入原版战斗日志："X 点伤害（X 点劈砍 + Y 点邪术）"；
- 原版伤害管线负责暴击、格挡、邪术抗性等全部判定。

sml 本体只做 3 处注入：词条池（仅 rare 池）、数值定值（7/13）、显示名（textLoader 注册）。

---

## 完整工作流

1. **关闭游戏**（避免 `data.win` 被占用）；
2. 用 MSLE 编译全部模组（勾选本模组），生成新的 `data.win`；
3. 运行 `BloodThirstPatcher`（见下节）；
4. 启动游戏测试。

每次 MSLE 重新编译后都必须重跑一次 patcher，因为编译会重建 `data.win`，之前的注入会被覆盖。

---

## patcher 使用说明

可执行文件：`dist/BloodThirstPatcher.exe`（自包含单文件，无需安装 .NET 运行时）。

### 方式一：双击运行（推荐）

默认处理 `D:\steam\steamapps\common\Stoneshard\data.win`，完成后窗口会停住显示结果，按回车退出。若游戏装在别处，请使用方式二或三。

### 方式二：把 data.win 拖到 exe 上

### 方式三：命令行

```
BloodThirstPatcher.exe "<data.win 路径>"
```

带参数运行时不停顿，便于脚本调用。

### 行为说明

| 项目 | 说明 |
| --- | --- |
| 备份 | 每次实际注入前把原文件复制为 `data.win.bak`（覆盖旧备份）；幂等跳过时不动备份 |
| 幂等 | 检测到已注入 `scr_bloodthirst_hit` 时直接跳过，重复运行无副作用 |
| 失败还原 | 保存失败会自动从 `.bak` 还原 |
| 日志 | 工具操作日志写入 `%LOCALAPPDATA%\StoneShard\Bloodthirst\bt_log.txt`（`[PATCHER]` 前缀），游戏运行时不写任何日志 |

### 回滚

把 `data.win.bak` 复制回 `data.win` 即可（回滚后需重新用 MSLE 编译模组）。

---

## 从源码构建

### 模组 sml

1. 修改 `BloodThirst/BloodThirst.csproj` 中的 `HintPath`，指向本机 MSLE 目录下的 `ModShardLauncher.dll` 与 `UndertaleModLib.dll`；
2. 用 MSLE 加载源码编译，或用打包工具：

```
PackTool "Blood Thirst" "internal/BloodThirst" "<输出目录>"
```

### patcher

1. 准备 UndertaleModTool **0.8.3.0**，将其 `UndertaleModLib.dll` 与 `Underanalyzer.dll` 放到任意目录；
2. 修改 `BloodThirstPatcher/BloodThirstPatcher.csproj` 中两个 `HintPath`，指向上述 DLL；
3. 构建：

```
dotnet build -c Release
# 或生成自包含单文件
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o dist
```

> `dist/BloodThirstPatcher.exe` 已随仓库提供，仅需源码改动时才重新构建。

---

## 注意

- 内部版与对外版（`external/`）**只能启用一个**。
- 内部版的 `data.win` 一旦被 patcher 改写过，就不能再叠加对外版使用（会出现双重结算）；切换版本前先还原 `data.win.bak`。
