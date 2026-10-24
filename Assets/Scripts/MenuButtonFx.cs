using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
{
    public enum Sound { Click, Confirm, Back }

    [SerializeField] float hoverScale = 1.05f;
    [SerializeField] float hoverNudge = 14f;
    [SerializeField] Sound sound = Sound.Click;

    RectTransform rt;
    Vector2 basePos;
    bool hot;

    void Awake()
    {
        rt = (RectTransform)transform;
        basePos = rt.anchoredPosition;
    }

    void OnEnable()
    {
        hot = false;
        if (rt != null) rt.localScale = Vector3.one;
    }

    void Update()
    {
        float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * (hot ? hoverScale : 1f), k);
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, basePos + (hot ? new Vector2(hoverNudge, 0) : Vector2.zero), k);
    }

    void Light(bool on)
    {
        if (on && !hot && Time.unscaledTime > 0.5f) UiSounds.Hover();
        hot = on;
    }

    public void OnPointerEnter(PointerEventData e) => Light(true);
    public void OnPointerExit(PointerEventData e) => Light(false);
    public void OnSelect(BaseEventData e) => Light(true);
    public void OnDeselect(BaseEventData e) => Light(false);
    public void OnPointerClick(PointerEventData e) => Pressed();
    public void OnSubmit(BaseEventData e) => Pressed();

    void Pressed()
    {
        if (sound == Sound.Confirm) UiSounds.Confirm();
        else if (sound == Sound.Back) UiSounds.Back();
        else UiSounds.Click();
    }

    public void Setup(Sound kind, float nudge = 14f)
    {
        sound = kind;
        hoverNudge = nudge;
    }
}
