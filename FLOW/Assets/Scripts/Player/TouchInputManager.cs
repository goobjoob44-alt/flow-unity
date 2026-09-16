using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Flow
{
    public sealed class TouchInputManager : MonoBehaviour
    {
        public Vector2 Stick { get; private set; }
        private readonly IntentBuffer jump = new IntentBuffer();
        private readonly IntentBuffer slide = new IntentBuffer();
        private bool turn;
        private int swipeFinger = -1;
        private Vector2 swipeStart;
        private void OnEnable() { EnhancedTouchSupport.Enable(); }
        private void OnDisable() { EnhancedTouchSupport.Disable(); Clear(); }
        public void SetStick(Vector2 value) { Stick = value.magnitude < 0.15f ? Vector2.zero : value; }
        public void Jump() { jump.Press(Time.time); }
        public void Slide() { slide.Press(Time.time); }
        public bool HasJump(float window) => jump.Pending(Time.time, window);
        public void AcceptJump() { jump.Clear(); }
        public bool ConsumeSlide(float window) => slide.Consume(Time.time, window);
        public bool ConsumeTurn() { bool value = turn; turn = false; return value; }
        public void Clear() { Stick = Vector2.zero; jump.Clear(); slide.Clear(); turn = false; swipeFinger = -1; }
        private void OnApplicationFocus(bool focus) { if (!focus) Clear(); }
        private void Update()
        {
            var touches = Touch.activeTouches;
            for (int i = 0; i < touches.Count; i++)
            {
                Touch touch = touches[i];
                Vector2 point = touch.screenPosition;
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && point.x > Screen.width * 0.5f && point.y > Screen.height * 0.4f && point.y < Screen.height * 0.8f)
                { swipeFinger = touch.finger.index; swipeStart = point; }
                if (touch.finger.index != swipeFinger) continue;
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    Vector2 distance = point - swipeStart;
                    turn = distance.y > Screen.height * 0.14f && Mathf.Abs(distance.x) < distance.y * 0.6f;
                    swipeFinger = -1;
                }
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled) swipeFinger = -1;
            }
        }
    }
}
