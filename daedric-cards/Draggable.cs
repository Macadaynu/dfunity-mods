using Assets.Scripts.Game.Macadaynu_Mods;
using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject hand;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (BattleSystem.state == BattleState.PLAYERTURN)
        {
            hand = transform.parent.gameObject;

            hand.SetActive(false);

            transform.SetParent(transform.root);

            GetComponent<CanvasGroup>().blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (BattleSystem.state == BattleState.PLAYERTURN)
        {
            transform.position = eventData.position;

            Board.DisplayValidDropZone();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (BattleSystem.state == BattleState.PLAYERTURN)
        {
            hand.SetActive(true);

            transform.SetParent(hand.transform);

            hand.transform.SetPositionAndRotation(new Vector3(hand.transform.position.x, -200.0f, hand.transform.position.z), new Quaternion());

            GetComponent<CanvasGroup>().blocksRaycasts = true;

            Board.ClearDisplayHelpers();
        }
    }
}
