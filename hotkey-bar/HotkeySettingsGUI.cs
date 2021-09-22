using Assets.Scripts.MyMods;
using UnityEngine;

namespace Assets.Scripts.Game.MacadaynuMods.HotkeyBar
{
    public class HotkeySettingsGUI : MonoBehaviour
    {
        public static HotkeysPanel panel;

        private GUIStyle style;
        private GUIStyle toggleStyle;

        private Texture2D scrollImage;

        public Rect hotkeySettingsControlsRect = new Rect(20, 20, 120, 50);

        int labelY = 35;
        int sliderY = 40;
        int valueY = 33;
        int ySpacing = 35;

        static int maxSlots;
        static int previousmaxSlots;

        static int maxRows;
        static int previousMaxRows;

        static int panelSize;
        static int previousPanelSize;

        static int xAxis;
        static int previousXAxis;

        static int yAxis;
        static int previousYAxis;

        static int spacing;
        static int previousSpacing;

        static bool displayHotkeys;
        static bool previousDisplayHotkeys;

        static bool invertScrollDirection;
        static bool previousInvertScrollDirection;

        public static bool useEquipDelayTimes;

        bool updateHotkeys;
        public static bool hotkeySettingsEnabled;

        public static HotkeySettings GetHotkeySettings()
        {
            return new HotkeySettings(
                maxSlots,
                maxRows,
                panelSize,
                xAxis,
                yAxis,
                spacing,
                displayHotkeys,
                invertScrollDirection,
                useEquipDelayTimes);
        }

        public static void LoadHotkeySettings(HotkeySettings settings)
        {
            maxSlots = settings.MaxSlots;
            maxRows = settings.MaxRows;
            panelSize = settings.PanelSize;
            xAxis = settings.XAxis;
            yAxis = settings.YAxis;
            spacing = settings.Spacing;
            displayHotkeys = settings.DisplayHotkeys;
            invertScrollDirection = settings.InvertScrollDirection;
            useEquipDelayTimes = settings.UseEquipDelayTimes;

            UpdateHotkeysUI();
        }

        public static void LoadDefaultHotkeySettings(int maxSlotsData, int maxRowsData)
        {
            maxSlots = maxSlotsData;
            maxRows = maxRowsData;

            panelSize = 16;
            panel.panelSize = panelSize;

            xAxis = panel.GetDefaultXAxis(maxSlots);
            panel.panelXAxis = xAxis;

            yAxis = 183;
            panel.panelYAxis = yAxis;

            spacing = 24;
            panel.columnStep = spacing;

            displayHotkeys = true;
            panel.displayHotkeyBar = displayHotkeys;

            invertScrollDirection = true;
            HotkeysMod.instance.InvertScrollOrder = invertScrollDirection;

            useEquipDelayTimes = false;
        }

        private void Start()
        {      
            panel = HotkeysMod.instance.panel;
            scrollImage = HotkeysMod.mod.GetAsset<Texture2D>("ScrollCleaned");

            maxSlots = HotkeysMod.instance.MaxHotkeyBarSize;
            previousmaxSlots = maxSlots;

            maxRows = HotkeysMod.instance.MaxHotkeyBanks;
            previousMaxRows = maxRows;

            panelSize = panel.panelSize;
            previousPanelSize = panelSize;

            xAxis = panel.panelXAxis;
            previousXAxis = xAxis;

            yAxis = panel.panelYAxis;
            previousYAxis = yAxis;

            spacing = panel.columnStep;
            previousSpacing = spacing;

            displayHotkeys = panel.displayHotkeyBar;
            previousDisplayHotkeys = displayHotkeys;

            invertScrollDirection = HotkeysMod.instance.InvertScrollOrder;
            previousInvertScrollDirection = invertScrollDirection;
        }

        private void OnGUI()
        {
            if (!hotkeySettingsEnabled)
            {
                return;
            }

            GUI.contentColor = Color.black;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box);
                style.normal.background = scrollImage;
                style.normal.textColor = Color.black;
                style.fontStyle = FontStyle.Bold;
                style.fontSize = 14;
            }

            hotkeySettingsControlsRect = GUI.Window(0, new Rect(Screen.width * .4f, Screen.height * .25f, 270, 400), HotkeySettingsControls, string.Empty, style);
        }

        private void HotkeySettingsControls(int windowID)
        {
            GUI.Label(new Rect(0, 5, 270, 25), "Hotkey Bar Settings", style);

            labelY = 35;
            sliderY = 40;
            valueY = 33;

            maxSlots = RenderSlider("Slots", 1, 9, ref maxSlots);
            maxRows = RenderSlider("Rows", 1, 5, ref maxRows);
            panelSize = RenderSlider("Size", 5, 25, ref panelSize);
            xAxis = RenderSlider("XAxis", -20, 300, ref xAxis);
            yAxis = RenderSlider("YAxis", -20, 200, ref yAxis);
            spacing = RenderSlider("Space", 10, 35, ref spacing);

            if (GUI.Button(new Rect(55, labelY, 150, 25), "Reset Size/Position", style))
            {
                panelSize = 16;
                xAxis = panel.GetDefaultXAxis(maxSlots);
                yAxis = 183;
                spacing = 24;
            }

            GUI.contentColor = Color.black;

            if (toggleStyle == null)
            {
                toggleStyle = new GUIStyle(GUI.skin.toggle);
                toggleStyle.normal.textColor = Color.black;
                toggleStyle.fontSize = 14;
                toggleStyle.fontStyle = FontStyle.Bold;
            }

            invertScrollDirection = GUI.Toggle(new Rect(10, labelY + ySpacing, 200, 25), invertScrollDirection, "Invert Scroll Direction", toggleStyle);

            displayHotkeys = GUI.Toggle(new Rect(10, labelY + (ySpacing + 20), 200, 25), displayHotkeys, "Display Hotkey Bar", toggleStyle);

            useEquipDelayTimes = GUI.Toggle(new Rect(10, labelY + (ySpacing + 40), 200, 25), useEquipDelayTimes, "Use Equip Delay Times", toggleStyle);

            // check for changes to the settings
            updateHotkeys = previousmaxSlots != maxSlots ||
                previousMaxRows != maxRows ||
                previousPanelSize != panelSize ||
                previousXAxis != xAxis ||
                previousYAxis != yAxis ||
                previousSpacing != spacing ||
                previousDisplayHotkeys != displayHotkeys ||
                previousInvertScrollDirection != invertScrollDirection;

            if (updateHotkeys)
            {
                UpdateHotkeysUI();
            }
        }

        private static void UpdateHotkeysUI()
        {
            previousInvertScrollDirection = invertScrollDirection;
            HotkeysMod.instance.InvertScrollOrder = invertScrollDirection;

            previousmaxSlots = maxSlots;
            HotkeysMod.instance.MaxHotkeyBarSize = maxSlots;
            
            HotkeysMod.instance.MaxHotkeyBanks = maxRows;
            HotkeysMod.instance.ResizeHotkeyBar();
            previousMaxRows = maxRows;

            previousPanelSize = panelSize;
            panel.panelSize = panelSize;

            previousXAxis = xAxis;
            panel.panelXAxis = xAxis;

            previousYAxis = yAxis;
            panel.panelYAxis = yAxis;

            previousSpacing = spacing;
            panel.columnStep = spacing;

            previousDisplayHotkeys = displayHotkeys;
            panel.displayHotkeyBar = displayHotkeys;

            panel.InitIcons();
            panel.UpdateIcons();
        }

        private int RenderSlider(string name, int minValue, int maxValue, ref int valueToUpdate)
        {
            GUI.Label(new Rect(10, labelY, 50, 25), name, style);
            var sliderValue = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(60, sliderY, 145, 25), valueToUpdate, minValue, maxValue));
            GUI.Label(new Rect(205, valueY, 55, 25), sliderValue.ToString(), style);

            labelY += ySpacing;
            sliderY += ySpacing;
            valueY += ySpacing;

            return sliderValue;
        }
    }
}
