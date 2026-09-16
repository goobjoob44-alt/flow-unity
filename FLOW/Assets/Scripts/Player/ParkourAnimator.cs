using UnityEngine;

namespace Flow
{
    public sealed class ParkourAnimator : MonoBehaviour
    {
        [SerializeField] private ParkourController player;
        [SerializeField] private Camera view;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        private Vector3 leftBase;
        private Vector3 rightBase;
        public void Configure(ParkourController runner, Camera camera, Transform left, Transform right)
        { player = runner; view = camera; leftArm = left; rightArm = right; }
        private void Start() { leftBase = leftArm.localPosition; rightBase = rightArm.localPosition; }
        private void LateUpdate()
        {
            float speed = player.Momentum.Normalized;
            bool reduced = RunSettings.Current.reducedMotion;
            float swing = reduced || !player.Playing ? 0f : Mathf.Sin(Time.time * (7f + speed * 5f)) * speed;
            bool low = player.State == ParkourState.Slide || player.State == ParkourState.Roll || player.State == ParkourState.Crouch;
            Vector3 cameraPosition = new Vector3(0f, low ? 0.62f : 1.6f, 0f);
            if (!reduced) cameraPosition.y += Mathf.Abs(swing) * 0.018f;
            view.transform.localPosition = Vector3.Lerp(view.transform.localPosition, cameraPosition, Time.deltaTime * 15f);
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, reduced ? 84f : 84f + speed * 5f, Time.deltaTime * 3f);
            Vector3 reach = player.State == ParkourState.Climb || player.State == ParkourState.Vault || player.State == ParkourState.ZipLine ? new Vector3(0f, 0.3f, 0.2f) : Vector3.zero;
            leftArm.localPosition = Vector3.Lerp(leftArm.localPosition, leftBase + reach + new Vector3(0f, swing * 0.035f, swing * 0.065f), Time.deltaTime * 15f);
            rightArm.localPosition = Vector3.Lerp(rightArm.localPosition, rightBase + reach - new Vector3(0f, swing * 0.035f, swing * 0.065f), Time.deltaTime * 15f);
        }
    }
}
