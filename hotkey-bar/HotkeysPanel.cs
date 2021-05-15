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
using UnityEngine;

namespace Assets.Scripts.MyMods
{
    /// <summary>
    /// Displays active spell icons on player HUD.
    /// </summary>
    public class HotkeysPanel : Panel
    {
        #region Fields

        const int maxIconPool = 9;

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

        Panel[] iconPool = new Panel[maxIconPool];
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
            AutoSize = AutoSizeModes.None;
            DaggerfallUI.Instance.UserInterfaceManager.OnWindowChange += UpdateIconsHandler;
            HotkeysMod.instance.OnHotkeyPressed += UpdateIconsHandler;

            SaveLoadManager.OnLoad += SaveLoadManager_OnLoad;

            InitIcons();
        }

        #endregion

        #region Public Methods

        public void UpdateIconsHandler(object o, EventArgs e)
        {
            var windowType = DaggerfallUI.Instance.UserInterfaceManager.TopWindow.GetType();

            if (windowType == typeof(HotkeysInventoryWindow) || windowType == typeof(HotkeysSpellBookWindow))
            {
                UpdateIcons();
            }
            else if (o.GetType() == typeof(HotkeysMod))
            {
                var hotkeyEventArgs = (HotkeysMod.HotkeyEventArgs) e;

                UpdateIcons(hotkeyEventArgs.HotKeyCode == KeyCode.Minus);

                FlashIcon(hotkeyEventArgs.HotKeyCode);
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
            var color = new Color(panel.BackgroundColor.r, panel.BackgroundColor.g, panel.BackgroundColor.b, panel.BackgroundColor.a);

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

        #endregion

        #region Private Methods

        void InitIcons()
        {
            selfIconsPositioning = new IconsPositioning(new Vector2(16, 16), new Vector2(50, 183), new Vector2(24, 0), new Vector2(0, 24), 9);

            // Setup default tooltip
            if (DaggerfallUnity.Settings.EnableToolTips)
            {
                defaultToolTip = new ToolTip();
                defaultToolTip.ToolTipDelay = DaggerfallUnity.Settings.ToolTipDelayInSeconds;
                defaultToolTip.BackgroundColor = DaggerfallUnity.Settings.ToolTipBackgroundColor;
                defaultToolTip.TextColor = DaggerfallUnity.Settings.ToolTipTextColor;
                defaultToolTip.Parent = this;
            }

            // Setup icon panels
            for (int i = 0; i < iconPool.Length; i++)
            {
                iconPool[i] = new Panel
                {
                    BackgroundColor = new Color(0, 0, 0, 0.5f), // faded black
                    AutoSize = AutoSizeModes.None,
                    Enabled = true,
                    ToolTip = defaultToolTip
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
                for (int i = 0; i < iconPool.Length; i++)
                {
                    iconPool[i].Enabled = !iconPool[i].Enabled;

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
            for (int i = 0;  i < assignedHotkeys.Count; i++)
            {
                var assignedHotkey = assignedHotkeys.ElementAt(i);

                ActiveHotkey hotkey = new ActiveHotkey();
                if (assignedHotkey.Value != null)
                {
                    switch (assignedHotkey.Value.Type)
                    {
                        case HotkeyType.Weapon:
                        case HotkeyType.Potion:
                            var item = GameManager.Instance.PlayerEntity.Items.GetItem((ulong)assignedHotkey.Value.Id);
                            if (item != null)
                            {
                                var imageData = DaggerfallUnity.Instance.ItemHelper.GetItemImage(item);
                                hotkey.iconIndex = imageData.record;
                                hotkey.imageData = imageData;
                                hotkey.item = item;
                                hotkey.displayName = item.LongName;
                            }
                            break;
                        case HotkeyType.Spell:

                            //TODO: Can this be done by a spell id, rather than name?
                            var spellSettings = new EffectBundleSettings();
                            EffectBundleSettings[] spellbook = GameManager.Instance.PlayerEntity.GetSpells();

                            for (int x = 0; x < spellbook.Length; x++)
                            {
                                if (spellbook[x].Name == assignedHotkey.Value.SpellName)
                                {
                                    if (!GameManager.Instance.PlayerEntity.GetSpell(x, out spellSettings))
                                    {
                                        return;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }

                            //GameManager.Instance.PlayerEntity.GetSpell(assignedHotkey.Value.Id, out spellSettings);
                            hotkey.icon = spellSettings.Icon;
                            hotkey.iconIndex = spellSettings.IconIndex;//assignedHotkey.Value.Id;
                            hotkey.displayName = spellSettings.Name;
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

        void AlignIcons(List<ActiveHotkey> icons, IconsPositioning iconsPositioning)
        {
            Vector2 rowOrigin = iconsPositioning.origin;
            Vector2 position = rowOrigin;
            int column = 0;
            foreach (ActiveHotkey hotkey in icons)
            {
                Panel icon = iconPool[hotkey.poolIndex];

                icon.Components.Clear();

                var hotkeyNumber = DaggerfallUI.AddTextLabel(DaggerfallUI.Instance.Font4, Vector2.zero, (hotkey.poolIndex + 1).ToString());
                hotkeyNumber.HorizontalAlignment = HorizontalAlignment.Left;
                hotkeyNumber.VerticalAlignment = VerticalAlignment.Top;
                hotkeyNumber.ShadowPosition = Vector2.zero;
                hotkeyNumber.TextScale = 0.75f;
                hotkeyNumber.TextColor = DaggerfallUI.DaggerfallDefaultInputTextColor;
                icon.Components.Add(hotkeyNumber);
                icon.BackgroundTextureLayout = BackgroundLayout.ScaleToFit;

                if (hotkey.item != null) //isItem instead?
                {
                    icon.BackgroundTexture = DaggerfallUnity.Instance.ItemHelper.GetInventoryImage(hotkey.item).texture;

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
                icon.Enabled = true;
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

        private void SaveLoadManager_OnLoad(SaveData_v1 saveData)
        {
            UpdateIcons();
        }

        #endregion
    }
}
