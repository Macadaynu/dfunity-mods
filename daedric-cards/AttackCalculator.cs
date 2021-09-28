using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Game.Macadaynu_Mods
{
    public class AttackCalculator : MonoBehaviour
    {
        public static void AttackMinion(Minion attackingMinion, Minion defendingMinion)
        {
            // TODO: add an attackTypeSFX column to card db
            // Play an attack sound
            DaggerfallUI.Instance.PlayOneShot(SoundClips.Hit1);

            attackingMinion.hasAttacked = true;

            defendingMinion.health -= attackingMinion.attack;
            attackingMinion.health -= defendingMinion.attack;

            HandleMinionHPChange(defendingMinion);
            HandleMinionHPChange(attackingMinion);

            attackingMinion.ApplyCanMoveOrAttackGlow(false);
        }

        public static bool CalculateTradeWorth(Minion attackingMinion, Minion defendingMinion)
        {
            var attackingMinionHealth = attackingMinion.health;
            var defendingMinionHealth = defendingMinion.health;

            defendingMinionHealth -= attackingMinion.attack;
            attackingMinionHealth -= defendingMinion.attack;

            // always attack the prince
            if (defendingMinion.isDaedricPrince)
            {
                return true;
            }

            // if bad, avoid the trade
            if (defendingMinionHealth > 0 && attackingMinionHealth <= 0)
            {
                return false;
            }

            // if in an offensive position, avoid the trade
            if (attackingMinion.currentCell.boardPosition.y < 2)
            {
                return false;
            }

            //TODO: if next to player prince, avoid the trade
            //if (defendingMinion)

            return true;

            //// if good, make the trade
            //if (defendingMinionHealth <= 0 && attackingMinionHealth > 0)
            //{
            //    return true;
            //}

            //// if even, make the trade
            //if (defendingMinionHealth <= 0 && attackingMinionHealth <= 0)
            //{
            //    return true;
            //}

            //return true;
        }

        static void HandleMinionHPChange(Minion minion)
        {
            if (minion.health < 1)
            {
                // Play a death sound
                DaggerfallUI.Instance.PlayOneShot(minion.deathSFX);

                minion.hasDied = true;
                Destroy(minion.gameObject);
                //isOccupied = false;
            }
            else
            {
                var textComponent = minion.transform.Find("Health").GetComponent<Text>();

                textComponent.text = minion.health.ToString();

                if (minion.health < minion.baseHealth)
                {
                    textComponent.color = Color.red;
                }
            }
        }
    }
}