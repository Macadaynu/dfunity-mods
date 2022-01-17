using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using Assets.MacadaynuMods.BetterArchery;
using DaggerfallWorkshop;

namespace BetterArcheryModMod
{
    public class BetterArcheryMod : MonoBehaviour
    {
        int playerLayerMask = 0;
        Camera mainCamera;
        private static Mod mod;
        static float RayDistance = 3072 * MeshReader.GlobalScale;
        GameObject target;
        public static DaggerfallAudioSource audioSource;
        public static BetterArcheryMod instance;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            instance = go.AddComponent<BetterArcheryMod>();
            go.AddComponent<AudioSource>();
            audioSource = go.AddComponent<DaggerfallAudioSource>();

            mod.IsReady = true;
        }

        private void Start()
        {
            mainCamera = GameManager.Instance.MainCamera;
            playerLayerMask = ~(1 << LayerMask.NameToLayer("Player"));
        }

        private void Update()
        {
            if (target != null)
            {
                var missile = FindObjectOfType<DaggerfallMissile>();

                if (missile != null
                    && missile.Caster == GameManager.Instance.PlayerEntityBehaviour
                    && missile.name.StartsWith("ArrowMissile")
                    && missile.gameObject.GetComponent<DaggerfallArrow>() == null)
                {
                    var arrow = missile.gameObject.AddComponent<DaggerfallArrow>();
                    arrow.target = target;
                    target = null;
                }
            }

            if (InputManager.Instance.ActionStarted(InputManager.Actions.SwingWeapon)
                && (GameManager.Instance.WeaponManager.ScreenWeapon && GameManager.Instance.WeaponManager.ScreenWeapon.WeaponType == WeaponTypes.Bow))
            {
                Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

                RaycastHit hit;
                bool hitSomething = Physics.Raycast(ray, out hit, RayDistance, playerLayerMask);
                if (hitSomething)
                {
                    target = hit.collider.gameObject;
                }
            }
        }
    }
}
