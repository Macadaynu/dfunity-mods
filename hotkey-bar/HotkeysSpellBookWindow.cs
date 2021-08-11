using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace Assets.Scripts.MyMods
{
    public class HotkeysSpellBookWindow : DaggerfallSpellBookWindow
    {
        #region Constructors

        public HotkeysSpellBookWindow(IUserInterfaceManager uiManager, DaggerfallBaseWindow previous = null, bool buyMode = false)
            : base(uiManager, previous, buyMode)
        {

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
