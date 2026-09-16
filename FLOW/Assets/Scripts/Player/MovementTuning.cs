using UnityEngine;

namespace Flow
{
    [CreateAssetMenu(menuName = "FLOW/Movement Tuning")]
    public sealed class MovementTuning : ScriptableObject
    {
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float sprintSpeed = 9f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float braking = 12f;
        [SerializeField] private float gravity = 19.62f;
        [SerializeField] private float jumpHeight = 2.2f;
        [SerializeField] private float turnSpeed = 95f;
        [SerializeField] private float airControl = 0.4f;
        public float RunSpeed => runSpeed;
        public float SprintSpeed => sprintSpeed;
        public float Acceleration => acceleration;
        public float Braking => braking;
        public float Gravity => gravity;
        public float JumpHeight => jumpHeight;
        public float TurnSpeed => turnSpeed;
        public float AirControl => airControl;
    }
}
