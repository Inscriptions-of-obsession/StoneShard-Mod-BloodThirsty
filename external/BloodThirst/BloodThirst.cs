// ============================================================================
// Blood Thirst - 武器附魔「饮血」 v1.0.3-release（External version）
// ----------------------------------------------------------------------------
// 原创 Stoneshard 模组
// 作者：       ErenSanchez
// 版本：       1.0.3-release
// TargetVersion: 0.9.4.25-vm
// 适配运行时： MSL-Enhanced（MSLE）
// ----------------------------------------------------------------------------
// 发行说明：面向普通玩家的独立发行版，只依赖 MSLE 编译，不需要任何额外的
// 后处理工具（BloodThirstPatcher 仅用于开发者内测版）。
// 核心效果沿用 v1.0.2 的 scr_damage 内联注入方案（溢出邪术伤害以红色数字
// 飘字提示）；显示名改用 v1.1.0+ 的 textLoader 确定性注册，彻底修复 v1.0.2
// 的 "Bloodthirst not found" 与 attributeKey[Bloodthirst] 文本错误。
// ----------------------------------------------------------------------------
// 功能：
//   新增武器附魔词条「饮血」：
//   - 攻击命中时按伤害比例汲取血量，修复该武器耐久（1 血 = 1 耐久）
//   - 武器耐久已满时，溢出部分转化为额外邪术伤害（可暴击，走原版法强/
//     格挡/邪术抗性管线），结算后以红色数字飘字提示
//   - 溢出邪术伤害向上取整、最低 1（0.97 → 1，1.11 → 2）
//   - 稀有度与池子与原版 Lifesteal 一致：仅 rare 池单条注册，槽位 "Weapon"，
//     无白名单，附魔行为完全回归原版（卷轴/大附魔卷/战利品/商店自动继承）
//   - 数值：普通附魔 7%，诅咒附魔 13%（独立定值）
//   - 显示名按游戏语言自动切换：武器名前缀"渴血"、词条名"饮血"，
//     英文统一 "Blood thirst"
//   - 运行时日志：关闭（不写任何文件）
// ----------------------------------------------------------------------------
// 注入点（共 4 处，全部为锚点注入，不覆盖原版逻辑）：
//   1. scr_weapon_generation               词条池注册（仅 rare 池单条）
//   2. scr_weapon_generation_prefix_search 数值定值（7 / 13）
//   3. scr_damage                          核心结算（内联代码 + 红色飘字）
//   4. o_textLoader Other_25               显示名注册（prefix_text / attribute /
//                                          attribute_percent，中英自动切换）
// ----------------------------------------------------------------------------
// 设计原则：充分复用原版系统——触发判定交由原版数据流（物理伤害字段>0、
// 武器 equipped 状态），武器识别与磨损同源（scr_get_weapon_id_equipped），
// 汲取基数与原版吸血同口径（减免后实际伤害）。
// ============================================================================

using ModShardLauncher;
using ModShardLauncher.Mods;

namespace BloodThirst;
public class BloodThirst : Mod
{
    public override string Author => "ErenSanchez";
    public override string Name => "Blood Thirst";
    public override string Description => "New weapon enchantment: Bloodthirst. Lifesteal repairs weapon durability; overflow turns into Unholy damage.";
    public override string Version => "1.0.3-release";
    public override string TargetVersion => "0.9.4.25-vm";

    public override void PatchMod()
    {
        EnchantPool();
        ValueGate();
        CoreEffect();
        InitText();
        Log("Mod patched successfully.");
    }

    // 编译期诊断日志：仅记录注入步骤（游戏运行时不写任何文件）
    private static void Log(string line)
    {
        try
        {
            string dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StoneShard", "Bloodthirst");
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(dir, "bt_log.txt"),
                $"[PATCH] {line}{Environment.NewLine}");
        }
        catch { /* 日志失败不影响注入 */ }
    }

    // ========================================================================
    // 1. 词条池注册：scr_weapon_generation
    //    锚点为原版 Lifesteal 注册行；饮血与 Lifesteal 完全同构：
    //    槽位 "Weapon"、仅 rare 池（_prefix=2）单条注册，同稀有度同池。
    // ========================================================================
    private void EnchantPool()
    {
        Msl.LoadGML("gml_GlobalScript_scr_weapon_generation")
            .MatchFrom("scr_weapon_prefix_generation(\"Lifesteal\", \"Weapon\", 2, 5)")
            .InsertBelow(
                ";\r\n" +
                "scr_weapon_prefix_generation(\"Bloodthirst\", \"Weapon\", 2, 7);")
            .Save();
        Log("EnchantPool OK");
    }

    // ========================================================================
    // 2. 数值特判：scr_weapon_generation_prefix_search
    //    饮血不走原版 ceil(基础值×1.3) 诅咒系数与随机段，改用定值：
    //    普通 7 / 诅咒 13。所有生成路径（战利品/商店/附魔卷轴/大附魔卷轴重掷）
    //    都经过本函数，故自动兼容。
    // ========================================================================
    private void ValueGate()
    {
        Msl.LoadGML("gml_GlobalScript_scr_weapon_generation_prefix_search")
            .MatchFrom("var stat_name = ds_map_find_value(argument1, key)")
            .InsertBelow(
                ";\r\n" +
                "if (stat_name == \"Bloodthirst\")\r\n" +
                "{\r\n" +
                "    char = (quality == Curse) ? 13 : 7;\r\n" +
                "}")
            .Save();
        Log("ValueGate OK");
    }

    // ========================================================================
    // 3. 核心结算：scr_damage（沿用 v1.0.2 内联方案）
    //    argument1 = 定稿后实际伤害；argument3 = 是否暴击；argument0 = 目标。
    //    反伤/陷阱/DOT 走 scr_simple_damage 不经过此处 → 天然不触发饮血。
    //    内联代码见 Codes/bt_inline.gml：汲取→修耐久→溢出转邪术（向上取整、
    //    最低 1）→红色飘字；不含任何日志写入。
    // ========================================================================
    private void CoreEffect()
    {
        Msl.LoadGML("gml_GlobalScript_scr_damage")
            .MatchFrom("scr_physical_damage(argument0, argument1, argument2, argument3)")
            .ReplaceBy(
                ";\r\n" +
                ModFiles.GetCode("bt_inline.gml") + "\r\n" +
                "argument1 += _bt_n;\r\n" +
                "scr_physical_damage(argument0, argument1, argument2, argument3);")
            .Save();
        Log("CoreEffect OK");
    }

    // ========================================================================
    // 4. 显示名注册：textLoader_Other_25 的 case 40 末尾（room==0 守卫块之外、
    //    break 之前）。textLoader 按 number 0→40 顺序加载文本表（numberMax=40，
    //    case 40 最后执行），语言切换（仅主菜单）会重建 textLoader 整体重跑：
    //    prefix_text（case 0）与 attribute（case ~5）每次都被 scr_tableWriteMap
    //    清空重建，attribute_percent 仅在首载（room==0）创建。因此注册点必须
    //    在守卫块之外才能同时覆盖首载与语言切换两个场景。写入三个 map：
    //    - global.prefix_text      ：武器名附魔前缀（中文"渴血"= chr(28212)
    //                                + chr(34880)，英文 "Blood thirst"）
    //    - global.attribute        ：hover 词条名（中文"饮血"= chr(39278)
    //                                + chr(34880)，英文 "Blood thirst"）
    //    - global.attribute_percent：词条 % 后缀
    //    该方案在 v1.0.x 的 scr_init_weapon_data 注册（会被 textLoader 重建
    //    清空）之上修复了文本错误。
    //    payload 采用"闭块 + if(true) 重开"手法插到 room==0 块外、break 之前，
    //    借用原有 } 闭合 if(true)，语法完整。MatchFrom 为逐行子串包含匹配，
    //    锚点行 attribute_order_all_without_damage 在全 entry 中唯一。
    // ========================================================================
    private void InitText()
    {
        Msl.LoadGML("gml_Object_o_textLoader_Other_25")
            .MatchFrom("ds_list_add(global.attribute_order_all_without_damage")
            .InsertBelow(
                ";\r\n" +
                "}\r\n" +
                "ds_map_replace(global.attribute, \"Bloodthirst\", (global.language == 3 ? chr(39278) + chr(34880) : \"Blood thirst\"));\r\n" +
                "ds_map_replace(global.attribute_percent, \"Bloodthirst\", \"%\");\r\n" +
                "ds_map_replace(global.prefix_text, \"Bloodthirst\", (global.language == 3 ? chr(28212) + chr(34880) : \"Blood thirst\"));\r\n" +
                "if (true)\r\n" +
                "{")
            .Save();
        Log("InitText OK");
    }
}
