using UnityEngine;
using UnityEngine.EventSystems;

// Menu button juice: grows slightly and nudges right while hovered or selected.
public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] float hoverScale = 1.05f;
    [SerializeField] float hoverNudge = 14f;

    RectTransform rt;
    Vector2 basePos;
    bool hot;

    void Awake()
    {
        rt = (RectTransform)transform;
        basePos = rt.anchoredPosition;
    }

    void Update()
    {
        float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * (hot ? hoverScale : 1f), k);
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, basePos + (hot ? new Vector2(hoverNudge, 0) : Vector2.zero), k);
    }

    public void OnPointerEnter(PointerEventData e) => hot = true;
    public void OnPointerExit(PointerEventData e) => hot = false;
    public void OnSelect(BaseEventData e) => hot = true;
    public void OnDeselect(BaseEventData e) => hot = false;
}
