namespace Assets.Scripts.Game.MacadaynuMods.HotkeyBar
{
    public class HotkeySettings
    {
        public int MaxSlots;
        public int MaxRows;
        public int PanelSize;
        public int XAxis;
        public int YAxis;
        public int Spacing;
        public bool DisplayHotkeys;
        public bool InvertScrollDirection;
        public bool UseEquipDelayTimes;

        public HotkeySettings(int maxSlots, int maxRows, int panelSize, int xAxis, int yAxis,
            int spacing, bool displayHotkeys, bool invertScrollDirection, bool useEquipDelayTimes)
        {
            MaxSlots = maxSlots;
            MaxRows = maxRows;
            PanelSize = panelSize;
            XAxis = xAxis;
            YAxis = yAxis;
            Spacing = spacing;
            DisplayHotkeys = displayHotkeys;
            InvertScrollDirection = invertScrollDirection;
            UseEquipDelayTimes = useEquipDelayTimes;
        }
    }
}