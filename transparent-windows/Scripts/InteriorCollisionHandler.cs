using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Entity;
using UnityEngine;

namespace Assets.MacadaynuMods.Transparent_Windows
{
    public class InteriorCollisionHandler : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name == "MobileNPC")
            {
                other.gameObject.SetActive(false);
            }
        }
    }
}