using UnityEngine;
using UnityEngine.EventSystems;

namespace Flow
{
    public sealed class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform area;
        private RectTransform knob;
        private RectTransform disc;
        private TouchInputManager input;
        private Vector2 origin;
        private int owner = int.MinValue;
        private const float Radius = 64f;
        public void Configure(TouchInputManager target, RectTransform handle)
        { input = target; area = (RectTransform)transform; knob = handle; disc = (RectTransform)handle.parent; }
        public void OnPointerDown(PointerEventData data)
        {
            if (owner != int.MinValue) return;
            owner = data.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, data.position, data.pressEventCamera, out origin);
            origin.x = Mathf.Clamp(origin.x, area.rect.xMin + Radius, area.rect.xMax - Radius);
            origin.y = Mathf.Clamp(origin.y, area.rect.yMin + Radius, area.rect.yMax - Radius);
            disc.anchoredPosition = origin;
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != owner) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, data.position, data.pressEventCamera, out Vector2 local);
            Vector2 value = Vector2.ClampMagnitude((local - origin) / Radius, 1f);
            input.SetStick(value);
            knob.anchoredPosition = value * Radius * 0.65f;
        }
        public void OnPointerUp(PointerEventData data) { if (data.pointerId == owner) ResetStick(); }
        private void OnDisable() { ResetStick(); }
        private void ResetStick()
        {
            owner = int.MinValue;
            if (input != null) input.SetStick(Vector2.zero);
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (disc != null) disc.anchoredPosition = Vector2.zero;
        }
    }
}
