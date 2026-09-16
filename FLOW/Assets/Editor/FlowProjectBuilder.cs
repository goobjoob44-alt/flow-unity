using System.IO;
using Flow;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FlowProjectBuilder
{
    private const string Generated = "Assets/Generated";
    static FlowProjectBuilder() { EditorApplication.delayCall += BuildOnFirstImport; }
    private static void BuildOnFirstImport()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (!File.Exists("Assets/Scenes/MainMenu.unity")) Build();
    }

    [MenuItem("FLOW/Build vertical slice")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Generated);
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Prefabs/Props");
        Directory.CreateDirectory("Assets/Prefabs/Chunks");
        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.Refresh();
        ConfigureProject();
        ConfigureRendering();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Material white = Material("EnvironmentWhite", "#E8E8E8");
        Material gray = Material("EnvironmentGray", "#D0D0D0");
        Material red = Material("InteractableRed", "#FF3B30");
        Material blue = Material("ShadowBlue", "#B0C4DE");
        Material orange = Material("AccentOrange", "#FF9500");
        Material dark = Material("GloveGraphite", "#263846");
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.69f, 0.77f, 0.87f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.63f, 0.77f, 0.88f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 90f;
        RenderSettings.fogEndDistance = 190f;
        Light sun = new GameObject("Sun / no realtime shadows").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.15f; sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.sun = sun;
        GameObject systems = new GameObject("FLOW Systems");
        ChunkGenerator generator = systems.AddComponent<ChunkGenerator>();
        GameObject level = generator.Generate(white, gray, red, blue);
        ChunkData data = Asset<ChunkData>(Generated + "/Sector7.asset");
        MovementTuning tuning = Asset<MovementTuning>(Generated + "/Movement.asset");
        GameObject playerObject = new GameObject("Kai / First-person runner");
        playerObject.layer = 2;
        playerObject.transform.position = data.StartPosition;
        CharacterController body = playerObject.AddComponent<CharacterController>();
        body.height = 1.8f; body.center = Vector3.up * 0.9f; body.radius = 0.28f; body.stepOffset = 0.25f; body.skinWidth = 0.025f;
        body.slopeLimit = 55f;
        MomentumSystem momentum = playerObject.AddComponent<MomentumSystem>(); momentum.Configure(tuning);
        playerObject.AddComponent<ContextDetector>();
        TouchInputManager input = playerObject.AddComponent<TouchInputManager>();
        ParkourController player = playerObject.AddComponent<ParkourController>(); player.Configure(input);
        Camera camera = new GameObject("First Person Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.transform.SetParent(playerObject.transform, false); camera.transform.localPosition = Vector3.up * 1.6f;
        camera.tag = "MainCamera"; camera.fieldOfView = 84f; camera.nearClipPlane = 0.04f; camera.farClipPlane = 190f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.29f, 0.56f, 0.85f);
        camera.allowHDR = false; camera.allowMSAA = true;
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderShadows = false;
        Transform left = Arm(camera.transform, -1f, orange, dark);
        Transform right = Arm(camera.transform, 1f, orange, dark);
        playerObject.AddComponent<ParkourAnimator>().Configure(player, camera, left, right);
        PrefabUtility.SaveAsPrefabAsset(playerObject, "Assets/Prefabs/Player.prefab");
        GameObject ghostObject = new GameObject("Personal-best ghost");
        GameObject ghostBody = ChunkGenerator.Box("Ghost silhouette", ghostObject.transform, Vector3.up * 0.9f, new Vector3(0.45f, 1.8f, 0.3f), blue);
        ghostBody.isStatic = false; Object.DestroyImmediate(ghostBody.GetComponent<Collider>());
        GhostReplay ghost = systems.AddComponent<GhostReplay>(); ghost.Configure(player.transform, ghostObject.transform);
        AudioManager audio = systems.AddComponent<AudioManager>(); audio.Configure(player);
        RunSession session = systems.AddComponent<RunSession>();
        HUDController hud = systems.AddComponent<HUDController>(); hud.Configure(player, input, session);
        Transform[] shards = new Transform[data.Shards.Length];
        for (int i = 0; i < shards.Length; i++)
        {
            GameObject shard = ChunkGenerator.Box("Data shard " + (i + 1), level.transform, data.Shards[i], Vector3.one * 0.38f, orange);
            shard.transform.rotation = Quaternion.Euler(0f, 45f, 45f); shard.isStatic = false;
            Object.DestroyImmediate(shard.GetComponent<Collider>()); shards[i] = shard.transform;
        }
        GameObject antenna = ChunkGenerator.Box("Hidden radio antenna", level.transform, new Vector3(-4.5f, 0f, 185f), Vector3.one, gray);
        ChunkGenerator.Box("Radio mast", antenna.transform, new Vector3(0f, 2f, 0f), new Vector3(0.08f, 4f, 0.08f), red);
        session.Configure(player, ghost, hud, audio, shards, antenna.transform);
        systems.AddComponent<AdaptiveResolution>();
        MakePropLibrary(white, red, gray);
        AddDistanceLods(level);
        GameObject chunkPrefab = PrefabUtility.SaveAsPrefabAsset(level, "Assets/Prefabs/Chunks/Sector7.prefab");
        data.SetPrefab(chunkPrefab); EditorUtility.SetDirty(data);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Chapter1.unity");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity", true);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true), new EditorBuildSettingsScene("Assets/Scenes/Chapter1.unity", true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("FLOW source slice generated. Open MainMenu and use a mobile simulator or build to Android/iOS. Native gameplay and performance must be validated on a device.");
    }
    private static T Asset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
    }
    private static Material Material(string name, string hex)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        ColorUtility.TryParseHtmlString(hex, out Color color);
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", 0.15f); material.enableInstancing = true;
        return material;
    }
    private static Transform Arm(Transform camera, float side, Material jacket, Material glove)
    {
        GameObject sleeve = ChunkGenerator.Box(side < 0f ? "Left sleeve" : "Right sleeve", camera, new Vector3(side * 0.28f, -0.38f, 0.42f), new Vector3(0.15f, 0.16f, 0.38f), jacket);
        sleeve.isStatic = false; Object.DestroyImmediate(sleeve.GetComponent<Collider>());
        GameObject hand = ChunkGenerator.Box("Fingerless glove", sleeve.transform, new Vector3(0f, 0f, 0.65f), new Vector3(0.92f, 0.9f, 0.42f), glove);
        hand.isStatic = false; Object.DestroyImmediate(hand.GetComponent<Collider>());
        return sleeve.transform;
    }
    private static void ConfigureRendering()
    {
        string path = Generated + "/MobileURP.asset";
        UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (pipeline == null)
        {
            UniversalRendererData renderer = Asset<UniversalRendererData>(Generated + "/MobileRenderer.asset");
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, path);
        }
        pipeline.supportsHDR = false; pipeline.msaaSampleCount = 2; pipeline.renderScale = 1f; pipeline.shadowDistance = 0f;
        pipeline.supportsCameraDepthTexture = false; pipeline.supportsCameraOpaqueTexture = false;
        GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
        EditorUtility.SetDirty(pipeline);
    }
    private static void ConfigureProject()
    {
        PlayerSettings.companyName = "FLOW Studio"; PlayerSettings.productName = "FLOW";
        PlayerSettings.bundleVersion = "0.2.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.flow.parkour");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.flow.parkour");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.iOS.targetOSVersionString = "14.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.runInBackground = false;
        SerializedObject settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        SerializedProperty input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo(); }
    }
    private static void MakePropLibrary(Material white, Material red, Material gray)
    {
        string[] names = { "LowFence", "HighFence", "Railing", "HorizontalPipe", "VerticalPipe", "Ledge", "Ramp", "Vent", "ZipLine", "ACUnit", "WaterTower", "SatelliteDish" };
        foreach (string name in names)
        {
            GameObject root = FlowPropBuilder.Create(name, white, red, gray);
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Props/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }
    }
    private static void AddDistanceLods(GameObject level)
    {
        Renderer[] renderers = level.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].gameObject.isStatic) continue;
            LODGroup group = renderers[i].gameObject.AddComponent<LODGroup>();
            Renderer[] mesh = { renderers[i] };
            // Primitive surfaces already have twelve triangles; distant tiers reuse them rather than adding geometry.
            group.SetLODs(new[] { new LOD(0.25f, mesh), new LOD(0.1f, mesh), new LOD(0.03f, mesh), new LOD(0.004f, mesh) });
            group.RecalculateBounds();
        }
    }
}
