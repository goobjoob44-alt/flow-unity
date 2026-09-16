using UnityEngine;

namespace Flow
{
    public sealed class RunMetrics
    {
        public int Actions { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int Recoveries { get; private set; }
        public int Score { get; private set; }
        public float FlowSeconds { get; private set; }
        public float TopSpeed { get; private set; }
        private float chainRemaining;
        public void Reset()
        {
            Actions = Combo = BestCombo = Recoveries = Score = 0;
            FlowSeconds = TopSpeed = chainRemaining = 0f;
        }
        public void Tick(float delta, float speed, bool inFlow)
        {
            TopSpeed = Mathf.Max(TopSpeed, speed);
            if (inFlow) FlowSeconds += delta;
            chainRemaining = Mathf.Max(0f, chainRemaining - delta);
            if (chainRemaining == 0f) Combo = 0;
        }
        public void Record(ParkourState state)
        {
            if (state == ParkourState.Recovery)
            {
                Recoveries++;
                Combo = 0;
                chainRemaining = 0f;
                return;
            }
            if (state == ParkourState.HardLand) { Combo = 0; chainRemaining = 0f; return; }
            if (state != ParkourState.Jump && state != ParkourState.Vault && state != ParkourState.Climb
                && state != ParkourState.WallRun && state != ParkourState.ZipLine && state != ParkourState.Roll
                && state != ParkourState.Slide) return;
            Actions++;
            Combo++;
            BestCombo = Mathf.Max(BestCombo, Combo);
            chainRemaining = 5f;
            Score += 100 * Mathf.Min(Combo, 5);
        }
        public void CollectShard() { Score += 500; }
    }
}
