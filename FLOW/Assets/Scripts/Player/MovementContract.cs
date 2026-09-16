namespace Flow
{
    public static class MovementContract
    {
        public const float JumpBuffer = 0.140f;
        public const float VaultBuffer = 0.180f;
        public const float RollBuffer = 0.220f;
        public const float CoyoteTime = 0.100f;
        public const float ContextLock = 0.120f;
        public const float QuickTurnDuration = 0.300f;
        public static float Window(float duration, bool assisted) => duration * (assisted ? 1.35f : 1f);
        public static bool Within(float now, float pressed, float window)
        {
            float age = now - pressed;
            return age >= 0f && age <= window;
        }
    }

    public sealed class IntentBuffer
    {
        private float pressed = float.NegativeInfinity;
        public void Press(float now) { pressed = now; }
        public bool Pending(float now, float window) => MovementContract.Within(now, pressed, window);
        public void Clear() { pressed = float.NegativeInfinity; }
        public bool Consume(float now, float window)
        {
            if (!Pending(now, window)) return false;
            Clear();
            return true;
        }
    }
}
