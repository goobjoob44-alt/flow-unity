using UnityEngine;

namespace Flow
{
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private ParkourController runner;
        private AudioSource source;
        private AudioSource music;
        private AudioClip footstep;
        private AudioClip jump;
        private AudioClip land;
        private AudioClip shard;
        private float nextStep;
        public void Configure(ParkourController value) { runner = value; }
        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0.35f;
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0f;
            footstep = Tone("Step", 75f, 0.065f, 0.5f);
            jump = Tone("Air", 160f, 0.15f, 0.15f);
            land = Tone("Land", 55f, 0.16f, 0.4f);
            shard = Tone("Shard", 880f, 0.35f, 0f);
            music.clip = Music();
            music.Play();
            runner.ActionPerformed += OnAction;
        }
        private void OnDestroy() { if (runner != null) runner.ActionPerformed -= OnAction; }
        private void OnAction(ParkourState state)
        {
            if (state == ParkourState.Jump || state == ParkourState.Vault || state == ParkourState.Climb) source.PlayOneShot(jump);
            if (state == ParkourState.Roll || state == ParkourState.Slide || state == ParkourState.Land || state == ParkourState.HardLand) source.PlayOneShot(land);
        }
        public void Shard() { source.PlayOneShot(shard); }
        public void EasterEgg() { source.PlayOneShot(shard, 0.5f); }
        private void Update()
        {
            music.volume = Mathf.MoveTowards(music.volume, runner.Playing ? 0.07f + runner.Momentum.Normalized * 0.16f : 0f, Time.deltaTime * 0.15f);
            if (!runner.Playing || runner.State != ParkourState.Run || runner.Momentum.Speed < 1f) return;
            if (Time.time < nextStep) return;
            nextStep = Time.time + Mathf.Lerp(0.48f, 0.25f, runner.Momentum.Normalized);
            source.pitch = 0.92f + Mathf.PingPong(Time.time * 0.13f, 0.15f);
            source.PlayOneShot(footstep, 0.5f);
        }
        private static AudioClip Tone(string name, float frequency, float duration, float noise)
        {
            const int rate = 22050;
            float[] samples = new float[(int)(duration * rate)];
            var random = new System.Random(72);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Sin(Mathf.PI * i / samples.Length) * Mathf.Exp(-t * 12f);
                samples[i] = (Mathf.Sin(t * frequency * 2f * Mathf.PI) * (1f - noise) + ((float)random.NextDouble() * 2f - 1f) * noise) * envelope * 0.35f;
            }
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private static AudioClip Music()
        {
            const int rate = 22050;
            const int seconds = 32;
            float[] samples = new float[rate * seconds];
            float[] notes = { 110f, 130.81f, 164.81f, 146.83f, 110f, 164.81f, 130.81f, 98f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float f = notes[(int)(t / 4f) % notes.Length];
                float phrase = Mathf.Sin(Mathf.PI * (t % 4f) / 4f);
                float pulse = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t * 2f);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 1.5f * t)) * phrase * pulse * 0.2f;
            }
            AudioClip clip = AudioClip.Create("Original FLOW ambient sequence", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
