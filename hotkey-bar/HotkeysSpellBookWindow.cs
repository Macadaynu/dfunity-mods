using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HotkeysMod;

namespace Assets.Scripts.MyMods
{
    public class HotkeysSpellBookWindow : DaggerfallSpellBookWindow
    {
        #region Constructors

        public HotkeysSpellBookWindow(IUserInterfaceManager uiManager, DaggerfallBaseWindow previous = null, bool buyMode = false)
            : base(uiManager, previous, buyMode)
        {
            instance.OnHotkeyAssigned += HotKeyAssignedHandler;
        }

        public void HotKeyAssignedHandler(object o, EventArgs e)
        {
            var hotkeyEventArgs = (HotkeyEventArgs)e;

            var windowType = DaggerfallUI.Instance.UserInterfaceManager.TopWindow.GetType();

            if (windowType == typeof(HotkeysSpellBookWindow))
            {
                var tokens = new List<TextFile.Token>();

                tokens.Add(new TextFile.Token { formatting = TextFile.Formatting.Text, text = $"{spellsListBox.SelectedItem} has been assigned to Hotkey: {hotkeyEventArgs.HotKeyCode.ToString().Last()}" });
                tokens.Add(new TextFile.Token { formatting = TextFile.Formatting.JustifyCenter });

                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.Nothing, tokens.ToArray(), this);
                messageBox.ClickAnywhereToClose = true;
                messageBox.AllowCancel = false;
                messageBox.ParentPanel.BackgroundColor = Color.clear;
                DaggerfallUI.UIManager.PushWindow(messageBox);
            }
        }

        #endregion
        public override void OnPush()
        {
            base.OnPush();

            HotkeysMod.instance.hotkeySelectionId = 0;
        }

        public override void OnPop()
        {
            base.OnPop();

            HotkeysMod.instance.hotkeySelectionId = null;
        }

        protected override void UpdateSelection()
        {
            base.UpdateSelection();

            if (!buyMode)
            {
                HotkeysMod.instance.hotkeySelectionId = spellsListBox.SelectedIndex;
            }
        }
    }
}
