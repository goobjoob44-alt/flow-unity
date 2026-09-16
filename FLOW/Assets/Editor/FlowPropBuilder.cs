using Flow;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FlowPropBuilder
{
    public static GameObject Create(string name, Material white, Material red, Material gray)
    {
        GameObject root = new GameObject(name);
        Transform parent = root.transform;
        switch (name)
        {
            case "LowFence": Surface(Box("Vault panel", parent, new Vector3(0f, 0.25f, 0f), new Vector3(1f, 0.5f, 0.1f), red), SurfaceType.Vault); break;
            case "HighFence": Surface(Box("Climb panel", parent, new Vector3(0f, 0.6f, 0f), new Vector3(1f, 1.2f, 0.1f), red), SurfaceType.Climb); break;
            case "Railing":
                Cylinder("Rail", parent, new Vector3(0f, 1.3f, 0f), 0.05f, 3f, Quaternion.Euler(0f, 0f, 90f), red);
                for (int side = -1; side <= 1; side += 2) Cylinder("Support", parent, new Vector3(side * 1.4f, 0.65f, 0f), 0.05f, 1.3f, Quaternion.identity, gray);
                break;
            case "HorizontalPipe":
                Cylinder("Pipe", parent, new Vector3(0f, 2.5f, 0f), 0.08f, 4f, Quaternion.Euler(0f, 0f, 90f), red).AddComponent<ParkourSurface>().Configure(SurfaceType.ZipLine, new Vector3(1.8f, -2.45f, 0f));
                break;
            case "VerticalPipe": Surface(Cylinder("Pipe", parent, Vector3.up * 1.5f, 0.08f, 3f, Quaternion.identity, red), SurfaceType.Climb); break;
            case "Ledge": Surface(Box("Ledge", parent, Vector3.up * 1.5f, new Vector3(2f, 0.15f, 0.3f), red), SurfaceType.Climb); break;
            case "Ramp":
                Vector3[] vertices = { new Vector3(-1.5f, 0f, -2.5f), new Vector3(1.5f, 0f, -2.5f), new Vector3(-1.5f, 0f, 2.5f), new Vector3(1.5f, 0f, 2.5f), new Vector3(-1.5f, 2.887f, 2.5f), new Vector3(1.5f, 2.887f, 2.5f) };
                int[] triangles = { 0, 4, 5, 0, 5, 1, 0, 2, 4, 1, 5, 3, 2, 3, 5, 2, 5, 4, 0, 1, 3, 0, 3, 2 };
                MeshPart("Ramp", parent, vertices, triangles, red, true);
                break;
            case "Vent":
                Box("Vent top", parent, new Vector3(0f, 0.65f, 0f), new Vector3(0.8f, 0.1f, 1f), gray);
                Box("Left side", parent, new Vector3(-0.4f, 0.3f, 0f), new Vector3(0.1f, 0.6f, 1f), gray);
                Box("Right side", parent, new Vector3(0.4f, 0.3f, 0f), new Vector3(0.1f, 0.6f, 1f), gray);
                break;
            case "ZipLine":
                Cylinder("Cable", parent, new Vector3(0f, 2.65f, 0f), 0.04f, 15f, Quaternion.Euler(90f, 0f, 0f), red).AddComponent<ParkourSurface>().Configure(SurfaceType.ZipLine, new Vector3(0f, -2.6f, 7f));
                break;
            case "ACUnit":
                Surface(Box("Housing", parent, Vector3.up * 0.5f, new Vector3(1f, 1f, 0.8f), white), SurfaceType.Vault);
                Cylinder("Fan hub", parent, new Vector3(0f, 1.02f, 0f), 0.3f, 0.04f, Quaternion.identity, gray);
                for (int i = 0; i < 4; i++) Box("Fan blade", parent, new Vector3(0f, 1.05f, 0f), new Vector3(0.5f, 0.02f, 0.06f), gray).transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
                break;
            case "WaterTower":
                Cylinder("Tank", parent, Vector3.up * 3f, 2f, 3f, Quaternion.identity, white);
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                    Cylinder("Leg", parent, new Vector3(Mathf.Cos(angle) * 1.5f, 0.75f, Mathf.Sin(angle) * 1.5f), 0.12f, 1.5f, Quaternion.identity, gray);
                }
                for (int i = 0; i < 6; i++) Box("Ladder rung", parent, new Vector3(0f, 0.3f + i * 0.5f, -2.05f), new Vector3(0.7f, 0.06f, 0.12f), red);
                break;
            case "SatelliteDish":
                Cylinder("Stand", parent, Vector3.up * 0.5f, 0.08f, 1f, Quaternion.identity, gray);
                CreateDish(parent, white);
                break;
        }
        return root;
    }
    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material) => ChunkGenerator.Box(name, parent, position, size, material);
    private static void Surface(GameObject part, SurfaceType type) { part.AddComponent<ParkourSurface>().Configure(type, Vector3.zero); }
    private static GameObject Cylinder(string name, Transform parent, Vector3 position, float radius, float length, Quaternion rotation, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        part.name = name; part.transform.SetParent(parent, false); part.transform.localPosition = position;
        part.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f); part.transform.localRotation = rotation;
        part.isStatic = true;
        Renderer renderer = part.GetComponent<Renderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        return part;
    }
    private static void MeshPart(string name, Transform parent, Vector3[] vertices, int[] triangles, Material material, bool collider)
    {
        string path = "Assets/Generated/" + name + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null) { mesh = new Mesh { name = name }; AssetDatabase.CreateAsset(mesh, path); }
        mesh.Clear(); mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
        GameObject part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); part.transform.SetParent(parent, false); part.isStatic = true;
        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = part.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        if (collider) part.AddComponent<MeshCollider>().sharedMesh = mesh;
    }
    private static void CreateDish(Transform parent, Material material)
    {
        const int rings = 4, segments = 16;
        Vector3[] vertices = new Vector3[(rings + 1) * (segments + 1)];
        int[] triangles = new int[rings * segments * 12];
        int cursor = 0;
        for (int ring = 0; ring <= rings; ring++)
        {
            float radius = ring / (float)rings;
            for (int s = 0; s <= segments; s++)
            {
                float angle = s * Mathf.PI * 2f / segments;
                vertices[ring * (segments + 1) + s] = new Vector3(Mathf.Cos(angle) * radius, 1f + radius * radius * 0.4f, Mathf.Sin(angle) * radius);
                if (ring == rings || s == segments) continue;
                int a = ring * (segments + 1) + s, b = a + segments + 1;
                int[] face = { a, b, a + 1, a + 1, b, b + 1, a + 1, b, a, b + 1, b, a + 1 };
                for (int j = 0; j < face.Length; j++) triangles[cursor++] = face[j];
            }
        }
        MeshPart("SatelliteBowl", parent, vertices, triangles, material, false);
    }
}
