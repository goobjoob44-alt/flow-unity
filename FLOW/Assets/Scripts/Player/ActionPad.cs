using UnityEngine;
using UnityEngine.EventSystems;

namespace Flow
{
    public sealed class ActionPad : MonoBehaviour, IPointerDownHandler
    {
        private System.Action action;
        public void Configure(System.Action value) { action = value; }
        public void OnPointerDown(PointerEventData data) { action?.Invoke(); }
    }
}
