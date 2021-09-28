using Assets.Scripts.Game.Macadaynu_Mods;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Cell : MonoBehaviour, IDropHandler
{
    [HideInInspector]
    public Image cellImage;

    [HideInInspector]
    public Vector2Int boardPosition = Vector2Int.zero;

    [HideInInspector]
    public Board board = null;

    [HideInInspector]
    public RectTransform rectTransform = null;

    public bool isValidDropZone;
    public bool isOccupied => GetComponentInChildren<Minion>() != null;

    public void Setup(Vector2Int newBoardPosition, Board newBoard, bool validDropZone)
    {
        boardPosition = newBoardPosition;
        board = newBoard;
        isValidDropZone = validDropZone;

        rectTransform = GetComponent<RectTransform>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (BattleSystem.state == BattleState.PLAYERTURN)
        {           
            if (isOccupied)
            {
                // if attacking with a minion
                if (eventData.pointerDrag.CompareTag("Minion"))
                {
                    var attackingMinion = eventData.pointerDrag.GetComponent<Minion>();

                    if (attackingMinion.CanMoveOrAttack())
                    {
                        var validCellMoves = Board.GetValidCellMoves(attackingMinion.currentCell);

                        if (validCellMoves.Any(x => x == this) && attackingMinion.CanMoveOrAttack())
                        {
                            AttackCalculator.AttackMinion(attackingMinion, transform.Find("Minion(Clone)").GetComponent<Minion>());
                        }
                    }
                }
            }
            else
            {
                // if playing a card
                if (eventData.pointerDrag.CompareTag("Card") && isValidDropZone)
                {
                    var card = CardLoader.GetCard(eventData.pointerDrag);

                    if (card.cost <= BattleSystem.ManaLeft)
                    {
                        // load a minion onto this cell
                        Minion.SpawnMinion(this, card, true);

                        // turn the card hand back on
                        var dragScript = eventData.pointerDrag.GetComponent<Draggable>();
                        dragScript.hand.SetActive(true);

                        // destroy the card just played
                        Destroy(eventData.pointerDrag);

                        // update mana left
                        BattleSystem.UpdateManaLeft(card.cost);
                    }
                }
                // if moving a minion
                else if (eventData.pointerDrag.CompareTag("Minion"))
                {
                    var movedMinion = eventData.pointerDrag.GetComponent<Minion>();

                    if (movedMinion.CanMoveOrAttack())
                    {
                        var validCellMoves = Board.GetValidCellMoves(movedMinion.currentCell);

                        if (validCellMoves.Any(x => x == this))
                        {
                            OccupyCell(movedMinion);

                            // Play a move sound ??
                            //DaggerfallUI.Instance.PlayOneShot(SoundClips.EnemyLichMove);
                        }
                        else
                        {
                            Debug.Log("CAN'T MOVE HERE JACKASS");
                        }
                    }

                }
            }
        }

        Board.ClearDisplayHelpers();

        var handTransform = DaedricCardsMod.cardCanvas.transform.Find("Hand").transform;

        handTransform.SetPositionAndRotation(new Vector3(handTransform.position.x, -200.0f, handTransform.position.z), new Quaternion());
    }

    public void OccupyCell(Minion movedMinion)
    {
        //isOccupied = true;

        movedMinion.transform.position = transform.position;
        movedMinion.transform.SetParent(transform);

        movedMinion.hasMoved = true;

        movedMinion.ApplyCanMoveOrAttackGlow(false);
        //movedMinion.cell = gameObject;
    }
}
