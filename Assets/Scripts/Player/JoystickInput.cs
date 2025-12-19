using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI")]
    public RectTransform joystickRoot;   // JoystickBg + Handle parent
    public RectTransform handle;

    [Header("Settings")]
    public float movementRange = 80f;
    [Range(0.2f, 1f)]
    public float sensitivityExponent = 0.6f;

    private Vector2 startPos;
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        joystickRoot.gameObject.SetActive(false); // Başta gizli
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        joystickRoot.gameObject.SetActive(true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            eventData.position,
            null,
            out startPos
        );

        joystickRoot.localPosition = startPos;
        handle.anchoredPosition = Vector2.zero;

        OnDrag(eventData);
    }


    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pointerLocalPos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickRoot,
            eventData.position,
            eventData.pressEventCamera,
            out pointerLocalPos
        );

        pointerLocalPos = Vector2.ClampMagnitude(pointerLocalPos, movementRange);
        handle.anchoredPosition = pointerLocalPos;

        Vector2 norm = pointerLocalPos / movementRange;

        float magnitude = norm.magnitude;
        magnitude = Mathf.Pow(magnitude, sensitivityExponent);

        norm = norm.normalized * magnitude;

        InputManager.Instance.SetMovementInput(norm);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = Vector2.zero;
        joystickRoot.gameObject.SetActive(false);
        InputManager.Instance.SetMovementInput(Vector2.zero);
    }
}
