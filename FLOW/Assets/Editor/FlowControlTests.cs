using System;
using Flow;
using UnityEditor;
using UnityEngine;

public static class FlowControlTests
{
    private static int checks;
    [MenuItem("FLOW/Run control checks")]
    public static void Run()
    {
        checks = 0;
        var intent = new IntentBuffer();
        Require(!intent.Pending(0f, MovementContract.JumpBuffer), "New intent must be empty");
        intent.Press(1f);
        Require(intent.Pending(1.13f, MovementContract.JumpBuffer), "Jump must survive a 130ms delay");
        Require(!intent.Pending(1.15f, MovementContract.JumpBuffer), "Jump must expire after 140ms");
        Require(intent.Consume(1.12f, MovementContract.JumpBuffer), "Buffered jump must be consumable");
        Require(!intent.Consume(1.12f, MovementContract.JumpBuffer), "One press cannot trigger two jumps");
        intent.Press(2f); intent.Clear();
        Require(!intent.Pending(2f, 1f), "Pause/recovery must discard stale actions");
        Require(MovementContract.Within(1.09f, 1f, MovementContract.CoyoteTime), "Coyote jump at 90ms");
        Require(!MovementContract.Within(1.11f, 1f, MovementContract.CoyoteTime), "Coyote must expire after 100ms");
        Require(!MovementContract.Within(0f, 1f, 1f), "Future timestamps must not be accepted");
        Require(Mathf.Abs(MovementContract.Window(1f, true) - 1.35f) < 0.0001f, "Assist must expand windows by 35 percent");
        Require(MovementContract.Within(0.21f, 0f, MovementContract.RollBuffer), "Landing roll buffer is 220ms");
        Require(!MovementContract.Within(0.23f, 0f, MovementContract.RollBuffer), "Expired roll must not fire");
        Require(MovementContract.ContextLock == 0.12f, "Context lock is 120ms");
        Require(MovementContract.QuickTurnDuration >= 0.25f && MovementContract.QuickTurnDuration <= 0.35f, "Quick turn timing contract");

        MovementTuning tuning = ScriptableObject.CreateInstance<MovementTuning>();
        Require(tuning.SprintSpeed / tuning.Acceleration <= 0.8f, "Default acceleration must reach cruise in 800ms");
        float apex = Mathf.Sqrt(2f * tuning.Gravity * tuning.JumpHeight) / tuning.Gravity;
        Require(apex >= 0.45f && apex <= 0.6f, "Default jump apex must be 450–600ms");
        UnityEngine.Object.DestroyImmediate(tuning);

        GhostRun valid = new GhostRun { schema = GhostReplay.SchemaVersion, build = "test", chunk = GhostReplay.ChunkRevision, tuning = "standard", duration = 1f,
            frames = new[] { new GhostFrame { time = 0f, rotation = Quaternion.identity }, new GhostFrame { time = 1f, position = Vector3.forward, rotation = Quaternion.identity } } };
        Require(GhostReplay.Validate(valid, "test", "standard"), "Valid ghost must load");
        Require(!GhostReplay.Validate(valid, "other-build", "standard"), "Cross-build ghosts must be rejected");
        Require(!GhostReplay.Validate(valid, "test", "assisted"), "Cross-tuning ghosts must be rejected");
        valid.frames[1].time = 0f;
        Require(!GhostReplay.Validate(valid, "test", "standard"), "Duplicate sample times must be rejected");
        valid.frames[1].time = 1f;
        valid.frames[1].position = new Vector3(float.NaN, 0f, 0f);
        Require(!GhostReplay.Validate(valid, "test", "standard"), "Non-finite transforms must be rejected");
        valid.frames[1].position = Vector3.forward;
        valid.frames[1].rotation = new Quaternion(0f, 0f, 0f, 0f);
        Require(!GhostReplay.Validate(valid, "test", "standard"), "Zero quaternions must be rejected");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chunks/Sector7.prefab");
        Require(prefab != null, "Generate scenes with FLOW / Build vertical slice before running checks");
        int triangles = 0, shards = 0;
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>()) if (filter.sharedMesh != null) triangles += filter.sharedMesh.triangles.Length / 3;
        foreach (Transform part in prefab.GetComponentsInChildren<Transform>()) if (part.name.StartsWith("Data shard ", StringComparison.Ordinal)) shards++;
        Require(triangles < 50000, "Graybox geometry must stay below 50k triangles");
        Require(shards == 3, "Serialized chunk prefab must include three data shards");
        Require(prefab.GetComponentsInChildren<ParkourSurface>().Length > 0, "Chunk must include authored traversal surfaces");
        Require(EditorBuildSettings.scenes.Length == 2, "Both native scenes must be registered");
        Debug.Log("FLOW: " + checks + " editor control/asset checks passed. These checks do not validate traversal, route advantage, mobile builds, FPS, thermal performance, or usability.");
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("FLOW control check failed: " + message);
        checks++;
    }
}
