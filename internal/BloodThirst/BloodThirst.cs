// ============================================================================
// Blood Thirst - 武器附魔「饮血」
// ----------------------------------------------------------------------------
// 原创 Stoneshard 模组
// 作者：       ErenSanchez
// 版本：       1.1.0
// TargetVersion: 0.9.4.25-vm
// 适配运行时： MSL-Enhanced（MSLE）
// ----------------------------------------------------------------------------
// 功能：
//   新增近战武器附魔词条「饮血」：
//   - 攻击命中时按伤害比例汲取血量，修复该武器耐久（1 血 = 1 耐久）
//   - 武器耐久已满时，汲取量（含临界溢出部分）转化为额外邪术伤害（可暴击，
//     与武器伤害同一刀结算，走原版法强/格挡/邪术抗性管线）
//   - 可附魔武器：与 Lifesteal 一致，全武器可生成（原版门槛判定，无白名单）
//   - 数值：普通附魔 7%，诅咒附魔 13%（独立定值）
//   - 稀有度：与 Lifesteal 一致，仅 rare 池单条注册
//   - 显示名按游戏语言自动切换（global.language == 3 为中文）
//   - 运行时日志：v1.2.2 起关闭
// ----------------------------------------------------------------------------
// 注入点（v1.2.2 定稿共 3 处，全部为锚点注入，不覆盖原版逻辑）：
//   1. scr_weapon_generation               词条池注册（仅 rare 池单条，与
//                                          Lifesteal 同稀有度同池，槽位
//                                          "Weapon"，附魔行为完全回归原版）
//   2. scr_weapon_generation_prefix_search 数值特判（7/13 定值）
//   3. o_textLoader Other_25               显示名注册（prefix_text / attribute /
//                                          attribute_percent，中英自动切换）
// ----------------------------------------------------------------------------
// v1.2.2：关闭全部运行时日志（sml 的 [ROLL] 与 patcher 的 [HIT] 写文件逻辑
// 移除，编译期 [PATCH] 诊断保留）；稀有度与池子对齐 Lifesteal（仅 rare 池）。
// ----------------------------------------------------------------------------
// v1.2.1 演进记录：
//   - 中文武器名前缀与词条名拆分：prefix_text="渴血"、attribute="饮血"
//   - 溢出邪术伤害向上取整最低 1（patcher v1.0.2）
//   - 废弃 "Melee" 自定义槽位与 data["Slot"] 白名单（v1.0.x~v1.2.0 门槛两次
//     失效：先空语句退化致首抽必中，修复后 data["Slot"] 生成期不存在致全禁），
//     附魔对象与触发范围完全交由原版机制（同 Lifesteal），patcher 端触发判定
//     本就武器无关，远程/锤棒/长杖带饮血行为与原版生命汲取同构
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
    public override string Version => "1.2.2";
    public override string TargetVersion => "0.9.4.25-vm";

    public override void PatchMod()
    {
        EnchantPool();
        ValueGate();
        InitText();
        Log("Mod patched successfully.");
    }

    // 日志：写一行到沙盒目录 Bloodthirst/bt_log.txt
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
    //    普通 7 / 诅咒 13。v1.2.2 起运行时日志关闭。
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
    // 3. 显示名注册：textLoader_Other_25 的 case 40 末尾（room==0 守卫块之外、
    //    break 之前）。textLoader 按 number 0→40 顺序加载文本表（numberMax=40，
    //    case 40 最后执行），语言切换（仅主菜单）会重建 textLoader 整体重跑：
    //    prefix_text（case 0）与 attribute（case ~5）每次都被 scr_tableWriteMap
    //    清空重建，attribute_percent 仅在首载（room==0）创建。因此注册点必须
    //    在守卫块之外才能同时覆盖首载与语言切换两个场景。写入三个 map：
    //    - global.prefix_text      ：武器名附魔前缀（v1.2.0：中文"渴血"= chr(28212)
    //                                + chr(34880)，英文 "Blood thirst" 不变，
    //                                scr_text_exeption(global.prefix_text, key)）
    //    - global.attribute        ：hover 词条名（v1.2.0 保持"饮血"= chr(39278)
    //                                + chr(34880)，scr_hoversGetAttributeName，
    //                                缺失时显示 attributeKey[Bloodthirst]）
    //    - global.attribute_percent：词条 % 后缀（scr_atr_percent）
    //    v1.0.2 的 scr_init_weapon_data 注册在 textLoader 首次加载时被重建清空
    //    （时序缺陷），已废弃；full_atr 无读取方，不再注册。
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
