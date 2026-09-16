using UnityEngine;
using UnityEngine.Rendering;

namespace Flow
{
    public sealed class ChunkGenerator : MonoBehaviour
    {
        public const float CourseLength = 800f;
        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.isStatic = true;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part;
        }

        public GameObject Generate(Material white, Material gray, Material red, Material blue)
        {
            GameObject root = new GameObject("Sector 7 / First Line");
            Transform level = root.transform;
            Box("Recovery promenade", level, new Vector3(0f, -4.5f, 400f), new Vector3(34f, 1f, 820f), gray);
            Box("Boundary west", level, new Vector3(-17.5f, -1f, 400f), new Vector3(1f, 7f, 820f), white);
            Box("Boundary east", level, new Vector3(17.5f, -1f, 400f), new Vector3(1f, 7f, 820f), white);
            Box("Start boundary", level, new Vector3(0f, -1f, -8f), new Vector3(34f, 7f, 1f), white);
            Box("End boundary", level, new Vector3(0f, -1f, 808f), new Vector3(34f, 7f, 1f), white);
            for (int i = 0; i < 20; i++)
            {
                float z = i * 40f;
                Box("Main roof " + i, level, new Vector3(0f, -0.5f, z + 16f), new Vector3(12f, 1f, 36.5f), white);
                Box("Runner vision stripe", level, new Vector3(0f, 0.012f, z + 13f), new Vector3(0.22f, 0.025f, 18f), red);
                Box("West expert roof", level, new Vector3(-10f, 0f, z + 17f), new Vector3(5f, 1f, 37f), gray);
                Box("East expert roof", level, new Vector3(10f, 0.5f, z + 17f), new Vector3(5f, 1f, 37f), white);
                GameObject ramp = Box("Recovery return ramp", level, new Vector3(14.3f, -1.5f, z + 31f), new Vector3(3.3f, 0.25f, 11f), gray);
                ramp.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
                Box("Recovery bridge", level, new Vector3(12.5f, 0.8f, z + 37f), new Vector3(6f, 0.2f, 4f), gray);
                if (i % 4 == 0)
                {
                    GameObject fence = Box("VAULT / double stripe", level, new Vector3(0f, 0.4f, z + 12f), new Vector3(5f, 0.8f, 0.2f), red);
                    fence.AddComponent<ParkourSurface>().Configure(SurfaceType.Vault, Vector3.zero);
                    Box("Fence marking", level, new Vector3(0f, 0.6f, z + 11.88f), new Vector3(3f, 0.08f, 0.025f), white);
                }
                if (i % 4 == 1)
                {
                    Box("SLIDE / overhead rail", level, new Vector3(0f, 1.35f, z + 13f), new Vector3(5f, 0.35f, 2f), red);
                    for (int side = -1; side <= 1; side += 2)
                        Box("Slide support", level, new Vector3(side * 2.5f, 0.65f, z + 13f), new Vector3(0.2f, 1.3f, 2f), white);
                }
                if (i % 4 == 2)
                {
                    GameObject wall = Box("WALL RUN / side route", level, new Vector3(4.2f, 1.6f, z + 17f), new Vector3(0.4f, 3.2f, 7f), red);
                    wall.AddComponent<ParkourSurface>().Configure(SurfaceType.Wall, Vector3.zero);
                    GameObject ac = Box("CLIMB / AC unit", level, new Vector3(-4.5f, 0.7f, z + 17f), new Vector3(2f, 1.4f, 1.5f), red);
                    ac.AddComponent<ParkourSurface>().Configure(SurfaceType.Climb, Vector3.zero);
                }
                if (i % 4 == 3)
                {
                    GameObject zip = Box("GRAB / zip line", level, new Vector3(0f, 2.65f, z + 16f), new Vector3(0.18f, 0.18f, 15f), red);
                    zip.AddComponent<ParkourSurface>().Configure(SurfaceType.ZipLine, new Vector3(0f, -2.6f, 7f));
                }
                if (i % 3 == 0)
                {
                    Box("West branch bridge", level, new Vector3(-6f, 0.05f, z + 4f), new Vector3(10f, 0.2f, 5f), gray).transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
                    Box("East branch bridge", level, new Vector3(6f, 0.35f, z + 4f), new Vector3(10f, 0.2f, 5f), gray).transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
                }
                for (int side = -1; side <= 1; side += 2)
                    Box("City silhouette", level, new Vector3(side * (25f + i % 3 * 7f), -5f, z + 15f), new Vector3(10f, 7f + i % 5 * 3f, 17f), i % 2 == 0 ? white : blue);
            }
            Gate(level, 1f, white, red);
            Gate(level, 795f, white, red);
            return root;
        }
        private static void Gate(Transform parent, float z, Material white, Material red)
        {
            Box("Route gate", parent, new Vector3(0f, 3.5f, z), new Vector3(12f, 0.3f, 0.3f), red);
            Box("Gate left", parent, new Vector3(-6f, 1.75f, z), new Vector3(0.3f, 3.5f, 0.3f), red);
            Box("Gate right", parent, new Vector3(6f, 1.75f, z), new Vector3(0.3f, 3.5f, 0.3f), red);
        }
    }
}
