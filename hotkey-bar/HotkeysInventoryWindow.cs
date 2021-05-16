using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HotkeysMod;

namespace Assets.Scripts.MyMods
{
    public class HotkeysInventoryWindow : DaggerfallInventoryWindow
    {
        //TODO: Refactor this class

        #region Constructors

        public HotkeysInventoryWindow(IUserInterfaceManager uiManager, DaggerfallBaseWindow previous = null)
            : base(uiManager, previous)
        {
            instance.OnHotkeyAssigned += HotKeyAssignedHandler;
        }

        #endregion

        protected override void Setup()
        {
            base.Setup();

            paperDoll.OnMouseMove += PaperDoll_OnMouseMoveHotkeys;
        }

        public override void OnPop()
        {
            base.OnPop();

            HotkeysMod.instance.hoveredItem = null;
            HotkeysMod.instance.hotkeySelectionId = null;
        }

        public override void OnPush()
        {
            base.OnPush();

            HotkeysMod.instance.hoveredItem = null;
            HotkeysMod.instance.hotkeySelectionId = null;
        }
        
        public void HotKeyAssignedHandler(object o, EventArgs e)
        {
            var hotkeyEventArgs = (HotkeyEventArgs)e;

            var windowType = DaggerfallUI.Instance.UserInterfaceManager.TopWindow.GetType();

            if (windowType == typeof(HotkeysInventoryWindow))
            {
                ItemListScroller_OnHover(instance.hoveredItem);

                var tokens = new List<TextFile.Token>();

                tokens.Add(new TextFile.Token { formatting = TextFile.Formatting.Text, text = $"{instance.hoveredItem.LongName} has been assigned to Hotkey: {hotkeyEventArgs.HotKeyCode.ToString().Last()}" });
                tokens.Add(new TextFile.Token { formatting = TextFile.Formatting.JustifyCenter });

                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.Nothing, tokens.ToArray(), this);
                messageBox.ClickAnywhereToClose = true;
                messageBox.AllowCancel = false;
                messageBox.ParentPanel.BackgroundColor = Color.clear;
                DaggerfallUI.UIManager.PushWindow(messageBox);
            }
        }

        protected override void ItemListScroller_OnHover(DaggerfallUnityItem item)
        {
            // Display info in local target icon panel, replacing justification tokens
            TextFile.Token[] tokens = ItemHelper.GetItemInfo(item, DaggerfallUnity.TextProvider);
            MacroHelper.ExpandMacros(ref tokens, item);

            // Only keep the title part for paintings
            if (item.ItemGroup == ItemGroups.Paintings)
            {
                tokens = new TextFile.Token[] { new TextFile.Token() { formatting = TextFile.Formatting.Text, text = tokens[tokens.Length - 1].text.Trim() } };
            }

            if (HotkeysMod.instance.hotkeys.Any(x => x.Value?.Id == (int)item.UID))
            {
                var assignedHotKey = HotkeysMod.instance.hotkeys.First(x => x.Value?.Id == (int)item.UID).Key;

                var assingedHotKeyText = assignedHotKey.ToString();

                var tokenList = tokens.ToList();
                tokenList.Add(new TextFile.Token { formatting = TextFile.Formatting.Text, text = $"Hotkey: {assingedHotKeyText.Last()}" });
                tokens = tokenList.ToArray();
            }

            UpdateItemInfoPanel(tokens);

            HotkeysMod.instance.hotkeySelectionId = (int)item.UID;
            HotkeysMod.instance.hoveredItem = item;
        }

        protected void PaperDoll_OnMouseMoveHotkeys(int x, int y)
        {
            byte value = paperDoll.GetEquipIndex(x, y);
            if (value != 0xff)
            {
                if (value >= 0 && value < ItemEquipTable.EquipTableLength)
                {
                    DaggerfallUnityItem item = playerEntity.ItemEquipTable.EquipTable[value];
                    if (item != null)
                    {
                        HotkeysMod.instance.hotkeySelectionId = (int)item.UID;
                        HotkeysMod.instance.hoveredItem = item;

                        if (HotkeysMod.instance.hotkeys.Any(h => h.Value?.Id == (int)item.UID))
                        {
                            // Display info in local target icon panel, replacing justification tokens
                            TextFile.Token[] tokens = ItemHelper.GetItemInfo(item, DaggerfallUnity.TextProvider);
                            MacroHelper.ExpandMacros(ref tokens, item);

                            var assignedHotKey = HotkeysMod.instance.hotkeys.First(i => i.Value?.Id == (int)item.UID).Key;

                            var assingedHotKeyText = assignedHotKey.ToString();

                            var tokenList = tokens.ToList();
                            tokenList.Add(new TextFile.Token { formatting = TextFile.Formatting.Text, text = $"Hotkey: {assingedHotKeyText.Last()}" });
                            tokens = tokenList.ToArray();

                            UpdateItemInfoPanel(tokens);
                        }
                    }
                }
            }
        }
    }
}

