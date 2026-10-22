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
| 核心效果 | 攻击命中时按伤害比例汲取血量，修复该武器耐久（1 血 = 1 耐久） |
| 溢出效果 | 武器耐久已满时，溢出部分转化为额外邪术伤害，向上取整、最低 1 |
| 数值 | 普通附魔 7%，诅咒附魔 13%|
| 稀有度 / 池子 | 与原版 Lifesteal 一致，位于rare池 |
| 触发范围 | 玩家手持带该附魔的武器且本次为物理伤害：普攻、武器主动伤害技能、反击。（法术 / DOT / 反伤 / 陷阱不触发） |
| 获取途径 | 通过附魔卷轴抽取、通过战利品或商店直接取得带饮血附魔的武器 |
| 语言 | 中文 / English |

---

## 两种发行形态

本模组有两种实现，**只能同时启用一个**：

| | 对外版（external） | 内部版（internal） |
| --- | --- | --- |
| 版本 | `v1.0.3-release` | `v1.2.2` |
| 注入点 | 4 处（词条池 / 数值 / `scr_damage` 内联 / 显示名） | 3 处（词条池 / 数值 / 显示名） |
| 依赖 | 仅 MSLE | MSLE + `BloodThirstPatcher` 后处理 |
| 伤害呈现 | 溢出邪术伤害为**红色数字飘字** | 并入原版战斗日志（例如："X 点伤害（X 点劈砍 + Y 点邪术）"） |

对外版之所以独立，是因为内测版需要直接改写 `data.win` 的 `scr_damage_calculation`（MSLE 的 GML 再编译链路在该函数上存在缺陷），普通玩家无法执行这一步，因此对外版改用 `scr_damage` 内联注入。

---

## 下载与安装（推荐普通玩家）

1. 在 [Releases](../../releases) 页面下载 `BloodThirsty-v1.0.3.zip`；
2. 解压得到 `v1.0.3-release.sml`；
3. 放入 `Stoneshard\Stoneshard\tools\ModShardLauncher-Enhanced\Mods`；
4. 选择data.win，记得备份
5. 打开 MSLE，勾选该模组，点击编译；
6. 启动游戏。

注意：

- 请确保 **MSLE 的 Mods 目录中只启用一个饮血模组**（内测版与对外版不要同时启用）。
- 若之前使用过内部版并运行过 `BloodThirstPatcher`，请先把 `data.win.bak` 还原为 `data.win` 再编译本版本，避免重复结算。

---


## 内部版与 patcher

内部版的伤害结算依赖 `BloodThirstPatcher` 对 `data.win` 的后处理注入，源码、可执行文件与完整使用说明见 [internal/README.md](internal/README.md)。

---

## 兼容性说明

- 仅在 0.9.4.25-vm 上验证；其他版本锚点可能失配。
- 饮血与 生命吸取 共用稀有度与池子，因此**所有原版可附魔的武器**（近战、弓弩、锤棒、长杖等）都可能生成该词条。
- 兼容Speedshard-MoneyDungeon：https://github.com/remyCases/SpeedshardMoneyDungeon

---

## 第三方组件

- [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool) 0.8.3.0（GPL-3.0）——`BloodThirstPatcher` 依赖其 `UndertaleModLib` 与 `Underanalyzer` 完成反编译与 code import。
- [ModShardLauncher](https://github.com/ModShardTeam/ModShardLauncher) —— 模组编译与注入框架。

## 致谢

- 卓越的ai模型：GLM5.3Flash 和 DeepSeek-v4.1Flash-1017
