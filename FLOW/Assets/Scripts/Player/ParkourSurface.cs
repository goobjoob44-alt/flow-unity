using UnityEngine;

namespace Flow
{
    public enum SurfaceType { Vault, Climb, Wall, ZipLine }

    public sealed class ParkourSurface : MonoBehaviour
    {
        [SerializeField] private SurfaceType kind;
        [SerializeField] private Vector3 exitOffset = new Vector3(0f, 0f, 15f);
        public SurfaceType Kind => kind;
        public Vector3 Exit => transform.position + exitOffset;
        public void Configure(SurfaceType value, Vector3 exit) { kind = value; exitOffset = exit; }
    }
}
