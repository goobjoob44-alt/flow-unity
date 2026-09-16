using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Flow
{
    public sealed class AdaptiveResolution : MonoBehaviour
    {
        private UniversalRenderPipelineAsset pipeline;
        private float originalScale;
        private float elapsed;
        private int frames;
        private void Start()
        {
            pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) return;
            originalScale = pipeline.renderScale;
            pipeline.renderScale = Mathf.Clamp(720f / Mathf.Max(Screen.height, 1), 0.5f, 1f);
        }
        private void Update()
        {
            if (pipeline == null || Time.timeScale == 0f) return;
            elapsed += Time.unscaledDeltaTime; frames++;
            if (elapsed < 3f) return;
            float fps = frames / elapsed;
            float minimum = Mathf.Clamp(540f / Mathf.Max(Screen.height, 1), 0.5f, 1f);
            float maximum = Mathf.Clamp(1080f / Mathf.Max(Screen.height, 1), minimum, 1f);
            if (fps < 53f) pipeline.renderScale = Mathf.Max(minimum, pipeline.renderScale - 0.05f);
            else if (fps > 59f) pipeline.renderScale = Mathf.Min(maximum, pipeline.renderScale + 0.025f);
            elapsed = 0f; frames = 0;
        }
        private void OnDestroy() { if (pipeline != null) pipeline.renderScale = originalScale; }
    }
}
