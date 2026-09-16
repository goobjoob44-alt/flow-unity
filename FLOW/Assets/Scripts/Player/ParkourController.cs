using UnityEngine;

namespace Flow
{
    public enum ParkourState { Idle, Run, Jump, Fall, Vault, Slide, WallRun, Climb, ZipLine, Roll, Crouch, Land, HardLand, Recovery }

    [RequireComponent(typeof(CharacterController), typeof(MomentumSystem), typeof(ContextDetector))]
    public sealed class ParkourController : MonoBehaviour
    {
        [SerializeField] private TouchInputManager input;
        private CharacterController body;
        private ContextDetector detector;
        private MomentumSystem momentum;
        private Vector3 velocity;
        private Vector3 checkpoint;
        private Vector3 traversalStart;
        private Vector3 traversalEnd;
        private Vector3 wallNormal;
        private float checkpointHeading;
        private float stateTime;
        private float traversalDuration;
        private float bufferedRoll = float.NegativeInfinity;
        private float lastGrounded = float.NegativeInfinity;
        private float recoveryTime;
        private float stalledTime;
        private float turnProgress = 1f;
        private bool wasGrounded;
        public bool Playing { get; set; }
        public ParkourState State { get; private set; }
        public ParkourContext Context { get; private set; }
        public MomentumSystem Momentum => momentum;
        public System.Action<ParkourState> ActionPerformed;
        private bool Traversing => State == ParkourState.Vault || State == ParkourState.Climb || State == ParkourState.ZipLine;
        private bool Low => State == ParkourState.Slide || State == ParkourState.Roll || State == ParkourState.Crouch;
        public void Configure(TouchInputManager value) { input = value; }
        private void Awake()
        {
            body = GetComponent<CharacterController>();
            detector = GetComponent<ContextDetector>();
            momentum = GetComponent<MomentumSystem>();
            checkpoint = transform.position;
        }
        public void SetCheckpoint(Vector3 position, float heading = 0f)
        { checkpoint = position; checkpointHeading = heading; }
        public void Recover()
        {
            body.enabled = false;
            body.height = 1.8f;
            body.center = Vector3.up * 0.9f;
            transform.SetPositionAndRotation(checkpoint, Quaternion.Euler(0f, checkpointHeading, 0f));
            body.enabled = true;
            velocity = Vector3.zero;
            recoveryTime = stalledTime = 0f;
            turnProgress = 1f;
            lastGrounded = bufferedRoll = float.NegativeInfinity;
            wasGrounded = false;
            detector.ResetContext();
            input.Clear();
            momentum.Recover();
            Enter(ParkourState.Recovery);
        }
        private void Update()
        {
            if (!Playing) { input.Clear(); return; }
            float delta = Mathf.Min(Time.deltaTime, 0.05f);
            stateTime += delta;
            if (transform.position.y < -8f) { Recover(); return; }
            if (Traversing) { Traverse(); return; }
            bool grounded = body.isGrounded;
            if (grounded && transform.position.y < -2f)
            {
                recoveryTime += delta;
                if (recoveryTime >= 2f) { Recover(); return; }
            }
            else recoveryTime = 0f;
            if (grounded) lastGrounded = Time.time;
            Context = detector.Scan(grounded, momentum.Speed);
            if (input.ConsumeTurn() && turnProgress >= 1f) turnProgress = 0f;
            if (turnProgress < 1f)
            {
                float beforeTurn = Mathf.SmoothStep(0f, 1f, turnProgress);
                turnProgress = Mathf.Min(1f, turnProgress + delta / MovementContract.QuickTurnDuration);
                transform.Rotate(0f, 180f * (Mathf.SmoothStep(0f, 1f, turnProgress) - beforeTurn), 0f);
            }
            transform.Rotate(0f, input.Stick.x * momentum.Tuning.TurnSpeed * (grounded ? 1f : momentum.Tuning.AirControl) * delta, 0f);
            bool automatic = RunSettings.Current.autoRun || RunSettings.Current.oneHanded;
            float throttle = automatic ? 1f : Mathf.Max(0f, input.Stick.y);
            if (input.Stick.y < -0.3f) throttle = 0f;
            momentum.Tick(throttle, automatic || input.Stick.magnitude >= 0.8f, delta);
            if (input.ConsumeSlide(momentum.RollWindow))
            {
                if (!grounded) bufferedRoll = Time.time;
                else Enter(momentum.Speed > 3f ? ParkourState.Slide : ParkourState.Crouch);
            }
            if (grounded && !wasGrounded && State != ParkourState.Recovery)
            {
                if (MovementContract.Within(Time.time, bufferedRoll, momentum.RollWindow))
                { bufferedRoll = float.NegativeInfinity; momentum.Retain(0.98f); Enter(ParkourState.Roll); }
                else if (velocity.y < -12f) { momentum.Retain(0.6f); Enter(ParkourState.HardLand); }
                else if (!Low) Enter(ParkourState.Land);
            }
            float buffer = Context.Kind == ContextType.Vault ? MovementContract.VaultBuffer : MovementContract.JumpBuffer;
            if (input.HasJump(MovementContract.Window(buffer, momentum.Assisted)) && Jump(grounded)) input.AcceptJump();
            if (Traversing) return;
            if (Low && stateTime > (State == ParkourState.Crouch ? 1.4f : 0.85f) && CanStand()) Enter(ParkourState.Run);
            body.height = Low ? 0.85f : 1.8f;
            body.center = Vector3.up * (body.height * 0.5f);
            if (State == ParkourState.WallRun)
            {
                if (grounded || stateTime > 1.1f || Context.Kind != ContextType.WallJump) Enter(grounded ? ParkourState.Run : ParkourState.Fall);
                else velocity.y = Mathf.Max(velocity.y - momentum.Tuning.Gravity * 0.15f * delta, -1f);
            }
            else velocity.y = grounded && velocity.y < 0f ? -2f : velocity.y - momentum.Tuning.Gravity * delta;
            if (!grounded && State != ParkourState.WallRun && !Low) SetState(velocity.y > 0f ? ParkourState.Jump : ParkourState.Fall);
            if (grounded && !Low && State != ParkourState.Jump && State != ParkourState.WallRun)
            {
                bool settling = (State == ParkourState.Land && stateTime < 0.08f) || (State == ParkourState.HardLand && stateTime < 0.3f) || (State == ParkourState.Recovery && stateTime < 0.1f);
                if (!settling) SetState(momentum.Speed > 0.1f ? ParkourState.Run : ParkourState.Idle);
            }
            Vector3 horizontal = transform.forward * momentum.Speed + new Vector3(velocity.x, 0f, velocity.z);
            if (State == ParkourState.WallRun) horizontal = Vector3.ProjectOnPlane(horizontal, wallNormal) - wallNormal * 0.7f;
            Vector3 before = transform.position;
            CollisionFlags flags = body.Move((horizontal + Vector3.up * velocity.y) * delta);
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, delta * 7f);
            velocity.z = Mathf.MoveTowards(velocity.z, 0f, delta * 7f);
            if ((flags & CollisionFlags.Above) != 0) velocity.y = Mathf.Min(velocity.y, 0f);
            bool stalled = grounded && throttle > 0f && momentum.Speed > 1f && Vector3.Distance(before, transform.position) < momentum.Speed * delta * 0.2f;
            stalledTime = stalled ? stalledTime + delta : 0f;
            if (stalledTime > 0.25f && stalledTime <= 0.25f + delta) momentum.Retain(0.6f);
            if (stalledTime > 2f) { Recover(); return; }
            wasGrounded = grounded;
        }
        private bool CanStand()
        {
            Vector3 feet = transform.position;
            return !Physics.CheckCapsule(feet + Vector3.up * 0.31f, feet + Vector3.up * 1.49f, 0.27f, 1, QueryTriggerInteraction.Ignore);
        }
        private bool Jump(bool grounded)
        {
            if (Low && !CanStand()) return false;
            switch (Context.Kind)
            {
                case ContextType.Climb: BeginTraversal(ParkourState.Climb, Context.Target, 1f); return true;
                case ContextType.Vault: BeginTraversal(ParkourState.Vault, Context.Target, 0.65f); return true;
                case ContextType.Grab: BeginTraversal(ParkourState.ZipLine, Context.Target, Vector3.Distance(transform.position, Context.Target) / 12f); return true;
                case ContextType.WallJump:
                    if (State != ParkourState.WallRun) { wallNormal = Context.Normal; Enter(ParkourState.WallRun); velocity.y = 1f; }
                    else { velocity = wallNormal * 5f + Vector3.up * 7f; Enter(ParkourState.Jump); }
                    return true;
            }
            if (!grounded && !MovementContract.Within(Time.time, lastGrounded, MovementContract.Window(MovementContract.CoyoteTime, momentum.Assisted))) return false;
            velocity.y = Mathf.Sqrt(2f * momentum.Tuning.Gravity * momentum.Tuning.JumpHeight);
            lastGrounded = float.NegativeInfinity;
            Enter(ParkourState.Jump);
            return true;
        }
        private void BeginTraversal(ParkourState state, Vector3 target, float duration)
        {
            traversalStart = transform.position;
            traversalEnd = target;
            traversalDuration = Mathf.Max(0.2f, duration);
            velocity = Vector3.zero;
            momentum.Retain(momentum.Assisted ? 0.88f : 0.96f);
            Enter(state);
        }
        private void Traverse()
        {
            float progress = Mathf.Clamp01(stateTime / traversalDuration);
            Vector3 target;
            if (State == ParkourState.Climb)
            {
                float rise = Mathf.Clamp01(progress / 0.55f);
                float forward = Mathf.Clamp01((progress - 0.45f) / 0.55f);
                target = Vector3.Lerp(traversalStart, traversalEnd, forward);
                target.y = Mathf.Lerp(traversalStart.y, traversalEnd.y + 0.12f, rise);
            }
            else
            {
                target = Vector3.Lerp(traversalStart, traversalEnd, progress);
                if (State == ParkourState.Vault) target.y += Mathf.Sin(progress * Mathf.PI) * 1.25f;
            }
            body.Move(target - transform.position);
            if (progress < 1f) return;
            if (Vector3.Distance(transform.position, traversalEnd) < 0.35f)
            { wasGrounded = false; detector.ResetContext(); Enter(ParkourState.Fall); }
            else if (stateTime > traversalDuration + 0.45f) Recover();
        }
        private void SetState(ParkourState value) { if (State != value) Enter(value); }
        private void Enter(ParkourState value) { State = value; stateTime = 0f; ActionPerformed?.Invoke(value); }
    }
}
