using UnityEngine;

namespace Flow
{
    [CreateAssetMenu(menuName = "FLOW/Chunk")]
    public sealed class ChunkData : ScriptableObject
    {
        [SerializeField] private string chunkName = "Sector 7 / REDLINE";
        [SerializeField] private float targetTime = 90f;
        [SerializeField] private Vector3 startPosition = new Vector3(0f, 0.1f, 3f);
        [SerializeField] private Vector3 endPosition = new Vector3(0f, 0.1f, 795f);
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private Vector3[] shards = { new Vector3(0f, 1f, -1f), new Vector3(6.8f, -3f, 97f), new Vector3(10f, 2f, 770f) };
        public string ChunkName => chunkName;
        public float TargetTime => targetTime;
        public Vector3 StartPosition => startPosition;
        public Vector3 EndPosition => endPosition;
        public GameObject ChunkPrefab => chunkPrefab;
        public Vector3[] Shards => shards;
        public void SetPrefab(GameObject value) { chunkPrefab = value; }
    }
}
