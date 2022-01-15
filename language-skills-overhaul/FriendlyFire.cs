using Assets.Scripts.Game.MacadaynuMods;
using DaggerfallWorkshop.Game;
using System.Linq;
using UnityEngine;

public class FriendlyFire : MonoBehaviour
{
    void Start()
    {
        var missile = GetComponent<DaggerfallMissile>();

        if (missile.name.StartsWith("ArrowMissile") || (!missile.Payload?.Settings.Effects?.Any(x => x.Key.StartsWith("Heal")) ?? false))
        {
            foreach (var follower in LanguageSkillsOverhaulMod.enemyFollowers)
            {
                foreach (var collider in gameObject.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(follower.GetComponent<Collider>(), collider);
                }                
            }
        }
    }
}
