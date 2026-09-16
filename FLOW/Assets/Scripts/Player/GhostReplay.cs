using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Flow
{
    [Serializable] public struct GhostFrame
    { public float time; public Vector3 position; public Quaternion rotation; public ParkourState state; public bool discontinuity; }
    [Serializable] public sealed class GhostRun
    { public int schema; public string build; public string chunk; public string tuning; public float duration; public GhostFrame[] frames; }

    public sealed class GhostReplay : MonoBehaviour
    {
        public const int SchemaVersion = 2;
        public const string ChunkRevision = "redline-graybox-02";
        private const int MaxFrames = 18000;
        [SerializeField] private Transform runner;
        [SerializeField] private Transform ghost;
        private ParkourController controller;
        private readonly List<GhostFrame> recording = new List<GhostFrame>(2400);
        private GhostRun best;
        private string tuningHash;
        private int cursor;
        private float nextSample;
        private bool discontinuity;
        private bool exhausted;
        private bool assistedAtStart;
        private string SavePath => Path.Combine(Application.persistentDataPath, "redline-" + tuningHash + "-best.json");
        public float BestTime => best == null ? 0f : best.duration;
        public bool IsTimeTrial { get; private set; }
        public void Configure(Transform player, Transform visual) { runner = player; ghost = visual; }
        private void Awake()
        {
            controller = runner.GetComponent<ParkourController>();
            controller.ActionPerformed += OnAction;
            ghost.gameObject.SetActive(false);
        }
        private void OnDestroy() { if (controller != null) controller.ActionPerformed -= OnAction; }
        private void OnAction(ParkourState state) { if (state == ParkourState.Recovery) discontinuity = true; }
        public void Begin()
        {
            IsTimeTrial = RunSettings.Current.timeTrial;
            assistedAtStart = controller.Momentum.Assisted;
            string profile = JsonUtility.ToJson(controller.Momentum.Tuning) + "|" + controller.Momentum.Assisted;
            tuningHash = Hash128.Compute(profile).ToString();
            best = null;
            try
            {
                if (IsTimeTrial && File.Exists(SavePath) && new FileInfo(SavePath).Length < 8 * 1024 * 1024)
                {
                    GhostRun loaded = JsonUtility.FromJson<GhostRun>(File.ReadAllText(SavePath));
                    if (Validate(loaded, Application.version, tuningHash)) best = loaded;
                }
            }
            catch (Exception error) when (error is IOException || error is ArgumentException || error is UnauthorizedAccessException)
            { Debug.LogWarning("Ghost load skipped: " + error.Message); }
            recording.Clear(); cursor = 0; nextSample = 0f; discontinuity = exhausted = false;
            if (IsTimeTrial) Sample(0f);
            ghost.gameObject.SetActive(best != null && IsTimeTrial);
        }
        public static bool Validate(GhostRun run, string build, string tuning)
        {
            if (run == null || run.schema != SchemaVersion || run.chunk != ChunkRevision || run.build != build || run.tuning != tuning
                || !Finite(run.duration) || run.duration <= 0f || run.duration > 1800f || run.frames == null || run.frames.Length < 2 || run.frames.Length > MaxFrames) return false;
            float previous = -1f;
            foreach (GhostFrame frame in run.frames)
            {
                Vector3 p = frame.position; Quaternion q = frame.rotation;
                if (!Finite(frame.time) || frame.time < 0f || frame.time <= previous || frame.time > run.duration
                    || !Finite(p.x) || !Finite(p.y) || !Finite(p.z) || p.sqrMagnitude > 4000000f
                    || !Finite(q.x) || !Finite(q.y) || !Finite(q.z) || !Finite(q.w)) return false;
                float norm = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
                if (norm < 0.9f || norm > 1.1f) return false;
                previous = frame.time;
            }
            return run.frames[0].time <= 0.1f && Mathf.Abs(previous - run.duration) < 0.1f;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void Sample(float time)
        {
            if (recording.Count >= MaxFrames) { exhausted = true; return; }
            recording.Add(new GhostFrame { time = time, position = runner.position, rotation = runner.rotation, state = controller.State, discontinuity = discontinuity });
            discontinuity = false;
            nextSample = time + 0.05f;
        }
        public void Tick(float elapsed)
        {
            if (!IsTimeTrial) return;
            if (controller.Momentum.Assisted != assistedAtStart) exhausted = true;
            if (!exhausted && elapsed >= nextSample) Sample(elapsed);
            if (best == null) return;
            if (elapsed >= best.duration) { ghost.gameObject.SetActive(false); return; }
            while (cursor + 1 < best.frames.Length && best.frames[cursor + 1].time <= elapsed) cursor++;
            GhostFrame a = best.frames[cursor];
            GhostFrame b = best.frames[Mathf.Min(cursor + 1, best.frames.Length - 1)];
            float blend = b.discontinuity ? 0f : Mathf.InverseLerp(a.time, b.time, elapsed);
            ghost.SetPositionAndRotation(Vector3.Lerp(a.position, b.position, blend), Quaternion.Slerp(a.rotation, b.rotation, blend));
        }
        public void Finish(float duration)
        {
            ghost.gameObject.SetActive(false);
            if (!IsTimeTrial || exhausted || recording.Count < 2 || (best != null && best.duration <= duration)) return;
            if (recording[recording.Count - 1].time < duration) Sample(duration);
            if (exhausted) return;
            GhostRun candidate = new GhostRun { schema = SchemaVersion, build = Application.version, chunk = ChunkRevision, tuning = tuningHash, duration = duration, frames = recording.ToArray() };
            if (!Validate(candidate, Application.version, tuningHash)) return;
            best = candidate;
            try
            {
                string temporary = SavePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(best));
                if (File.Exists(SavePath)) File.Replace(temporary, SavePath, null);
                else File.Move(temporary, SavePath);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            { Debug.LogWarning("Ghost is available this session but could not be saved: " + error.Message); }
        }
    }
}
