using Assets.Scripts.Game.Macadaynu_Mods;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;

public class DaedricCardsMod : MonoBehaviour
{
    public static Mod mod;
    public static DaedricCardsMod instance;
    static ModSettings settings;
    public static GameObject backgroundCanvas;
    public static GameObject boardCanvas;
    bool displayCardGame;

    //Card skeletonCard;
    public static GameObject cardCanvas;

    //starts mod manager on game begin. Grabs mod initializing paramaters.
    //ensures SateTypes is set to .Start for proper save data restore values.
    [Invoke(StateManager.StateTypes.Start, 0)]
    public static void Init(InitParams initParams)
    {
        //sets up instance of class/script/mod.
        GameObject go = new GameObject("DaedricCardsMod");
        instance = go.AddComponent<DaedricCardsMod>();

        //initiates mod paramaters for class/script.
        mod = initParams.Mod;
        //loads mods settings.
        //settings = mod.GetSettings();
        //initiates save paramaters for class/script.
        //mod.SaveDataInterface = instance;
        //after finishing, set the mod's IsReady flag to true.
        mod.IsReady = true;
    }

    void Start()
    {
        //var cardsJson = mod.GetAsset<TextAsset>("Cards");

        //skeletonCard = JsonUtility.FromJson<Card>(cardsJson.text);

        //skeletonCard = LoadCards.cards.First();

        CardLoader.LoadAllCards();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        var ui = FindObjectOfType<DaggerfallUI>();

        if (!displayCardGame)
        {
            displayCardGame = true;
            ui.enabled = false;      

            backgroundCanvas = mod.GetAsset<GameObject>("BackgroundCanvas", true);
            cardCanvas = mod.GetAsset<GameObject>("CardCanvas", true);
            boardCanvas = mod.GetAsset<GameObject>("BoardCanvas", true);

            BattleSystem.SetupBattle();

            //LoadCard("Skeleton Warrior");
            //LoadCard("Vampire Ancient");
            ////LoadCard("Flame Atronach");
            //LoadCard("Ancient Lich");

            //Minion.LoadMinion(Board.allCells[2,0], CardLoader.GetCard("Azura"));
            //Minion.LoadMinion(Board.allCells[2,4], CardLoader.GetCard("Hircine"));
        }
        else
        {            
            Destroy(backgroundCanvas);
            Destroy(boardCanvas);
            Destroy(cardCanvas);

            displayCardGame = false;
            ui.enabled = true;
        }
    }

    //GameObject LoadCard(string cardName)
    //{
    //    var cardStats = CardLoader.cards.Single(x => x.name == cardName);

    //    var card = mod.GetAsset<GameObject>("Card", true);

    //    // place the card in the hand section
    //    card.transform.SetParent(cardCanvas.transform.Find("Hand"));

    //    // load the card template
    //    var cardTemplateSprite = mod.GetAsset<Sprite>("CardTemplate4");
    //    var cardTemplate = card.transform.Find("Template");
    //    var templateImage = cardTemplate.GetComponent<Image>();
    //    templateImage.sprite = cardTemplateSprite;

    //    // display the card's image
    //    var cardSprite = mod.GetAsset<Sprite>(cardName);
    //    var cardImage = card.transform.Find("Image");
    //    var image = cardImage.GetComponent<Image>();
    //    image.sprite = cardSprite;

    //    // display the card's name
    //    var name = card.transform.Find("Name");
    //    var text = name.GetComponent<TextMeshProUGUI>();
    //    text.text = cardName;

    //    // display the card's description
    //    var description = card.transform.Find("Description");
    //    var descriptionText = description.GetComponent<TextMeshProUGUI>();
    //    descriptionText.text = cardStats.description;

    //    // display the card's attack
    //    var attack = card.transform.Find("Attack");
    //    var attackText = attack.GetComponent<TextMeshProUGUI>();
    //    attackText.text = cardStats.attack.ToString();

    //    // display the card's health
    //    var hp = card.transform.Find("Health");
    //    var hpText = hp.GetComponent<TextMeshProUGUI>();
    //    hpText.text = cardStats.health.ToString();

    //    // display the card's cost
    //    var cost = card.transform.Find("Cost");
    //    var costText = cost.GetComponent<TextMeshProUGUI>();
    //    costText.text = cardStats.cost.ToString();

    //    return card;
    //}
}
