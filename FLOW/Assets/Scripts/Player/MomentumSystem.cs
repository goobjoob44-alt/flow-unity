using UnityEngine;

namespace Flow
{
    public sealed class MomentumSystem : MonoBehaviour
    {
        [SerializeField] private MovementTuning tuning;
        public float Speed { get; private set; }
        public float Normalized => tuning == null ? 0f : Speed / tuning.SprintSpeed;
        public bool InFlow => Normalized >= 0.94f;
        public bool Assisted => RunSettings.Current.flowAssist || RunSettings.Current.oneHanded;
        public float RollWindow => MovementContract.Window(MovementContract.RollBuffer, Assisted);
        public void Retain(float fraction) { Speed *= Mathf.Clamp01(fraction); }
        public MovementTuning Tuning => tuning;
        public void Configure(MovementTuning value) { tuning = value; }
        public void Tick(float throttle, bool sprint, float delta)
        {
            float target = Mathf.Clamp01(throttle) * (sprint ? tuning.SprintSpeed : tuning.RunSpeed);
            Speed = Mathf.MoveTowards(Speed, target, (target > Speed ? tuning.Acceleration : tuning.Braking) * delta);
        }
        public void BreakFlow() { Speed = Mathf.Min(Speed, tuning.RunSpeed * 0.5f); }
        public void Recover() { Speed = tuning.RunSpeed * 0.65f; }
    }
}
