var _bt_n = 0;
var _bt_weap = -4;
if (is_player() && (Slashing_Damage + Piercing_Damage + Blunt_Damage + Rending_Damage) > 0)
{
    _bt_weap = scr_get_weapon_id_equipped();
}
if (_bt_weap != -4)
{
    var _bt_heal_recv = Healing_Received;
    with (_bt_weap)
    {
        var _bt_val = ds_map_find_value(data, "Bloodthirst");
        if (!__is_undefined(_bt_val))
        {
            var _bt_hp = math_round(_bt_val * 0.01 * argument1 * _bt_heal_recv / 100);
            var _bt_gap = ds_map_find_value(data, "MaxDuration") - ds_map_find_value(data, "Duration");
            var _bt_repair = min(_bt_hp, _bt_gap);
            if (_bt_repair > 0)
            {
                ds_map_replace(data, "Duration", ds_map_find_value(data, "Duration") + _bt_repair);
            }
            _bt_n = _bt_hp - _bt_repair;
        }
    }
    if (_bt_n > 0)
    {
        var _bt_k = 1;
        if (argument3)
        {
            _bt_k = CRTD / 100;
        }
        var _bt_resist = 0;
        if (variable_instance_exists(argument0, "tmpUnholy_Resistance"))
        {
            _bt_resist = clamp(argument0.tmpUnholy_Resistance / 100, 0, 1);
        }
        var _bt_recv = 1;
        if (variable_instance_exists(argument0, "Damage_Received"))
        {
            _bt_recv = clamp(argument0.Damage_Received / 100, 0, 2);
        }
        var _bt_conv = _bt_n * _bt_k * (1 - _bt_resist) * _bt_recv;
        _bt_n = 0;
        if (_bt_conv > 0)
        {
            _bt_n = max(1, ceil(_bt_conv));
        }
        if ((_bt_n > 0) && instance_exists(argument0))
        {
            scr_damage_info(argument0, string(_bt_n), 0, make_colour_rgb(230, 44, 40));
        }
    }
}
