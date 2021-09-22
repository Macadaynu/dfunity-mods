using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.Game.MacadaynuMods.HotkeyBar;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using UnityEngine;

namespace Assets.Scripts.MyMods
{
    public class HotkeysPanel : Panel
    {
        #region Fields

        public int panelSize = 16;
        public int panelXAxis;
        public int panelYAxis = 183;
        public int columnStep = 24;
        public bool displayHotkeyBar = true;

        class IconsPositioning
        {
            public readonly Vector2 iconSize;
            public readonly Vector2 origin;
            public readonly Vector2 columnStep;
            public readonly Vector2 rowStep;
            public readonly int iconColumns;

            public IconsPositioning(Vector2 iconSize, Vector2 origin, Vector2 columnStep, Vector2 rowStep, int iconColumns)
            {
                this.iconSize = iconSize;
                this.origin = origin;
                this.columnStep = columnStep;
                this.rowStep = rowStep;
                this.iconColumns = iconColumns;
            }
        }

        IconsPositioning selfIconsPositioning;

        Panel[] iconPool;
        List<ActiveHotkey> activeSelfList = new List<ActiveHotkey>();

        ToolTip defaultToolTip = null;
        bool lastLargeHUD;

        #endregion

        #region Structs & Enums

        /// <summary>
        /// Stores information for icon display.
        /// </summary>
        public struct ActiveHotkey
        {
            public int iconIndex;
            public SpellIcon? icon;
            public string displayName;
            public bool isItem;
            public int poolIndex;
            public ImageData imageData;
            public DaggerfallUnityItem item;
            public KeyCode keyCode;
        }

        #endregion

        #region Constructors

        public HotkeysPanel()
            : base()
        {
            iconPool = new Panel[HotkeysMod.instance.MaxHotkeyBarSize];

            AutoSize = AutoSizeModes.None;
            DaggerfallUI.Instance.UserInterfaceManager.OnWindowChange += UpdateIconsHandler;
            HotkeysMod.instance.OnHotkeyPressed += UpdateIconsHandler;

            panelXAxis = GetDefaultXAxis(HotkeysMod.instance.MaxHotkeyBarSize);

            InitIcons();
        }

        #endregion

        #region Public Methods

        public void UpdateIconsHandler(object o, EventArgs e)
        {
            var windowType = DaggerfallUI.Instance.UserInterfaceManager.TopWindow.GetType();

            if (windowType == typeof(DaggerfallInventoryWindow) || windowType == typeof(DaggerfallSpellBookWindow) || (windowType?.IsSubclassOf(typeof(DaggerfallSpellBookWindow)) ?? false || (windowType?.IsSubclassOf(typeof(DaggerfallInventoryWindow)) ?? false)))
            {
                UpdateIcons();
            }
            else if (o.GetType() == typeof(HotkeysMod))
            {
                var hotkeyEventArgs = (HotkeysMod.HotkeyEventArgs)e;

                UpdateIcons(hotkeyEventArgs.HotKeyCode == KeyCode.Alpha0);

                if (hotkeyEventArgs.HotKeyCode != KeyCode.Minus && hotkeyEventArgs.HotKeyCode != KeyCode.Equals)
                {
                    FlashIcon(hotkeyEventArgs.HotKeyCode);
                }
            }
        }

        private async Task FlashIcon(KeyCode keyCode)
        {
            var icon = activeSelfList.First(x => x.keyCode == keyCode);

            var panel = iconPool[icon.poolIndex];

            await FlashHotkey(panel);
        }

        private async Task FlashHotkey(Panel panel)
        {
            var color = new Color(0, 0, 0, 0.5f); // faded black

            await Fade(panel, 0f, Color.white);
            await Fade(panel, 0.5f, color);
        }

        private async Task Fade(Panel panel, float alpha, Color targetColor)
        {
            Color initialColor = panel.BackgroundColor;

            float elapsedTime = 0f;
            float fadeDuration = 0.1f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                Color currentColor = Color.Lerp(initialColor, targetColor, elapsedTime / fadeDuration);
                panel.BackgroundColor = currentColor;
                await Task.Yield();
            }
        }

        public override void Update()
        {
            base.Update();

            // Adjust icons when large HUD state changes
            if (DaggerfallUI.Instance.DaggerfallHUD != null &&
                DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.Enabled != lastLargeHUD)
            {
                lastLargeHUD = DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.Enabled;
                UpdateIcons();
            }
        }

        public override void Draw()
        {
            base.Draw();

            // Draw tooltips when paused or cursor active
            if ((GameManager.IsGamePaused || GameManager.Instance.PlayerMouseLook.cursorActive) && defaultToolTip != null)
                defaultToolTip.Draw();
        }

        public void ResizeHotkeyBar()
        {
            Components.Clear();


        }

        public int GetDefaultXAxis(int maxHotkeyBarSize)
        {
            switch (maxHotkeyBarSize)
            {
                case 1:
                    return panelXAxis = 150;
                case 2:
                    return panelXAxis = 140;
                case 3:
                    return panelXAxis = 130;
                case 4:
                    return panelXAxis = 115;
                case 5:
                    return panelXAxis = 100;
                case 6:
                    return panelXAxis = 90;
                case 7:
                    return panelXAxis = 80;
                case 8:
                    return panelXAxis = 70;
                case 9:
                    return panelXAxis = 50;
                default:
                    return panelXAxis = 50;
            }
        }

        #endregion

        #region Private Methods

        public void InitIcons()
        {
            Components.Clear();

            iconPool = new Panel[HotkeysMod.instance.MaxHotkeyBarSize];

            // move the hotkeys to align with max hotkey bar size
            selfIconsPositioning = new IconsPositioning(
                new Vector2(panelSize, panelSize),
                new Vector2(panelXAxis, panelYAxis),
                new Vector2(columnStep, 0),
                new Vector2(0, 24),
                HotkeysMod.instance.MaxHotkeyBarSize);

            // Setup default tooltip
            //if (DaggerfallUnity.Settings.EnableToolTips)
            //{
            //    defaultToolTip = new ToolTip();
            //    defaultToolTip.ToolTipDelay = DaggerfallUnity.Settings.ToolTipDelayInSeconds;
            //    defaultToolTip.BackgroundColor = DaggerfallUnity.Settings.ToolTipBackgroundColor;
            //    defaultToolTip.TextColor = DaggerfallUnity.Settings.ToolTipTextColor;
            //    defaultToolTip.Parent = this;
            //}

            // Setup icon panels
            for (int i = 0; i < iconPool.Length; i++)
            {
                iconPool[i] = new Panel
                {
                    BackgroundColor = new Color(0, 0, 0, 0.5f), // faded black
                    AutoSize = AutoSizeModes.None,
                    Enabled = true,
                    //ToolTip = defaultToolTip
                };

                Components.Add(iconPool[i]);
            }
        }

        void ClearIcons()
        {
            for (int i = 0; i < iconPool.Length; i++)
            {
                iconPool[i].BackgroundTexture = null;
                iconPool[i].Enabled = false;
            }

            activeSelfList.Clear();
        }

        public void UpdateIcons(bool toggleDisplay = false)
        {
            if (toggleDisplay)
            {
                displayHotkeyBar = !displayHotkeyBar;

                for (int i = 0; i < iconPool.Length; i++)
                {
                    iconPool[i].Enabled = displayHotkeyBar;

                    //var color = new Color(iconPool[i].BackgroundColor.r, iconPool[i].BackgroundColor.g, iconPool[i].BackgroundColor.b, 0.0f);
                    //Fade(iconPool[i], 0.0f, color);
                }

                return;
            }

            ClearIcons();

            //Get All Hotkeys
            var assignedHotkeys = HotkeysMod.instance.hotkeys;

            // Sort icons into active spells in self and other icon lists
            int poolIndex = 0;
            for (int i = 0; i < assignedHotkeys.Count; i++)
            {
                var assignedHotkey = assignedHotkeys.ElementAt(i);

                ActiveHotkey hotkey = new ActiveHotkey();
                if (assignedHotkey.Value != null)
                {
                    switch (assignedHotkey.Value.Type)
                    {
                        case HotkeyType.Spell:
                            hotkey.icon = assignedHotkey.Value.Spell.Icon;
                            hotkey.iconIndex = assignedHotkey.Value.Spell.IconIndex;
                            hotkey.displayName = assignedHotkey.Value.Spell.Name;
                            break;
                        case HotkeyType.CampingEquipment:
                            hotkey = AddItemInfoToPanel(ItemGroups.UselessItems2, 530, hotkey);
                            break;
                        case HotkeyType.Meat:
                            hotkey = AddItemInfoToPanel(ItemGroups.UselessItems2, 537, hotkey);
                            break;
                        case HotkeyType.LightSource:
                            hotkey = AddItemInfoToPanel(ItemGroups.UselessItems2, assignedHotkey.Value.TemplateId, hotkey);
                            break;
                        default:
                            var item = GameManager.Instance.PlayerEntity.Items.GetItem((ulong)assignedHotkey.Value.Id);
                            // check if you have the item in your inventory, if not remove icon from hotkey bar
                            if (item != null)
                            {
                                var imageData = DaggerfallUnity.Instance.ItemHelper.GetItemImage(item);
                                hotkey.iconIndex = imageData.record;
                                hotkey.imageData = imageData;
                                hotkey.item = item;
                                hotkey.displayName = item.LongName;
                            }
                            break;
                    }
                }

                hotkey.poolIndex = poolIndex++;
                hotkey.keyCode = assignedHotkey.Key;
                //item.expiring = (GetMaxRoundsRemaining(bundle) < 2) ? true : false;
                //item.isItem = (effectBundles[i].fromEquippedItem != null);
                activeSelfList.Add(hotkey);
            }

            // Update icon panels in pooled collection
            AlignIcons(activeSelfList, selfIconsPositioning);
        }


        ActiveHotkey AddItemInfoToPanel(ItemGroups itemGroup, int? templateId, ActiveHotkey hotkey)
        {
            if (templateId.HasValue)
            {
                var item = ItemBuilder.CreateItem(itemGroup, templateId.Value);
                if (item != null)
                {
                    var imageData = DaggerfallUnity.Instance.ItemHelper.GetItemImage(item);

                    hotkey.iconIndex = imageData.record;
                    hotkey.imageData = imageData;
                    hotkey.item = item;
                    hotkey.displayName = item.LongName;
                }
            }

            return hotkey;
        }

        void AlignIcons(List<ActiveHotkey> icons, IconsPositioning iconsPositioning)
        {
            Vector2 rowOrigin = iconsPositioning.origin;
            Vector2 position = rowOrigin;
            int column = 0;
            foreach (ActiveHotkey hotkey in icons)
            {
                Panel icon = iconPool[hotkey.poolIndex];

                icon.Components.Clear();

                // add the hotkey number label
                var hotkeyNumber = DaggerfallUI.AddTextLabel(DaggerfallUI.Instance.Font4, Vector2.zero, (hotkey.poolIndex + 1).ToString());
                hotkeyNumber.HorizontalAlignment = HorizontalAlignment.Left;
                hotkeyNumber.VerticalAlignment = VerticalAlignment.Top;
                hotkeyNumber.ShadowPosition = Vector2.zero;
                hotkeyNumber.TextScale = 0.75f;
                hotkeyNumber.TextColor = DaggerfallUI.DaggerfallDefaultInputTextColor;
                icon.Components.Add(hotkeyNumber);
                icon.BackgroundTextureLayout = BackgroundLayout.ScaleToFit;

                // add the bank number label
                if (hotkey.poolIndex == 0 && HotkeysMod.instance.MaxHotkeyBanks > 1)
                {
                    var hotkeyBankNumber = DaggerfallUI.AddTextLabel(DaggerfallUI.Instance.Font4, Vector2.zero, HotkeysMod.instance.CurrentHotkeyBank.ToString());
                    hotkeyBankNumber.HorizontalAlignment = HorizontalAlignment.Left;
                    hotkeyBankNumber.VerticalAlignment = VerticalAlignment.Bottom;
                    hotkeyBankNumber.ShadowPosition = Vector2.zero;
                    hotkeyBankNumber.TextScale = 0.75f;
                    hotkeyBankNumber.TextColor = DaggerfallUI.DaggerfallUnityDefaultToolTipTextColor;
                    icon.Components.Add(hotkeyBankNumber);
                    icon.BackgroundTextureLayout = BackgroundLayout.ScaleToFit;
                }

                if (hotkey.item != null) //isItem instead?
                {
                    icon.BackgroundTexture = DaggerfallUnity.Instance.ItemHelper.GetInventoryImage(hotkey.item).texture;

                    // add stack amount label
                    if (hotkey.item.stackCount > 1)
                    {
                        var label = DaggerfallUI.AddTextLabel(DaggerfallUI.Instance.Font4, Vector2.zero, hotkey.item.stackCount.ToString());
                        label.HorizontalAlignment = HorizontalAlignment.Right;
                        label.VerticalAlignment = VerticalAlignment.Bottom;
                        label.ShadowPosition = Vector2.zero;
                        label.TextScale = 0.75f;
                        label.TextColor = DaggerfallUI.DaggerfallUnityDefaultToolTipTextColor;
                        icon.Components.Add(label);
                    }
                }
                else if (hotkey.icon.HasValue)
                {
                    icon.BackgroundTexture = DaggerfallUI.Instance.SpellIconCollection.GetSpellIcon(hotkey.icon.Value);
                }

                icon.ToolTipText = hotkey.displayName;
                icon.Enabled = displayHotkeyBar;
                icon.Position = position;
                AdjustIconPositionForLargeHUD(icon);
                icon.Size = iconsPositioning.iconSize;
                if (++column == iconsPositioning.iconColumns)
                {
                    rowOrigin += iconsPositioning.rowStep;
                    position = rowOrigin;
                    column = 0;
                }
                else
                {
                    position += iconsPositioning.columnStep;
                }
            }
        }

        void AdjustIconPositionForLargeHUD(Panel icon)
        {
            // Adjust position for variable sized large HUD
            // Icon will remain in default position unless it needs to avoid being drawn under HUD
            if (DaggerfallUI.Instance.DaggerfallHUD != null && DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.Enabled)
            {
                float startY = icon.Position.y;
                float offset = Screen.height - (int)DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.Rectangle.height;
                float localY = (offset / LocalScale.y) - 18;
                if (localY < startY)
                    icon.Position = new Vector2(icon.Position.x, (int)localY);
            }
        }

        #endregion
    }
}