using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using UnityEngine;

namespace Assets.Scripts.Game.MacadaynuMods.ConvenientQuestLog
{
    public class RegisterConvenientQuestLogWindow : MonoBehaviour
    {
        static Mod mod;

        public void Awake()
        {
            mod.IsReady = true;
        }

        public void Start()
        {
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.QuestJournal, typeof(ConvenientQuestLogWindow));
        }

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<RegisterConvenientQuestLogWindow>();
        }
    }
}