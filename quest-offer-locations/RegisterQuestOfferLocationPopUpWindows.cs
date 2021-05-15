using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using UnityEngine;

namespace Assets.Scripts.Game.UserInterfaceWindows
{
    public class RegisterQuestOfferLocationPopUpWindows : MonoBehaviour
    {
        static Mod mod;

        public void Awake()
        {
            mod.IsReady = true;
        }

        public void Start()
        {
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.GuildServicePopup, typeof(QuestOfferLocationGuildServicePopUpWindow));
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.QuestOffer, typeof(QuestOfferLocationWindow));
        }

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<RegisterQuestOfferLocationPopUpWindows>();
        }
    }
}
