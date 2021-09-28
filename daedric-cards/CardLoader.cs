using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class CardLoader
{
    public static List<Card> cards = new List<Card>();

    public static void LoadAllCards()
    {
        TextAsset cardData = DaedricCardsMod.mod.GetAsset<TextAsset>("Daedric Princes Card Database");

        string[] data = cardData.text.Split(new char[] { '\n' });

        for (int i = 1; i < data.Length; i++)
        {
            string[] row = data[i].Split(new char[] { ',' });

            cards.Add(new Card(row));
        }
    }

    public static Card GetCard(string cardName)
    {
        return cards.Single(x => x.name == cardName);
    }

    public static Card GetCard(GameObject gameObject)
    {
        var cardName = gameObject.transform.Find("Name");

        var nameText = cardName.GetComponent<TextMeshProUGUI>()?.text;

        return cards.Single(x => x.name == nameText);
    }

    public static GameObject LoadCard(string cardName)
    {
        var cardStats = cards.SingleOrDefault(x => x.name == cardName);
        if (cardStats == null)
        {
            Debug.LogError($"{cardName} not found in DB");
        }

        var card = DaedricCardsMod.mod.GetAsset<GameObject>("Card", true);

        // place the card in the hand section
        card.transform.SetParent(DaedricCardsMod.cardCanvas.transform.Find("Hand"));

        // load the card template
        var cardTemplateSprite = DaedricCardsMod.mod.GetAsset<Sprite>("CardTemplate4");
        var cardTemplate = card.transform.Find("Template");
        var templateImage = cardTemplate.GetComponent<Image>();
        templateImage.sprite = cardTemplateSprite;

        // display the card's image
        var cardSprite = DaedricCardsMod.mod.GetAsset<Sprite>(cardName);
        var cardImage = card.transform.Find("Image");
        var image = cardImage.GetComponent<Image>();
        image.sprite = cardSprite;

        // display the card's name
        var name = card.transform.Find("Name");
        var text = name.GetComponent<TextMeshProUGUI>();
        text.text = cardName;

        // display the card's description
        var description = card.transform.Find("Description");
        var descriptionText = description.GetComponent<TextMeshProUGUI>();
        descriptionText.text = cardStats.description;

        // display the card's attack
        var attack = card.transform.Find("Attack");
        var attackText = attack.GetComponent<TextMeshProUGUI>();
        attackText.text = cardStats.attack.ToString();

        // display the card's health
        var hp = card.transform.Find("Health");
        var hpText = hp.GetComponent<TextMeshProUGUI>();
        hpText.text = cardStats.health.ToString();

        // display the card's cost
        var cost = card.transform.Find("Cost");
        var costText = cost.GetComponent<TextMeshProUGUI>();
        costText.text = cardStats.cost.ToString();

        return card;
    }
}
