using DaggerfallWorkshop.Game.MagicAndEffects;

namespace Assets.Scripts.Game.MacadaynuMods.HotkeyBar
{
    public class Hotkey
    {
        public int Id { get; set; }

        public HotkeyType Type { get; set; }

        public EffectBundleSettings Spell { get; set; }
    }
}
