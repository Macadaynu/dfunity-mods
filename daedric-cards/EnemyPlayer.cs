using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts.Game.Macadaynu_Mods
{
    public class EnemyPlayer
    {
        public static List<Card> cards = new List<Card>();
        public static int mana;

        public static void LoadCardHand()
        {
            // Load enemy cards
            cards.Add(CardLoader.GetCard("Skeletal Warrior"));
            cards.Add(CardLoader.GetCard("Vampire Ancient"));
            cards.Add(CardLoader.GetCard("Ancient Lich"));
            cards.Add(CardLoader.GetCard("Grizzly Bear"));
            cards.Add(CardLoader.GetCard("Giant Rat"));
            cards.Add(CardLoader.GetCard("Fire Atronach"));
        }
    }
}