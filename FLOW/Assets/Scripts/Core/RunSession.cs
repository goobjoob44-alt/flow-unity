using System.Collections;
using UnityEngine;

namespace Flow
{
    public sealed class RunSession : MonoBehaviour
    {
        [SerializeField] private ParkourController player;
        [SerializeField] private GhostReplay replay;
        [SerializeField] private HUDController hud;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private Transform[] shards;
        [SerializeField] private Transform antenna;
        private bool[] collected;
        private bool countingDown;
        private float countdown;
        private int lastCount;
        private int tutorialStage;
        public RunMetrics Metrics { get; } = new RunMetrics();
        public float Progress => Mathf.Clamp01(player.transform.position.z / ChunkGenerator.CourseLength);
        public bool CountingDown => countingDown;
        private static readonly float[] TutorialDistances = { 22f, 42f, 82f, 122f, 160f };
        private static readonly string[] TutorialHints = {
            "Gap ahead. Tap JUMP before the edge.",
            "Low clearance. Tap SLIDE to keep your momentum.",
            "Try the red wall or AC unit. JUMP adapts to your route.",
            "Overhead cable. Tap JUMP beneath it to ride.",
            "Your line now. Side rooftops hold hidden data shards."
        };
        private int shardCount;
        private float checkpointZ;
        private float antennaWait;
        private bool radioFound;
        public float Elapsed { get; private set; }
        public int Shards => shardCount;
        public bool Ready { get; private set; }
        public bool HasActiveRun { get; private set; }
        public bool IsTimeTrial => replay.IsTimeTrial;
        public void Configure(ParkourController runner, GhostReplay ghost, HUDController display, AudioManager sound, Transform[] pickups, Transform radio)
        { player = runner; replay = ghost; hud = display; audioManager = sound; shards = pickups; antenna = radio; }
        private IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            yield return RunSettings.Load();
            collected = new bool[shards.Length];
            player.ActionPerformed += OnAction;
            Ready = true;
            hud.ShowMenu(false, 0f, 0f);
        }
        public void Begin()
        {
            if (!Ready) return;
            Time.timeScale = 1f;
            Elapsed = checkpointZ = antennaWait = 0f;
            radioFound = false;
            shardCount = 0;
            for (int i = 0; i < shards.Length; i++) { collected[i] = false; shards[i].gameObject.SetActive(true); }
            player.SetCheckpoint(new Vector3(0f, 0.1f, 3f));
            player.Recover();
            Metrics.Reset();
            tutorialStage = 0;
            player.Playing = false;
            HasActiveRun = true;
            countingDown = true;
            countdown = 3f;
            lastCount = 3;
            replay.Begin();
            hud.ShowRun();
            hud.Notify("3 / FIND YOUR LINE", 1.1f);
        }
        public void Pause()
        {
            if (!HasActiveRun) return;
            player.Playing = false;
            Time.timeScale = 0f;
            hud.ShowPause();
        }
        public void Resume()
        {
            if (!HasActiveRun) return;
            Time.timeScale = 1f;
            player.Playing = !countingDown;
            hud.ShowRun();
        }
        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            player.Playing = HasActiveRun = countingDown = false;
            replay.Hide();
            hud.ShowMenu(false, 0f, 0f);
        }
        private void OnAction(ParkourState state)
        {
            if (!player.Playing) return;
            Metrics.Record(state);
            if (state == ParkourState.Recovery) hud.Notify("Back on your line. Keep moving.", 2f);
        }
        private void OnDestroy() { if (player != null) player.ActionPerformed -= OnAction; Time.timeScale = 1f; }
        private void OnApplicationPause(bool paused) { if (paused && HasActiveRun) Pause(); }
        private void OnApplicationFocus(bool focused) { if (!focused && HasActiveRun) Pause(); }
        private void Update()
        {
            if (countingDown && Time.timeScale > 0f)
            {
                countdown -= Time.deltaTime;
                int count = Mathf.CeilToInt(countdown);
                if (count > 0 && count != lastCount) { lastCount = count; hud.Notify(count + " / FIND YOUR LINE", 1.1f); }
                if (countdown <= 0f)
                {
                    countingDown = false;
                    player.Playing = true;
                    hud.Notify("GO / Left thumb steers. Tap JUMP at the striped fence.", 5f);
                }
                return;
            }
            if (!player.Playing) return;
            Elapsed += Time.deltaTime;
            Metrics.Tick(Time.deltaTime, player.Momentum.Speed, player.Momentum.InFlow);
            replay.Tick(Elapsed);
            Vector3 position = player.transform.position;
            if (position.z > checkpointZ + 40f && player.Grounded && player.State == ParkourState.Run && position.y > -0.2f)
            {
                checkpointZ = Mathf.Floor(position.z / 40f) * 40f;
                player.SetCheckpoint(position + Vector3.up * 0.05f, player.transform.eulerAngles.y);
            }
            for (int i = 0; i < shards.Length; i++)
            {
                if (collected[i]) continue;
                shards[i].Rotate(0f, 55f * Time.deltaTime, 0f);
                if ((position + Vector3.up - shards[i].position).sqrMagnitude > 3.2f) continue;
                collected[i] = true;
                shardCount++;
                Metrics.CollectShard();
                shards[i].gameObject.SetActive(false);
                audioManager.Shard();
                hud.Notify("DATA SHARD " + shardCount + " / 3", 2.5f);
            }
            if (!radioFound && (position - antenna.position).sqrMagnitude < 16f && player.Momentum.Speed < 0.5f)
            {
                antennaWait += Time.deltaTime;
                if (antennaWait >= 3f)
                {
                    radioFound = true;
                    audioManager.EasterEgg();
                    hud.Notify("HIDDEN FREQUENCY / You found the rooftop radio.", 5f);
                }
            }
            else antennaWait = 0f;
            if (tutorialStage < TutorialDistances.Length && position.z >= TutorialDistances[tutorialStage])
            {
                hud.Notify(TutorialHints[tutorialStage], 5f);
                tutorialStage++;
            }
            if (position.z >= 780f && Mathf.Abs(position.x) > 6f)
                hud.Notify("Finish gate: return to the center rooftop.", 1f);
            if (position.z >= 793f && Mathf.Abs(position.x) <= 6f && position.y >= -0.2f && position.y < 3.5f)
            {
                player.Playing = false;
                HasActiveRun = false;
                replay.Finish(Elapsed);
                hud.ShowMenu(true, Elapsed, replay.BestTime);
            }
        }
    }
}
