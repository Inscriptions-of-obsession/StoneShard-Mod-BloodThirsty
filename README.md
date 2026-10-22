# BloodThirsty — 饮血附魔（Stoneshard）

为 **Stoneshard 0.9.4.25-vm（modbranch）** 新增原创武器附魔词条「饮血」的模组。

- 作者：ErenSanchez
- 目标版本：0.9.4.25-vm
- 运行环境：MSL-Enhanced（MSLE）+ 本仓库对应的发行版

---

## 功能

| 项目 | 说明 |
| --- | --- |
| 词条名 | 中文「饮血」/ 英文 "Blood thirst" |
| 武器名前缀 | 中文「渴血」（例：渴血公会配剑）/ 英文 "Blood thirst Guild Blade" |
| 核心效果 | 攻击命中时按伤害比例汲取血量，修复该武器耐久（1 血 = 1 耐久） |
| 溢出效果 | 武器耐久已满时，溢出部分转化为额外邪术伤害（可暴击，走原版格挡/邪术抗性管线），向上取整、最低 1（0.97 → 1，1.11 → 2） |
| 数值 | 普通附魔 7%，诅咒附魔 13%（独立定值，不走原版 ×1.3 系数） |
| 稀有度 / 池子 | 与原版 Lifesteal 一致：仅 rare 池单条注册，槽位 "Weapon"，无白名单 |
| 触发范围 | 玩家手持带该附魔的武器且本次为物理伤害：普攻、武器主动伤害技能、反击。徒手 / 法术 / DOT / 反伤 / 陷阱不触发（由原版机制保证） |
| 获取途径 | 附魔卷轴、大附魔卷、战利品、商店（全部继承原版词条池） |
| 语言 | 中文 / 英文按游戏语言自动切换 |
| 日志 | 不写任何运行时日志 |

---

## 两种发行形态

本模组有两种实现，**只能同时启用一个**：

| | 对外版（external） | 内部版（internal） |
| --- | --- | --- |
| 版本 | `v1.0.3-release` | `v1.2.2` |
| 注入点 | 4 处（词条池 / 数值 / `scr_damage` 内联 / 显示名） | 3 处（词条池 / 数值 / 显示名） |
| 依赖 | 仅 MSLE | MSLE + `BloodThirstPatcher` 后处理 |
| 伤害呈现 | 溢出邪术伤害为**红色数字飘字** | 并入原版战斗日志（"X 点伤害（X 点劈砍 + Y 点邪术）"） |
| 用途 | 面向普通玩家，Release 附件分发 | 作者内测，**不发布 Release** |

对外版之所以独立，是因为内测版需要直接改写 `data.win` 的 `scr_damage_calculation`（MSLE 的 GML 再编译链路在该函数上存在缺陷），普通玩家无法执行这一步，因此对外版改用 `scr_damage` 内联注入。

---

## 下载与安装（推荐普通玩家）

1. 在 [Releases](../../releases) 页面下载 `BloodThirsty-v1.0.3.zip`；
2. 解压得到 `v1.0.3-release.sml`；
3. 放入 `Stoneshard\Stoneshard\tools\ModShardLauncher-Enhanced\Mods`；
4. 打开 MSLE，勾选该模组，点击编译；
5. 启动游戏。

注意：

- 请确保 **MSLE 的 Mods 目录中只启用一个饮血模组**（内测版与对外版不要同时启用）。
- 若之前使用过内部版并运行过 `BloodThirstPatcher`，请先把 `data.win.bak` 还原为 `data.win` 再编译本版本，避免重复结算。

---

## 仓库结构

```
BloodThirsty/
├─ README.md
├─ .gitignore
├─ external/                     # 对外版（仅 MSLE，Release 分发）
│  └─ BloodThirst/
│     ├─ BloodThirst.cs
│     ├─ BloodThirst.csproj
│     ├─ Codes/bt_inline.gml      # scr_damage 内联结算代码
│     └─ icon.png
└─ internal/                     # 内部版（需要 patcher，不分发 Release）
   ├─ README.md                   # 内部版 + patcher 使用说明
   ├─ BloodThirst/
   │  ├─ BloodThirst.cs
   │  ├─ BloodThirst.csproj
   │  └─ icon.png
   └─ BloodThirstPatcher/
      ├─ BloodThirstPatcher.csproj
      ├─ Program.cs
      └─ dist/BloodThirstPatcher.exe   # 自包含单文件，可直接运行
```

> 构建产物（`.sml`、`data.win`、`bin/`、`obj/`）不纳入版本控制；成品通过 Release 附件分发。

---

## 从源码构建

前置：.NET SDK 8、MSL-Enhanced、原版 `0.9.4.25-vm.win`。

1. 修改 `*.csproj` 中的 `HintPath`，指向你本机的 MSLE 与 UndertaleModLib 安装位置；
2. 用 MSLE 打开 `Mods` 目录，把编译好的 `.sml` 放入并编译；

或使用打包工具：

```
PackTool <sml名> <源码目录> <输出目录>
```

`sml` 文件名即 MSLE 列表中显示的模组名。

---

## 内部版与 patcher

内部版的伤害结算依赖 `BloodThirstPatcher` 对 `data.win` 的后处理注入，源码、可执行文件与完整使用说明见 [internal/README.md](internal/README.md)。

---

## 兼容性说明

- 仅在 0.9.4.25-vm 上验证；其他版本锚点可能失配。
- 饮血与 Lifesteal 共用稀有度与池子，因此**所有原版可附魔的武器**（近战、弓弩、锤棒、长杖等）都可能生成该词条，这是「回归原版」的预期行为。
- 不依赖任何其他模组；大附魔卷重掷走原版同一生成函数，自动兼容。

---

## 版本历史

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| `v1.0.3`（对外） | 2026-10-22 | 首个对外开放版本：仅需 MSLE，红色飘字提示溢出邪术，稀有度与 Lifesteal 一致，关闭运行时日志 |
| `v1.2.2`（内部） | 2026-10-21 | 内测版：patcher 注入 `scr_damage_calculation`，溢出并入原版战斗日志 |
| `v1.2.1` | 2026-10-21 | 修复附魔白名单失效（空语句退化），随后废弃白名单、回归原版 |
| `v1.2.0` | 2026-10-21 | 武器名前缀改为「渴血」，溢出邪术伤害向上取整最低 1 |
| `v1.1.0` | 2026-10-21 | 改用 `textLoader` 注册显示名，修复 "not found" 与 attributeKey 文本错误 |
| `v1.0.0` ~ `v1.0.2` | 2026-10-20 | 首个可用版本及早期修复 |

---

## 第三方组件

- [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool) 0.8.3.0（GPL-3.0）——`BloodThirstPatcher` 依赖其 `UndertaleModLib` 与 `Underanalyzer` 完成反编译与 code import。
- [ModShardLauncher](https://github.com/ModShardTeam/ModShardLauncher) —— 模组编译与注入框架。

## 致谢

- 模组作者：ErenSanchez
