using UnityEngine;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public Text nameText;
    public Text descriptionText;
    public Image artworkImage;
    public Text soulGemText;
    public Text attackText;
    public Text healthText;


    public void LoadCard(Card card)
    {
        nameText.text = card.name;
        descriptionText.text = card.description;
        //artworkImage.sprite = card.artwork;
        soulGemText.text = card.cost.ToString();
        attackText.text = card.attack.ToString();
        healthText.text = card.health.ToString();
    }

    public void DisplayCardHand()
    {
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
    }

    public void HideCardHand()
    {
        transform.position = new Vector3(transform.position.x, -110, transform.position.z);
    }
}
