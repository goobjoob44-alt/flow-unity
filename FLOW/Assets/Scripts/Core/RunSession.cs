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
        private readonly bool[] collected = new bool[3];
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
            player.Playing = true;
            HasActiveRun = true;
            replay.Begin();
            hud.ShowRun();
            hud.Notify("Follow the striped red props. JUMP vaults. SLIDE ducks. Find your line.", 10f);
        }
        public void Pause()
        {
            player.Playing = false;
            Time.timeScale = 0f;
            hud.ShowPause();
        }
        public void Resume()
        { Time.timeScale = 1f; player.Playing = true; hud.ShowRun(); }
        private void OnApplicationPause(bool paused) { if (paused && player.Playing) Pause(); }
        private void Update()
        {
            if (!player.Playing) return;
            Elapsed += Time.deltaTime;
            replay.Tick(Elapsed);
            Vector3 position = player.transform.position;
            if (position.z > checkpointZ + 40f && player.State == ParkourState.Run && position.y > -0.2f)
            {
                checkpointZ = Mathf.Floor(position.z / 40f) * 40f;
                player.SetCheckpoint(new Vector3(0f, 0.1f, checkpointZ + 3f));
            }
            for (int i = 0; i < shards.Length; i++)
            {
                if (collected[i]) continue;
                shards[i].Rotate(0f, 55f * Time.deltaTime, 0f);
                if ((position + Vector3.up - shards[i].position).sqrMagnitude > 3.2f) continue;
                collected[i] = true;
                shardCount++;
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
            if (position.z >= 793f)
            {
                player.Playing = false;
                HasActiveRun = false;
                replay.Finish(Elapsed);
                hud.ShowMenu(true, Elapsed, replay.BestTime);
            }
        }
    }
}
