// ============================================================================
// BloodThirstPatcher - 饮血附魔 damage_calculation 后处理注入器 (v1.0.2)
// ----------------------------------------------------------------------------
// v1.0.1：溢出邪术伤害向上取整、最低 1（ceil + max(1,·)，无溢出仍为 0）
// v1.0.2：关闭游戏内运行时日志（[HIT] 写文件逻辑移除；本工具自身的 [PATCHER]
//         操作日志保留，仅在打补丁时写入）
// ----------------------------------------------------------------------------
// 作者：ErenSanchez  |  TargetVersion: 0.9.4.25-vm  |  基于 UTMT 0.8.3.0
// ----------------------------------------------------------------------------
// 用途：MSLE 编译全部模组生成 data.win 之后运行本工具，把饮血的伤害结算
//       逻辑注入 gml_GlobalScript_scr_damage_calculation（0.6.1.0 的
//       round-trip 缺陷导致 MSLE 链路无法注入该 entry，故用 0.8.3.0 后处理）。
//       注入后战斗日志显示原版同款格式：X点伤害（X点劈砍 + Y点邪术）。
// ----------------------------------------------------------------------------
// 用法：BloodThirstPatcher [data.win路径]
//   - 不传参数时默认处理 D:\steam\steamapps\common\Stoneshard\data.win（支持双击运行，
//     或把 data.win 拖到 exe 图标上）；命令行/脚本调用传参数覆盖路径
//   - 自动备份原文件为 <data.win>.bak（每次实际注入前覆盖；幂等跳过时不动备份）
//   - 幂等：检测到 scr_bloodthirst_hit 已注入则跳过
//   - 双击运行时退出前停顿显示结果；命令行调用（带参数）不停顿
//   - 工具操作日志：追加到 %LOCALAPPDATA%\StoneShard\Bloodthirst\bt_log.txt
//     （游戏内运行时日志已于 v1.0.2 关闭）
// ============================================================================

using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using Underanalyzer.Decompiler;

const string CodeName = "gml_GlobalScript_scr_damage_calculation";
const string HitMarker = "scr_bloodthirst_hit";
const string Anchor = "var _damage = math_round(_slashing + _piercing + _blunt + _rending + _magic_damage + _nature_damage);";
const string CallLines =
    "var _bt_n = scr_bloodthirst_hit(_damage, _unholy, _interUnholyDmg, _AllDamage);\n" +
    "_damage += _bt_n;\n" +
    "_unholy += _bt_n;";
const string HitFunction =
    "function scr_bloodthirst_hit(_bt_dmg, _bt_unholy, _bt_inter, _bt_all)\n" +
    "{\n" +
    "    var _bt_extra = 0\n" +
    "    var _bt_weap = -4\n" +
    "    if (is_player() && (Slashing_Damage + Piercing_Damage + Blunt_Damage + Rending_Damage) > 0)\n" +
    "    {\n" +
    "        _bt_weap = scr_get_weapon_id_equipped()\n" +
    "    }\n" +
    "    if (_bt_weap != -4)\n" +
    "    {\n" +
    "        var _bt_heal_recv = Healing_Received\n" +
    "        with (_bt_weap)\n" +
    "        {\n" +
    "            var _bt_val = ds_map_find_value(data, \"Bloodthirst\")\n" +
    "            if (!__is_undefined(_bt_val))\n" +
    "            {\n" +
    "                var _bt_hp = math_round(_bt_val * 0.01 * _bt_dmg * _bt_heal_recv / 100)\n" +
    "                var _bt_gap = ds_map_find_value(data, \"MaxDuration\") - ds_map_find_value(data, \"Duration\")\n" +
    "                var _bt_repair = min(_bt_hp, _bt_gap)\n" +
    "                if (_bt_repair > 0)\n" +
    "                {\n" +
    "                    ds_map_replace(data, \"Duration\", ds_map_find_value(data, \"Duration\") + _bt_repair)\n" +
    "                }\n" +
    "                _bt_extra = _bt_hp - _bt_repair\n" +
    "                if (_bt_extra > 0)\n" +
    "                {\n" +
    "                    _bt_extra = max(1, ceil(_bt_extra))\n" +
    "                }\n" +
    "            }\n" +
    "        }\n" +
    "    }\n" +
    "    return _bt_extra\n" +
    "}";

// 不传参数时默认处理游戏本体 data.win（支持双击运行）；命令行参数可覆盖路径
string winPath = args.Length >= 1
    ? args[0]
    : @"D:\steam\steamapps\common\Stoneshard\data.win";
if (!File.Exists(winPath))
{
    Console.Error.WriteLine("文件不存在: " + winPath);
    Console.Error.WriteLine(args.Length >= 1 ? "usage: BloodThirstPatcher <data.win路径>" : "如游戏安装在其他位置，请把 data.win 路径作为参数拖到本 exe 上运行");
    Pause();
    return 1;
}

string logDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StoneShard", "Bloodthirst");
Directory.CreateDirectory(logDir);
string logFile = Path.Combine(logDir, "bt_log.txt");
void Log(string line)
{
    try { File.AppendAllText(logFile, "[PATCHER] " + line + Environment.NewLine); } catch { }
    Console.WriteLine("[PATCHER] " + line);
}

// 双击运行时窗口会随进程退出关闭，所有出口前停一下让用户看清结果
void Pause()
{
    if (args.Length >= 1) return; // 命令行调用（脚本/终端）不停顿
    Console.WriteLine();
    Console.WriteLine("按回车键退出...");
    Console.In.ReadLine();
}

Log("target: " + winPath);

// 读取
using var fs = File.OpenRead(winPath);
UndertaleData data;
try { data = UndertaleIO.Read(fs); }
catch (Exception e) { Log("FAIL(read): " + e.Message); Pause(); return 1; }
fs.Close();

var code = data.Code.FirstOrDefault(c => c.Name != null && c.Name.Content == CodeName);
if (code == null) { Log("FAIL(no code) " + CodeName); Pause(); return 1; }

// 反编译当前文本（0.8.3.0 Underanalyzer）
string current;
try
{
    var gctx = new GlobalDecompileContext(data);
    var dctx = new DecompileContext(gctx, code, null);
    current = dctx.DecompileToString();
}
catch (Exception e) { Log("FAIL(decompile): " + e.Message); Pause(); return 1; }

// 幂等检测
if (current.Contains(HitMarker))
{
    Log("SKIP: scr_bloodthirst_hit already injected (idempotent)");
    if (args.Length < 1)
    {
        Console.WriteLine();
        Console.WriteLine("data.win 已注入过饮血，无需重复处理。按回车键退出...");
        Console.In.ReadLine();
    }
    return 0;
}

// 锚点匹配
if (!current.Contains(Anchor))
{
    Log("FAIL(anchor): anchor not found in decompiled text");
    Pause();
    return 1;
}

// 组装新文本：锚点行后插调用行，entry 尾部追加函数定义
int anchorIdx = current.IndexOf(Anchor, StringComparison.Ordinal);
int lineEnd = current.IndexOf('\n', anchorIdx);
if (lineEnd < 0) lineEnd = current.Length;
string newText = current.Substring(0, lineEnd + 1) + CallLines + "\n" + current.Substring(lineEnd + 1);
newText = newText.TrimEnd() + "\n\n" + HitFunction + "\n";

// 导入（0.8.3.0 编译器）
try
{
    var group = new CodeImportGroup(data);
    group.QueueReplace(code, newText);
    group.Import();
}
catch (Exception e)
{
    Log("FAIL(import): " + e.Message.Replace("\r", "").Replace("\n", " | "));
    Pause();
    return 1;
}
Log("import OK");

// 备份并保存
// 备份每次实际注入前都覆盖：MSLE 重新编译后 data.win 已是全新状态，旧备份失效；
// 幂等跳过路径不会走到这里，已注入文件不会被误覆盖
string backup = winPath + ".bak";
File.Copy(winPath, backup, true);
Log("backup: " + backup);
try
{
    using var outFs = File.Create(winPath);
    UndertaleIO.Write(outFs, data);
}
catch (Exception e)
{
    Log("FAIL(write): " + e.Message);
    // 保存失败时从备份还原
    if (File.Exists(backup)) { File.Copy(backup, winPath, true); Log("restored from backup"); }
    Pause();
    return 1;
}
Log("saved: " + winPath);
Log("done. 饮血 damage_calculation 注入完成");
Pause();
return 0;
