using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheLastMemory
{
    /// <summary>Visual-only image screens for Level 1. Contains no colliders or puzzle logic.</summary>
    public sealed class LevelOneRedCodeTheme : MonoBehaviour
    {
        private const string RootName = "Level1_RedDigitalTheme";
        private readonly List<Material> runtimeMaterials = new List<Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ListenForLevelOne()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SampleScene" || GameObject.Find(RootName) != null) return;
            GameObject root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<LevelOneRedCodeTheme>();
        }

        private void Start()
        {
            string[] imagePaths =
            {
                "Level1Theme/CodeGrid",
                "Level1Theme/SignalFigure",
                "Level1Theme/PuppetSignal"
            };
            float[] xPositions = { -13.5f, 0f, 13.5f };

            ApplyFloorTexture();
            ApplyWallTextures();
            BrightenMachinesAndCrowd();
            BuildRedCeiling();

            for (int i = 0; i < imagePaths.Length; i++)
            {
                Texture2D image = Resources.Load<Texture2D>(imagePaths[i]);
                if (image == null) continue;

                GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
                screen.name = "Red Code Theme Screen " + (i + 1);
                screen.transform.SetParent(transform, false);
                screen.transform.SetPositionAndRotation(new Vector3(xPositions[i], 6f, 21.35f), Quaternion.Euler(0f, 180f, 0f));
                screen.transform.localScale = new Vector3(5.2f, 10.4f, 1f);
                Collider screenCollider = screen.GetComponent<Collider>();
                if (screenCollider != null) Destroy(screenCollider);
                screen.GetComponent<Renderer>().sharedMaterial = CreateImageMaterial(image);
                AddFrame(i, xPositions[i]);
            }
        }

        private void ApplyFloorTexture()
        {
            Texture2D texture = Resources.Load<Texture2D>("Level1Theme/CodeGrid");
            GameObject floor = GameObject.Find("Stage Floor");
            if (texture == null || floor == null) return;
            Renderer renderer = floor.GetComponent<Renderer>();
            if (renderer == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;
            Material floorMaterial = new Material(shader)
            {
                name = "Level 1 Red Circuit Floor",
                mainTexture = texture,
                mainTextureScale = new Vector2(3f, 4f),
                color = Color.white
            };
            if (floorMaterial.HasProperty("_BaseMap"))
            {
                floorMaterial.SetTexture("_BaseMap", texture);
                floorMaterial.SetTextureScale("_BaseMap", new Vector2(3f, 4f));
            }
            if (floorMaterial.HasProperty("_BaseColor")) floorMaterial.SetColor("_BaseColor", Color.white);
            if (floorMaterial.HasProperty("_EmissionMap")) floorMaterial.SetTexture("_EmissionMap", texture);
            if (floorMaterial.HasProperty("_EmissionColor"))
            {
                floorMaterial.EnableKeyword("_EMISSION");
                floorMaterial.SetColor("_EmissionColor", Color.white * 1.8f);
            }
            if (floorMaterial.HasProperty("_Metallic")) floorMaterial.SetFloat("_Metallic", .16f);
            if (floorMaterial.HasProperty("_Smoothness")) floorMaterial.SetFloat("_Smoothness", .42f);
            runtimeMaterials.Add(floorMaterial);
            renderer.sharedMaterial = floorMaterial;
        }

        private void ApplyWallTextures()
        {
            Texture2D texture = Resources.Load<Texture2D>("Level1Theme/CodeGrid");
            if (texture == null) return;

            string[] wallNames = { "Left Wall", "Right Wall", "Rear Wall" };
            for (int i = 0; i < wallNames.Length; i++)
            {
                GameObject wall = GameObject.Find(wallNames[i]);
                if (wall == null) continue;
                Renderer[] renderers = wall.GetComponentsInChildren<Renderer>(true);
                Material wallMaterial = CreatePatternMaterial("Level 1 Red Circuit " + wallNames[i], texture,
                    new Vector2(2f, 1f), new Color(1.4f, .04f, .08f));
                foreach (Renderer renderer in renderers)
                    renderer.sharedMaterial = wallMaterial;
            }
        }

        private Material CreatePatternMaterial(string materialName, Texture2D texture, Vector2 tiling, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = materialName,
                mainTexture = texture,
                mainTextureScale = tiling,
                color = Color.white
            };
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", texture);
                material.SetTextureScale("_EmissionMap", tiling);
            }
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            runtimeMaterials.Add(material);
            return material;
        }

        private void BrightenMachinesAndCrowd()
        {
            GameObject environment = GameObject.Find("Level1_Auditory_Environment");
            if (environment == null) return;
            Material redAccent = CreateSolidMaterial("Level 1 Red Neck Highlight", new Color(.85f, .015f, .035f),
                new Color(1f, .008f, .02f));

            EchoToneMachine[] machines = environment.GetComponentsInChildren<EchoToneMachine>(true);
            foreach (EchoToneMachine machine in machines)
            {
                Renderer[] renderers = machine.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                    BrightenRenderer(renderer, .68f, new Color(.18f, .58f, 1f));
            }

            Renderer[] figures = environment.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in figures)
            {
                if (renderer.name.IndexOf("Crowd silhouette", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    renderer.name.IndexOf("Faceless memory figure", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                DarkenFigure(renderer);
                if (renderer.name.IndexOf("Crowd silhouette", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Bounds bounds = renderer.bounds;
                    float bandWidth = Mathf.Max(.12f, bounds.size.x * .75f);
                    float bandDepth = Mathf.Max(.035f, bounds.size.z * .75f);
                    Vector3 localBand = renderer.transform.InverseTransformPoint(
                        new Vector3(bounds.center.x, bounds.max.y - Mathf.Max(.06f, bounds.size.y * .12f), bounds.center.z));
                    CreateNeckBand(renderer.transform, localBand, new Vector3(bandWidth, .035f, bandDepth), redAccent);
                }
            }

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                Transform[] children = player.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.name != "Neon Hologram Avatar") continue;
                    CreateNeckBand(child, new Vector3(0f, 1.53f, .095f), new Vector3(.29f, .04f, .025f), redAccent);
                    break;
                }
            }
        }

        private void DarkenFigure(Renderer renderer)
        {
            Material material = renderer.material;
            Color black = new Color(.004f, .004f, .008f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", black);
            material.color = black;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .08f);
            runtimeMaterials.Add(material);
        }

        private void BrightenRenderer(Renderer renderer, float whiteMix, Color glowTint)
        {
            Material material = renderer.material;
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            color = Color.Lerp(color, Color.white, whiteMix);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", glowTint * .78f);
            }
            runtimeMaterials.Add(material);
        }

        private void CreateNeckBand(Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
            band.name = "Red neck highlight";
            band.transform.SetParent(parent, false);
            band.transform.localPosition = localPosition;
            band.transform.localRotation = Quaternion.identity;
            band.transform.localScale = localScale;
            Collider collider = band.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            band.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void BuildRedCeiling()
        {
            Material ceilingMaterial = CreateSolidMaterial("Level 1 Ceiling Panels", new Color(.035f, .012f, .021f), new Color(.045f, .001f, .008f));
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ceiling.name = "Level 1 Dark Ceiling";
            ceiling.transform.SetParent(transform, false);
            ceiling.transform.SetPositionAndRotation(new Vector3(0f, 11.82f, 9f), Quaternion.Euler(90f, 0f, 0f));
            ceiling.transform.localScale = new Vector3(55f, 25f, 1f);
            Collider ceilingCollider = ceiling.GetComponent<Collider>();
            if (ceilingCollider != null) Destroy(ceilingCollider);
            ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMaterial;

            Material red = CreateSolidMaterial("Level 1 Ceiling Red Glow", new Color(.72f, .018f, .055f), new Color(.85f, .012f, .04f));
            float[] crossBeams = { -1f, 6f, 13f, 20f };
            for (int i = 0; i < crossBeams.Length; i++)
                CreateDecoration("Ceiling red strip " + (i + 1), new Vector3(0f, 11.74f, crossBeams[i]), new Vector3(38f, .045f, .075f), red);
            float[] sideStrips = { -19f, -10f, 10f, 19f };
            for (int i = 0; i < sideStrips.Length; i++)
                CreateDecoration("Ceiling side strip " + (i + 1), new Vector3(sideStrips[i], 11.74f, 9f), new Vector3(.07f, .045f, 23f), red);
        }

        private Material CreateSolidMaterial(string materialName, Color baseColor, Color emissionColor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = materialName,
                color = baseColor
            };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }
            runtimeMaterials.Add(material);
            return material;
        }

        private void CreateDecoration(string objectName, Vector3 position, Vector3 scale, Material material)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = objectName;
            piece.transform.SetParent(transform, false);
            piece.transform.position = position;
            piece.transform.localScale = scale;
            Collider collider = piece.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            piece.GetComponent<Renderer>().sharedMaterial = material;
        }

        private Material CreateImageMaterial(Texture2D image)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = new Material(shader);
            material.name = "Level 1 Red Digital Art";
            material.mainTexture = image;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", image);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            runtimeMaterials.Add(material);
            return material;
        }

        private void AddFrame(int index, float centerX)
        {
            Material frameMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            frameMaterial.name = "Level 1 Red Frame " + index;
            Color red = new Color(.9f, .025f, .07f);
            frameMaterial.color = red;
            if (frameMaterial.HasProperty("_BaseColor")) frameMaterial.SetColor("_BaseColor", red);
            if (frameMaterial.HasProperty("_EmissionColor"))
            {
                frameMaterial.EnableKeyword("_EMISSION");
                frameMaterial.SetColor("_EmissionColor", red * 2.1f);
            }
            runtimeMaterials.Add(frameMaterial);

            FrameBar("Top", new Vector3(centerX, 11.27f, 21.28f), new Vector3(5.35f, .07f, .06f), frameMaterial);
            FrameBar("Bottom", new Vector3(centerX, .73f, 21.28f), new Vector3(5.35f, .07f, .06f), frameMaterial);
            FrameBar("Left", new Vector3(centerX - 2.64f, 6f, 21.28f), new Vector3(.07f, 10.6f, .06f), frameMaterial);
            FrameBar("Right", new Vector3(centerX + 2.64f, 6f, 21.28f), new Vector3(.07f, 10.6f, .06f), frameMaterial);
        }

        private void FrameBar(string label, Vector3 position, Vector3 scale, Material material)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Red hologram frame " + label;
            bar.transform.SetParent(transform, false);
            bar.transform.position = position;
            bar.transform.localScale = scale;
            Collider collider = bar.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            bar.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeMaterials.Count; i++)
                if (runtimeMaterials[i] != null) Destroy(runtimeMaterials[i]);
            runtimeMaterials.Clear();
        }
    }
}
