using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform handle;
    public float movementRange = 50f;

    private Vector2 startPos;

    private void Start()
    {
        startPos = handle.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform bgRect = (RectTransform)transform;
        Vector2 pointerLocalPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, eventData.position, eventData.pressEventCamera, out pointerLocalPos))
        {
            pointerLocalPos = Vector2.ClampMagnitude(pointerLocalPos, movementRange);
            handle.anchoredPosition = pointerLocalPos;
            Vector2 norm = pointerLocalPos / movementRange;
            InputManager.Instance.SetMovementInput(norm);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = startPos;
        InputManager.Instance.SetMovementInput(Vector2.zero);
    }
}