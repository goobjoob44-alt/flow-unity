using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Flow
{
    [Serializable]
    public sealed class RunSettings
    {
        public bool autoRun = true;
        public bool flowAssist = true;
        public bool oneHanded;
        public bool reducedMotion;
        public bool timeTrial;
        public static RunSettings Current { get; private set; } = new RunSettings();

        public static IEnumerator Load()
        {
            string saved = Path.Combine(Application.persistentDataPath, "flow-settings.json");
            if (File.Exists(saved))
            {
                try { Apply(File.ReadAllText(saved)); }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException) { Debug.LogWarning(exception.Message); }
                yield break;
            }
            string path = Path.Combine(Application.streamingAssetsPath, "flow-settings.json");
            if (!path.Contains("://")) path = new Uri(path).AbsoluteUri;
            using (UnityWebRequest request = UnityWebRequest.Get(path))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) Apply(request.downloadHandler.text);
            }
        }

        private static void Apply(string json)
        {
            try { Current = JsonUtility.FromJson<RunSettings>(json) ?? new RunSettings(); }
            catch (ArgumentException) { Current = new RunSettings(); }
        }

        public static void Save()
        {
            try { File.WriteAllText(Path.Combine(Application.persistentDataPath, "flow-settings.json"), JsonUtility.ToJson(Current)); }
            catch (IOException exception) { Debug.LogWarning(exception.Message); }
        }
    }
}
