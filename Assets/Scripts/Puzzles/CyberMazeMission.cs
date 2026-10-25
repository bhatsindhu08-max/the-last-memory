using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TheLastMemory
{
    /// <summary>Final story level: find home in the neon city and restore the machine's humanity.</summary>
    public sealed class CyberMazeMission : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform playerVisual;
        [SerializeField] private Transform homeBeacon;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text progressLabel;
        [SerializeField] private AudioSource ambience;
        [SerializeField, Min(1f)] private float homeRadius = 3.1f;

        private PlayerController controller;
        private bool ending;
        private GameObject endOverlay;
        private Text endingText;
        private readonly Queue<GameObject> breadcrumbs = new Queue<GameObject>();
        private readonly Queue<GameObject> returnFlags = new Queue<GameObject>();
        private Material breadcrumbMaterial;
        private Material flagMaterial;
        private Material alleyThemeMaterial;
        private readonly List<Material> floorEmissionMaterials = new List<Material>();
        private Vector3 lastBreadcrumbPosition;
        private float nextHintUpdate;
        private const float BreadcrumbSpacing = 2.5f;
        private const int MaxBreadcrumbs = 180;
        private const int MaxFlags = 24;

        public void Configure(Transform targetPlayer, Transform visual, Transform home, Text status, Text progress, AudioSource music)
        {
            player = targetPlayer;
            playerVisual = visual;
            homeBeacon = home;
            statusLabel = status;
            progressLabel = progress;
            ambience = music;
        }

        private void Start()
        {
            ResolveReferences();
            SealMazeRoute();
            BuildNeonAlleyTheme();
            ApplyCyberpunkEmissionFloor();
            if (ambience != null && ambience.clip != null)
            {
                ambience.loop = true;
                if (!ambience.isPlaying) ambience.Play();
            }
            if (statusLabel != null)
                statusLabel.text = "LEVEL 4 — LET'S GO BACK HOME. Find the bright HOME beacon at the end of the city maze. Follow the streets through the buildings.";
            if (player != null)
            {
                lastBreadcrumbPosition = player.position;
                DropBreadcrumb(lastBreadcrumbPosition);
            }
            if (progressLabel != null) progressLabel.text = "CYAN TRAIL: YOUR ROUTE  •  F: PLANT A RETURN FLAG";
        }

        private void SealMazeRoute()
        {
            GameObject worldObject = GameObject.Find("Level4_CyberCityMaze");
            if (worldObject == null) return;
            Transform maze = worldObject.transform;
            RebuildSingleSolutionMaze(maze);
            Material cityWallMaterial = null;

            Renderer[] renderers = maze.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.gameObject.name.StartsWith("Maze Building")) continue;
                if (cityWallMaterial == null) cityWallMaterial = renderer.sharedMaterial;

                // Slightly overlapping, unrotated blocks close the collision cracks between buildings.
                Transform wall = renderer.transform;
                wall.rotation = Quaternion.identity;
                Vector3 scale = wall.localScale;
                scale.x = Mathf.Max(scale.x, 4.12f);
                scale.z = Mathf.Max(scale.z, 4.12f);
                wall.localScale = scale;
            }

            Transform homePlaza = maze.Find("Home plaza");
            if (homePlaza != null)
            {
                Vector3 size = homePlaza.localScale;
                size.z = Mathf.Max(size.z, 16f);
                homePlaza.localScale = size;
            }

            // Close the back of the start and contain the supported HOME plaza.
            CreateMazeSeal(maze, "Maze Start Backstop", new Vector3(0f, 4f, -39.75f), new Vector3(4.3f, 8f, .5f), cityWallMaterial);
            CreateMazeSeal(maze, "Home District End Wall", new Vector3(0f, 4f, 51.8f), new Vector3(13f, 8f, .5f), cityWallMaterial);
            CreateMazeSeal(maze, "Home District Left Wall", new Vector3(-6.05f, 4f, 45f), new Vector3(.5f, 8f, 14f), cityWallMaterial);
            CreateMazeSeal(maze, "Home District Right Wall", new Vector3(6.05f, 4f, 45f), new Vector3(.5f, 8f, 14f), cityWallMaterial);
        }

        private static void RebuildSingleSolutionMaze(Transform maze)
        {
            const int size = 19;
            const int center = size / 2;
            bool[,] open = new bool[size, size];
            const int logicalSize = (size - 1) / 2;
            bool[,] visited = new bool[logicalSize, logicalSize];
            Stack<Vector2Int> search = new Stack<Vector2Int>();
            System.Random random = new System.Random(41024);

            Vector2Int start = new Vector2Int(center / 2, logicalSize - 1);
            visited[start.y, start.x] = true;
            open[size - 2, center] = true;
            open[1 + start.y * 2, 1 + start.x * 2] = true;
            search.Push(start);

            // A deterministic depth-first carve creates a dense, diagram-style perfect maze.
            // Every corridor cell belongs to one connected tree, so there are no shortcut loops.
            while (search.Count > 0)
            {
                Vector2Int current = search.Peek();
                int[] order = { 0, 1, 2, 3 };
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int swap = random.Next(i + 1);
                    int value = order[i];
                    order[i] = order[swap];
                    order[swap] = value;
                }

                bool advanced = false;
                for (int i = 0; i < order.Length; i++)
                {
                    Vector2Int direction = order[i] == 0 ? Vector2Int.up :
                        order[i] == 1 ? Vector2Int.right :
                        order[i] == 2 ? Vector2Int.down : Vector2Int.left;
                    Vector2Int next = current + direction;
                    if (next.x < 0 || next.x >= logicalSize || next.y < 0 || next.y >= logicalSize || visited[next.y, next.x])
                        continue;

                    int row = 1 + current.y * 2;
                    int col = 1 + current.x * 2;
                    int nextRow = 1 + next.y * 2;
                    int nextCol = 1 + next.x * 2;
                    open[row, col] = true;
                    open[nextRow, nextCol] = true;
                    open[(row + nextRow) / 2, (col + nextCol) / 2] = true;
                    visited[next.y, next.x] = true;
                    search.Push(next);
                    advanced = true;
                    break;
                }

                if (!advanced) search.Pop();
            }

            // The two openings are deliberate: the player's start and the single HOME exit.
            open[size - 1, center] = true;
            open[0, center] = true;
            open[1, center] = true;

            Material floorMaterial = null;
            Material wallMaterial = null;
            Transform foundation = maze.Find("Continuous city road foundation");
            Renderer foundationRenderer = foundation != null ? foundation.GetComponent<Renderer>() : null;
            if (foundationRenderer != null) floorMaterial = foundationRenderer.sharedMaterial;
            for (int i = 0; i < maze.childCount && wallMaterial == null; i++)
            {
                Transform child = maze.GetChild(i);
                if (!child.name.StartsWith("Maze Building")) continue;
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null) wallMaterial = renderer.sharedMaterial;
            }

            // Remove decor attached to the old generated layout so no lamps or signs float over the new paths.
            for (int i = 0; i < maze.childCount; i++)
            {
                Transform child = maze.GetChild(i);
                if (child.name.StartsWith("Neon shopfront") || child.name == "Faint lane marker" ||
                    child.name == "Neon lamp post" || child.name == "Neon lamp arm" || child.name == "Neon lamp" ||
                    child.name == "Rooftop antenna")
                    child.gameObject.SetActive(false);
            }

            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    string oldName = open[row, col] ? "Street Tile " + row + " " + col : "Maze Building " + row + " " + col;
                    string otherName = open[row, col] ? "Maze Building " + row + " " + col : "Street Tile " + row + " " + col;
                    Transform cell = maze.Find(oldName);
                    if (cell == null) cell = maze.Find(otherName);
                    if (cell == null)
                    {
                        GameObject created = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        created.transform.SetParent(maze, false);
                        cell = created.transform;
                    }

                    cell.name = open[row, col] ? "Street Tile " + row + " " + col : "Maze Building " + row + " " + col;
                    cell.gameObject.SetActive(true);
                    float x = (col - center) * 4f;
                    float z = (center - row) * 4f;
                    Renderer cellRenderer = cell.GetComponent<Renderer>();
                    BoxCollider box = cell.GetComponent<BoxCollider>();
                    if (open[row, col])
                    {
                        cell.localPosition = new Vector3(x, .035f, z);
                        cell.localRotation = Quaternion.identity;
                        cell.localScale = new Vector3(4f, .07f, 4f);
                        if (cellRenderer != null && floorMaterial != null) cellRenderer.sharedMaterial = floorMaterial;
                        if (box != null) box.enabled = true;
                    }
                    else
                    {
                        bool boundary = row == 0 || col == 0 || row == size - 1 || col == size - 1;
                        float height = boundary ? 14f : 8f;
                        cell.localPosition = new Vector3(x, height * .5f, z);
                        cell.localRotation = Quaternion.identity;
                        cell.localScale = new Vector3(4.12f, height, 4.12f);
                        if (cellRenderer != null && wallMaterial != null) cellRenderer.sharedMaterial = wallMaterial;
                        if (box == null) box = cell.gameObject.AddComponent<BoxCollider>();
                        box.enabled = true;
                        box.size = Vector3.one;
                    }
                }
            }
        }

        private static void CreateMazeSeal(Transform parent, string objectName, Vector3 position, Vector3 size, Material material)
        {
            if (parent.Find(objectName) != null) return;
            GameObject seal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seal.name = objectName;
            seal.transform.SetParent(parent, false);
            seal.transform.position = position;
            seal.transform.localScale = size;
            Renderer renderer = seal.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
        }

        private void BuildNeonAlleyTheme()
        {
            Texture2D alleyImage = Resources.Load<Texture2D>("Level4Theme/NeonAlley");
            if (alleyImage == null)
            {
                Debug.LogWarning("Level 4 alley theme image was not found at Resources/Level4Theme/NeonAlley.", this);
                return;
            }

            GameObject worldObject = GameObject.Find("Level4_CyberCityMaze");
            if (worldObject == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) return;

            alleyThemeMaterial = new Material(shader) { name = "Level 4 Neon Alley Theme" };
            alleyThemeMaterial.mainTexture = alleyImage;
            if (alleyThemeMaterial.HasProperty("_BaseMap")) alleyThemeMaterial.SetTexture("_BaseMap", alleyImage);
            if (alleyThemeMaterial.HasProperty("_MainTex")) alleyThemeMaterial.SetTexture("_MainTex", alleyImage);

            // Portrait art billboards sit beyond the maze boundary as a distant neon-city skyline.
            float[] positions = { -36f, -12f, 12f, 36f };
            for (int i = 0; i < positions.Length; i++)
            {
                CreateThemeBillboard(worldObject.transform, new Vector3(-63f, 15f, positions[i]), Quaternion.Euler(0f, 90f, 0f));
                CreateThemeBillboard(worldObject.transform, new Vector3(63f, 15f, positions[i]), Quaternion.Euler(0f, -90f, 0f));
                CreateThemeBillboard(worldObject.transform, new Vector3(positions[i], 15f, -60f), Quaternion.identity);
                CreateThemeBillboard(worldObject.transform, new Vector3(positions[i], 15f, 60f), Quaternion.Euler(0f, 180f, 0f));
            }
        }


        private void ApplyCyberpunkEmissionFloor()
        {
            Texture2D emissionPattern = Resources.Load<Texture2D>("Level4Theme/CyberpunkEmission");
            GameObject worldObject = GameObject.Find("Level4_CyberCityMaze");
            if (emissionPattern == null || worldObject == null)
            {
                if (emissionPattern == null)
                    Debug.LogWarning("Level 4 floor emission image was not found at Resources/Level4Theme/CyberpunkEmission.", this);
                return;
            }

            Renderer foundation = FindRenderer(worldObject.transform, "Continuous city road foundation");
            if (foundation != null)
                foundation.sharedMaterial = CreateFloorEmissionMaterial(foundation.sharedMaterial, emissionPattern, new Vector2(18f, 18f), 1.25f, "L4 Road Foundation Emission");

            Material streetTileMaterial = null;
            Material homeFloorMaterial = null;
            Material wallEmissionMaterial = null;
            Renderer[] renderers = worldObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer floorRenderer in renderers)
            {
                if (floorRenderer == null || floorRenderer.gameObject == foundation?.gameObject) continue;
                string objectName = floorRenderer.gameObject.name;
                if (objectName.StartsWith("Street Tile"))
                {
                    if (streetTileMaterial == null)
                        streetTileMaterial = CreateFloorEmissionMaterial(floorRenderer.sharedMaterial, emissionPattern, Vector2.one, 1.25f, "L4 Street Tile Emission");
                    floorRenderer.sharedMaterial = streetTileMaterial;
                }
                else if (objectName == "Home plaza")
                {
                    if (homeFloorMaterial == null)
                        homeFloorMaterial = CreateFloorEmissionMaterial(floorRenderer.sharedMaterial, emissionPattern, Vector2.one, 1.25f, "L4 Home Plaza Emission");
                    floorRenderer.sharedMaterial = homeFloorMaterial;
                }
                else if (objectName.StartsWith("Maze Building") || objectName == "Maze Start Backstop" ||
                         objectName.StartsWith("Home District"))
                {
                    if (wallEmissionMaterial == null)
                        wallEmissionMaterial = CreateFloorEmissionMaterial(floorRenderer.sharedMaterial, emissionPattern, Vector2.one, .55f, "L4 Neon Circuit Building Emission");
                    if (wallEmissionMaterial != null) floorRenderer.sharedMaterial = wallEmissionMaterial;
                }
            }
        }

        private Material CreateFloorEmissionMaterial(Material source, Texture2D emissionPattern, Vector2 tiling, float emissionStrength, string materialName)
        {
            if (source == null) return null;
            Material material = new Material(source) { name = materialName };
            if (material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", emissionPattern);
                material.SetTextureScale("_EmissionMap", tiling);
            }
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.white * emissionStrength);
            }
            floorEmissionMaterials.Add(material);
            return material;
        }

        private static Renderer FindRenderer(Transform root, string objectName)
        {
            Transform target = root.Find(objectName);
            return target != null ? target.GetComponent<Renderer>() : null;
        }

        private void CreateThemeBillboard(Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject billboard = GameObject.CreatePrimitive(PrimitiveType.Quad);
            billboard.name = "Neon Alley Theme Billboard";
            billboard.transform.SetParent(parent, false);
            billboard.transform.SetPositionAndRotation(position, rotation);
            billboard.transform.localScale = new Vector3(10f, 28f, 1f);
            Collider collider = billboard.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            billboard.GetComponent<Renderer>().sharedMaterial = alleyThemeMaterial;
        }

        private void Update()
        {
            if (ending)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            ResolveReferences();
            if (player == null || homeBeacon == null) return;

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                PlantReturnFlag();

            Vector3 travelled = player.position - lastBreadcrumbPosition;
            travelled.y = 0f;
            if (travelled.sqrMagnitude >= BreadcrumbSpacing * BreadcrumbSpacing)
            {
                lastBreadcrumbPosition = player.position;
                DropBreadcrumb(player.position);
            }

            if (Time.unscaledTime >= nextHintUpdate)
            {
                UpdateHomeHint();
                nextHintUpdate = Time.unscaledTime + .35f;
            }

            Vector3 delta = player.position - homeBeacon.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= homeRadius * homeRadius) BeginEnding();
        }

        private void ResolveReferences()
        {
            if (player == null)
            {
                controller = FindFirstObjectByType<PlayerController>();
                if (controller != null) player = controller.transform;
            }
            else if (controller == null) controller = player.GetComponent<PlayerController>();

            if (playerVisual == null && player != null)
            {
                Transform capsule = player.Find("Capsule");
                if (capsule != null) playerVisual = capsule;
            }
            if (homeBeacon == null)
            {
                GameObject beacon = GameObject.Find("HOME Beacon");
                if (beacon != null) homeBeacon = beacon.transform;
            }
        }


        private void OnDestroy()
        {
            if (alleyThemeMaterial != null) Destroy(alleyThemeMaterial);
            foreach (Material material in floorEmissionMaterials)
                if (material != null) Destroy(material);
            floorEmissionMaterials.Clear();
        }

        private void UpdateHomeHint()
        {
            if (progressLabel == null || player == null || homeBeacon == null) return;
            Vector3 toHome = homeBeacon.position - player.position;
            toHome.y = 0f;
            float distance = toHome.magnitude;
            Vector3 forward = player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            float ahead = Vector3.Dot(toHome, forward);
            float side = Vector3.Dot(toHome, right);
            string direction;
            if (Mathf.Abs(ahead) > Mathf.Abs(side)) direction = ahead >= 0f ? "AHEAD" : "BEHIND";
            else direction = side >= 0f ? "RIGHT" : "LEFT";
            progressLabel.text = $"HOME  •  {distance:0}m {direction}  •  CYAN TRAIL: YOUR ROUTE  •  F: FLAG";
        }

        private void DropBreadcrumb(Vector3 position)
        {
            if (breadcrumbMaterial == null)
                breadcrumbMaterial = MakeMaterial(new Color32(35, 240, 255, 255), new Color(.05f, .8f, 1f));
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Maze Breadcrumb";
            marker.transform.position = new Vector3(position.x, .045f, position.z);
            marker.transform.localScale = new Vector3(.52f, .035f, .52f);
            marker.GetComponent<Renderer>().sharedMaterial = breadcrumbMaterial;
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            breadcrumbs.Enqueue(marker);
            while (breadcrumbs.Count > MaxBreadcrumbs)
            {
                GameObject oldest = breadcrumbs.Dequeue();
                if (oldest != null) Destroy(oldest);
            }
        }

        private void PlantReturnFlag()
        {
            if (flagMaterial == null)
                flagMaterial = MakeMaterial(new Color32(255, 190, 60, 255), new Color(1f, .42f, .04f));
            Vector3 ground = new Vector3(player.position.x, .04f, player.position.z);
            GameObject flag = new GameObject($"Return Flag {returnFlags.Count + 1}");
            GameObject baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRing.name = "Flag Base";
            baseRing.transform.SetParent(flag.transform, false);
            baseRing.transform.localPosition = Vector3.zero;
            baseRing.transform.localScale = new Vector3(.8f, .045f, .8f);
            baseRing.GetComponent<Renderer>().sharedMaterial = flagMaterial;
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Flag Mast";
            mast.transform.SetParent(flag.transform, false);
            mast.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            mast.transform.localScale = new Vector3(.07f, 1.25f, .07f);
            mast.GetComponent<Renderer>().sharedMaterial = flagMaterial;
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "Flag Banner";
            banner.transform.SetParent(flag.transform, false);
            banner.transform.localPosition = new Vector3(.34f, 2.12f, 0f);
            banner.transform.localScale = new Vector3(.68f, .36f, .06f);
            banner.GetComponent<Renderer>().sharedMaterial = flagMaterial;
            foreach (Collider col in flag.GetComponentsInChildren<Collider>()) Destroy(col);
            flag.transform.position = ground;
            returnFlags.Enqueue(flag);
            while (returnFlags.Count > MaxFlags)
            {
                GameObject oldest = returnFlags.Dequeue();
                if (oldest != null) Destroy(oldest);
            }
            if (statusLabel != null) statusLabel.text = "RETURN FLAG PLANTED  •  Follow the cyan trail back here";
        }

        private void BeginEnding()
        {
            ending = true;
            if (controller != null) controller.enabled = false;
            CharacterController collider = player != null ? player.GetComponent<CharacterController>() : null;
            if (collider != null) collider.enabled = false;
            if (ambience != null) ambience.Stop();

            if (playerVisual != null)
            {
                Transform machine = playerVisual.Find("Neon Hologram Avatar");
                if (machine == null) machine = playerVisual.Find("Cybernetic Avatar");
                if (machine != null) machine.gameObject.SetActive(false);
                Renderer oldMesh = playerVisual.GetComponent<Renderer>();
                if (oldMesh != null) oldMesh.enabled = false;
                CreateHumanForm(playerVisual);
            }

            if (statusLabel != null) statusLabel.text = "The signal is familiar. The machine remembers who it was.";
            if (progressLabel != null) progressLabel.text = "HOME REACHED  •  MEMORY RESTORED";
            ShowEnding();
        }

        private static void CreateHumanForm(Transform parent)
        {
            Transform prior = parent.Find("Human Form");
            if (prior != null) return;
            Transform human = new GameObject("Human Form").transform;
            human.SetParent(parent, false);

            Material skin = MakeMaterial(new Color32(168, 157, 145, 255));
            Material jacket = MakeMaterial(new Color32(75, 91, 102, 255));
            Material pants = MakeMaterial(new Color32(39, 48, 59, 255));
            Material hair = MakeMaterial(new Color32(32, 35, 39, 255));
            Material eye = MakeMaterial(new Color32(80, 205, 215, 255), new Color(.05f, .55f, .65f));

            Part(human, PrimitiveType.Capsule, "Human jacket", new Vector3(0f, .13f, 0f), new Vector3(.34f, .29f, .22f), jacket);
            Part(human, PrimitiveType.Sphere, "Human head", new Vector3(0f, .66f, 0f), new Vector3(.16f, .2f, .15f), skin);
            Part(human, PrimitiveType.Sphere, "Hair", new Vector3(0f, .78f, -.015f), new Vector3(.165f, .09f, .155f), hair);
            Part(human, PrimitiveType.Cube, "Left eye", new Vector3(-.055f, .68f, .138f), new Vector3(.035f, .018f, .012f), eye);
            Part(human, PrimitiveType.Cube, "Right eye", new Vector3(.055f, .68f, .138f), new Vector3(.035f, .018f, .012f), eye);
            Part(human, PrimitiveType.Capsule, "Left arm", new Vector3(-.39f, .08f, 0f), new Vector3(.1f, .25f, .1f), jacket, new Vector3(0f, 0f, -8f));
            Part(human, PrimitiveType.Capsule, "Right arm", new Vector3(.39f, .08f, 0f), new Vector3(.1f, .25f, .1f), jacket, new Vector3(0f, 0f, 8f));
            Part(human, PrimitiveType.Sphere, "Left hand", new Vector3(-.4f, -.22f, .01f), new Vector3(.09f, .09f, .09f), skin);
            Part(human, PrimitiveType.Sphere, "Right hand", new Vector3(.4f, -.22f, .01f), new Vector3(.09f, .09f, .09f), skin);
            Part(human, PrimitiveType.Capsule, "Left trouser", new Vector3(-.15f, -.39f, 0f), new Vector3(.13f, .25f, .14f), pants);
            Part(human, PrimitiveType.Capsule, "Right trouser", new Vector3(.15f, -.39f, 0f), new Vector3(.13f, .25f, .14f), pants);
            Part(human, PrimitiveType.Cube, "Left shoe", new Vector3(-.15f, -.9f, .04f), new Vector3(.15f, .13f, .23f), hair);
            Part(human, PrimitiveType.Cube, "Right shoe", new Vector3(.15f, -.9f, .04f), new Vector3(.15f, .13f, .23f), hair);
        }

        private void ShowEnding()
        {
            Canvas canvas = statusLabel != null ? statusLabel.canvas : null;
            if (canvas == null) return;

            endOverlay = new GameObject("Humanity Restored Ending", typeof(RectTransform), typeof(Image));
            endOverlay.transform.SetParent(canvas.transform, false);
            RectTransform full = endOverlay.GetComponent<RectTransform>();
            full.anchorMin = Vector2.zero;
            full.anchorMax = Vector2.one;
            full.offsetMin = Vector2.zero;
            full.offsetMax = Vector2.zero;
            Image dim = endOverlay.GetComponent<Image>();
            dim.color = new Color(.005f, .02f, .035f, .83f);
            dim.raycastTarget = false;

            GameObject card = new GameObject("Home Ending Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(endOverlay.transform, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(780f, 360f);
            Image background = card.GetComponent<Image>();
            background.color = new Color(.025f, .085f, .105f, .98f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Ending Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(card.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(30f, 28f);
            textRect.offsetMax = new Vector2(-30f, -28f);
            endingText = textObject.GetComponent<Text>();
            endingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            endingText.fontSize = 34;
            endingText.fontStyle = FontStyle.Bold;
            endingText.alignment = TextAnchor.MiddleCenter;
            endingText.horizontalOverflow = HorizontalWrapMode.Wrap;
            endingText.verticalOverflow = VerticalWrapMode.Overflow;
            endingText.color = new Color(.68f, 1f, .96f);
            endingText.text = "HOME FOUND\n\nThe machine remembers who it was.\n\nHumanity restored.\nTHE END\n\nPress R to play again";
        }

        private static void Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3 rotation = default)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(rotation);
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material MakeMaterial(Color color, Color? emission = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value * 2f);
            }
            return material;
        }
    }
}
