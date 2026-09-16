using UnityEngine;

namespace Flow
{
    public enum ContextType { None, Jump, Vault, Climb, WallJump, Grab, Slide, Roll, Crouch }
    public struct ParkourContext
    {
        public ContextType Kind;
        public Vector3 Target;
        public Vector3 Normal;
        public ParkourSurface Surface;
        public float Score;
    }

    public sealed class ContextDetector : MonoBehaviour
    {
        [SerializeField] private LayerMask geometry = 1;
        private readonly ParkourContext[] candidates = new ParkourContext[4];
        private ParkourContext selected;
        private float lockedUntil;
        private float nextScan;
        private bool previousGrounded;
        private int count;
        public void ResetContext() { selected = default; lockedUntil = nextScan = 0f; }

        public ParkourContext Scan(bool grounded, float speed = 0f)
        {
            if (Time.time < nextScan && previousGrounded == grounded) return selected;
            previousGrounded = grounded;
            nextScan += (Mathf.Floor((Time.time - nextScan) * 60f) + 1f) / 60f;
            count = 0;
            Vector3 feet = transform.position;
            Vector3 forward = transform.forward;
            float reach = Mathf.Clamp(1.7f + speed * 0.08f, 1.7f, 2.5f);
            if (Physics.Raycast(feet + Vector3.up * 0.65f, forward, out RaycastHit front, reach, geometry, QueryTriggerInteraction.Ignore)
                && front.collider.TryGetComponent(out ParkourSurface surface))
            {
                Bounds bounds = front.collider.bounds;
                float height = bounds.max.y - feet.y;
                float depth = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z))) * 2f;
                if (surface.Kind == SurfaceType.Climb && height > 0.3f && height <= 2.2f)
                {
                    Vector3 target = front.point + forward * 0.55f;
                    target.y = bounds.max.y + 0.06f;
                    if (ClearAt(target)) Add(ContextType.Climb, surface, target, front.normal, front.distance, speed, 0.9f);
                }
                if (surface.Kind == SurfaceType.Vault && height > 0.1f && height <= 1.2f)
                {
                    Vector3 target = front.point + forward * (depth + 0.65f);
                    target.y = feet.y;
                    if (ClearAt(target)) Add(ContextType.Vault, surface, target, front.normal, front.distance, speed, 1f);
                }
            }
            if (!grounded)
            {
                ProbeWall(transform.right, speed);
                ProbeWall(-transform.right, speed);
            }
            if (Physics.Raycast(feet + Vector3.up * 1.25f, Vector3.up, out RaycastHit overhead, 2f, geometry, QueryTriggerInteraction.Ignore)
                && overhead.collider.TryGetComponent(out ParkourSurface zip) && zip.Kind == SurfaceType.ZipLine && ClearAt(zip.Exit))
                Add(ContextType.Grab, zip, zip.Exit, Vector3.zero, overhead.distance, speed, 0.85f);

            ParkourContext best = new ParkourContext { Kind = grounded ? ContextType.Jump : ContextType.None };
            for (int i = 0; i < count; i++)
            {
                ParkourContext candidate = candidates[i];
                if (candidate.Surface == selected.Surface && candidate.Kind == selected.Kind && Time.time < lockedUntil)
                { selected = candidate; return selected; }
                if (candidate.Score > best.Score) best = candidate;
            }
            if (best.Surface != selected.Surface || best.Kind != selected.Kind) lockedUntil = Time.time + MovementContract.ContextLock;
            selected = best;
            return selected;
        }
        private bool ClearAt(Vector3 feet)
        {
            return !Physics.CheckCapsule(feet + Vector3.up * 0.31f, feet + Vector3.up * 1.49f, 0.27f, geometry, QueryTriggerInteraction.Ignore);
        }
        private void Add(ContextType kind, ParkourSurface surface, Vector3 target, Vector3 normal, float distance, float speed, float priority)
        {
            if (count == candidates.Length) return;
            float contactTime = distance / Mathf.Max(1f, speed);
            float alignment = kind == ContextType.WallJump ? 1f - Mathf.Abs(Vector3.Dot(transform.forward, normal)) : 1f;
            candidates[count++] = new ParkourContext { Kind = kind, Surface = surface, Target = target, Normal = normal,
                Score = priority * 0.4f + alignment * 0.2f + 0.2f / (1f + distance) + 0.2f / (1f + contactTime) };
        }
        private void ProbeWall(Vector3 direction, float speed)
        {
            if (Physics.Raycast(transform.position + Vector3.up, direction, out RaycastHit hit, 0.9f, geometry, QueryTriggerInteraction.Ignore)
                && hit.collider.TryGetComponent(out ParkourSurface surface) && surface.Kind == SurfaceType.Wall)
                Add(ContextType.WallJump, surface, Vector3.zero, hit.normal, hit.distance, speed, 0.8f);
        }
    }
}
