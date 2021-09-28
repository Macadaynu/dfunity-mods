using UnityEngine;

public class OnHandHover : MonoBehaviour
{
    Transform handTransform;

    private void Start()
    {
        handTransform = gameObject.transform.parent.Find("Hand");
    }

    public void OnPointerEnter()
    {
        //var hand = gameObject.transform.parent.Find("Hand");
        handTransform.SetPositionAndRotation(new Vector3(handTransform.position.x, 0.0f, handTransform.position.z), new Quaternion());
    }

    public void OnPointerExit()
    {
        //var hand = gameObject.transform.parent.Find("Hand");
        handTransform.SetPositionAndRotation(new Vector3(handTransform.position.x, -200.0f, handTransform.position.z), new Quaternion());
    }
}
