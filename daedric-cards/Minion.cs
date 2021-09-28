using Assets.Scripts.Game.Macadaynu_Mods;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Minion : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int attack;
    public int health;
    public int baseAttack;
    public int baseHealth;
    public bool hasMoved;
    public bool hasAttacked;
    public SoundClips introSFX;
    public SoundClips deathSFX;
    public bool isPlayerMinion;
    public bool isDaedricPrince;
    public bool hasDied;
    public GameObject glow;

    public Cell currentCell => transform.parent.GetComponent<Cell>();

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CanMoveOrAttack())
        {
            if (!hasMoved)
            {
                Board.DisplayValidMoves(currentCell);
            }

            GetComponent<CanvasGroup>().blocksRaycasts = false;

            GetComponent<Canvas>().sortingOrder = 5;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (CanMoveOrAttack())
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.position = transform.parent.position;

        GetComponent<CanvasGroup>().blocksRaycasts = true;

        GetComponent<Canvas>().sortingOrder = 2;

        Board.ClearDisplayHelpers();
    }

    public static void SpawnMinion(Cell spawnCell, Card card, bool isPlayerMinion, bool isDaedricPrince = false)
    {
        var minionPrefab = DaedricCardsMod.mod.GetAsset<GameObject>("Minion");

        var minion = Instantiate(minionPrefab, spawnCell.transform);
        minion.gameObject.SetActive(true);

        var minionStats = minion.GetComponent<Minion>();
        minionStats.attack = card.attack;
        minionStats.health = card.health;
        minionStats.baseAttack = card.attack;
        minionStats.baseHealth = card.health;
        minionStats.introSFX = card.introSFX;
        minionStats.deathSFX = card.deathSFX;
        minionStats.isPlayerMinion = isPlayerMinion;
        minionStats.isDaedricPrince = isDaedricPrince;

        var minionArtwork = minion.transform.Find("Artwork");
        var minionImage = minionArtwork.GetComponent<Image>();
        minionImage.sprite = card.artwork;

        var attack = minion.transform.Find("Attack");
        attack.GetComponent<Text>().text = card.attack.ToString();

        var health = minion.transform.Find("Health");
        health.GetComponent<Text>().text = card.health.ToString();

        if (!isPlayerMinion)
        {
            var frame = minion.transform.Find("Frame");
            var frameImage = frame.GetComponent<Image>();
            frameImage.sprite = DaedricCardsMod.mod.GetAsset<Sprite>("Black Frame");
        }
        //else if (!isDaedricPrince)
        //{
        //    // TODO: This will need to change when summoning sickness is applied
        //    minionStats.ApplyCanMoveOrAttackGlow(true);
        //}

        minionStats.hasMoved = true;

        // Play the relevant minion sound
        DaggerfallUI.Instance.PlayOneShot(card.introSFX);
    }

    public bool CanMoveOrAttack()
    {
        return BattleSystem.state == BattleState.PLAYERTURN && isPlayerMinion && !isDaedricPrince && !hasMoved && !hasAttacked;
    }

    public void ApplyCanMoveOrAttackGlow(bool applyGlow)
    {
        if (glow == null)
        {
            glow = transform.Find("Glow").gameObject;
        }

        glow.SetActive(applyGlow);
    }
}
