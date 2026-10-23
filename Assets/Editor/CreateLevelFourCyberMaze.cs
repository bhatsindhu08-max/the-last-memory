using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheLastMemory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CreateLevelFourCyberMaze
{
    private const string ScenePath = "Assets/Scenes/Level4_CyberMaze.unity";
    private const string MaterialsPath = "Assets/Art/Materials";
    private const int MazeSize = 19;
    private const int Center = MazeSize / 2;
    private const float CellSize = 4f;

    [MenuItem("The Last Memory/Level 4/Create Cyber Maze Ending")]
    private static void BuildFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += BuildFromMenu;
            return;
        }

        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Rebuild Level 4?", "Level4_CyberMaze already exists. Rebuild this scene from the maze generator?", "Rebuild", "Cancel"))
            return;

        BuildScene();
        EditorUtility.DisplayDialog("Level 4 ready", "Cyber Maze is ready. Level 3 now leads here, and the home beacon ends the story.", "OK");
    }

    public static void BuildBatch()
    {
        if (!File.Exists(ScenePath)) BuildScene();
    }

    private static void BuildScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Art");
        EnsureFolder(MaterialsPath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Scene previousActiveScene = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene);

        Material asphalt = Lit("L4_CyberAsphalt", new Color32(12, 19, 31, 255), new Color(.015f, .045f, .085f));
        Material building = Lit("L4_DarkBuilding", new Color32(19, 27, 42, 255));
        Material steel = Lit("L4_Steel", new Color32(33, 43, 58, 255));
        Material cyan = Lit("L4_CyanNeon", new Color32(24, 205, 230, 255), new Color(.03f, .9f, 1f));
        Material pink = Lit("L4_PinkNeon", new Color32(218, 39, 148, 255), new Color(1f, .025f, .43f));
        Material gold = Lit("L4_HomeGold", new Color32(255, 190, 72, 255), new Color(1f, .56f, .08f));
        Material homeWhite = Lit("L4_HomeWhite", new Color32(204, 255, 242, 255), new Color(.52f, 1f, .91f));
        Material sign = Lit("L4_Sign", new Color32(19, 26, 42, 255), new Color(.025f, .04f, .09f));

        GameObject world = new GameObject("Level4_CyberCityMaze");
        Make("Continuous city road foundation", PrimitiveType.Cube, world.transform, new Vector3(0f, -.48f, 0f), new Vector3(86f, .9f, 90f), asphalt, true);

        bool[,] open = GenerateMaze();
        System.Random random = new System.Random(408);
        for (int row = 0; row < MazeSize; row++)
        {
            for (int col = 0; col < MazeSize; col++)
            {
                Vector3 center = CellPosition(row, col);
                if (open[row, col])
                {
                    GameObject tile = Make("Street Tile " + row + " " + col, PrimitiveType.Cube, world.transform,
                        center + Vector3.up * .035f, new Vector3(CellSize, .07f, CellSize), asphalt, false);
                    if ((row + col) % 4 == 0)
                        Make("Faint lane marker", PrimitiveType.Cube, world.transform,
                            center + Vector3.up * .078f, new Vector3(.07f, .018f, .75f), row % 2 == 0 ? cyan : pink, false);
                    continue;
                }

                float height = row == 0 || col == 0 || row == MazeSize - 1 || col == MazeSize - 1
                    ? random.Next(12, 19) : random.Next(5, 10);
                GameObject block = Make("Maze Building " + row + " " + col, PrimitiveType.Cube, world.transform,
                    center + Vector3.up * height * .5f, new Vector3(CellSize, height, CellSize), building, true);
                block.transform.rotation = Quaternion.Euler(0f, random.Next(-3, 4), 0f);

                if (random.Next(0, 5) == 0)
                    AddNeonFacade(world.transform, open, row, col, height, random.Next(0, 2) == 0 ? cyan : pink);
                if (height > 7f && random.Next(0, 3) == 0)
                    Make("Rooftop antenna", PrimitiveType.Cube, world.transform,
                        center + Vector3.up * (height + 1.3f), new Vector3(.13f, 2.6f, .13f), random.Next(0, 2) == 0 ? cyan : pink, false);
            }
        }

        AddStreetlights(world.transform, open, cyan, pink);
        BuildEntrance(world.transform, steel, cyan, sign);
        Transform homeBeacon = BuildHome(world.transform, steel, gold, homeWhite, pink);
        BuildCityHorizon(world.transform, building, cyan, pink);

        GameObject player = new GameObject("PlayerController");
        player.transform.SetPositionAndRotation(CellPosition(MazeSize - 1, Center) + Vector3.up, Quaternion.identity);
        CharacterController character = player.AddComponent<CharacterController>();
        character.height = 2f;
        character.radius = .46f;
        PlayerController playerController = player.AddComponent<PlayerController>();
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Capsule";
        capsule.transform.SetParent(player.transform, false);
        capsule.transform.localPosition = Vector3.zero;
        Object.DestroyImmediate(capsule.GetComponent<Collider>());
        SerializedObject playerSettings = new SerializedObject(playerController);
        playerSettings.FindProperty("playerVisual").objectReferenceValue = capsule.transform;
        playerSettings.FindProperty("moveSpeed").floatValue = 4.5f;
        playerSettings.FindProperty("cameraDistance").floatValue = 3.7f;
        playerSettings.FindProperty("cameraHeight").floatValue = 1.7f;
        playerSettings.ApplyModifiedPropertiesWithoutUndo();

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 72f;
        camera.farClipPlane = 180f;
        cameraObject.AddComponent<AudioListener>();
        SerializedObject cameraSettings = new SerializedObject(playerController);
        cameraSettings.FindProperty("cameraPivot").objectReferenceValue = cameraObject.transform;
        cameraSettings.ApplyModifiedPropertiesWithoutUndo();

        Light key = new GameObject("Blue City Moonlight").AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color32(116, 158, 205, 255);
        key.intensity = .58f;
        key.transform.rotation = Quaternion.Euler(32f, -28f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(33, 42, 61, 255);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color32(13, 20, 34, 255);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = .0028f;

        Text instructions, progress;
        BuildHud(out instructions, out progress);
        AudioSource music = new GameObject("Level 4 — Homebound Ambience").AddComponent<AudioSource>();
        music.clip = FindFearTrack();
        music.loop = true;
        music.playOnAwake = true;
        music.spatialBlend = 0f;
        music.volume = .22f;

        CyberMazeMission mission = new GameObject("Cyber Maze Ending").AddComponent<CyberMazeMission>();
        mission.Configure(player.transform, capsule.transform, homeBeacon, instructions, progress, music);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddBuildScene("Assets/Scenes/SampleScene.unity");
        AddBuildScene("Assets/Scenes/Level2_DotCircuit.unity");
        AddBuildScene("Assets/Scenes/Level3_NeonCourier.unity");
        AddBuildScene(ScenePath);
        ConnectLevelThree();
        Selection.activeGameObject = player;
        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            SceneManager.SetActiveScene(previousActiveScene);
        EditorSceneManager.SetActiveScene(scene);
    }

    private static bool[,] GenerateMaze()
    {
        bool[,] open = new bool[MazeSize, MazeSize];
        bool[,] visited = new bool[MazeSize, MazeSize];
        System.Random random = new System.Random(20261023);
        List<Vector2Int> stack = new List<Vector2Int>();
        Vector2Int start = new Vector2Int(Center, MazeSize - 2);
        stack.Add(start);
        visited[start.y, start.x] = true;
        open[start.y, start.x] = true;
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (stack.Count > 0)
        {
            Vector2Int current = stack[stack.Count - 1];
            List<Vector2Int> candidates = new List<Vector2Int>();
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i] * 2;
                if (next.x <= 0 || next.x >= MazeSize - 1 || next.y <= 0 || next.y >= MazeSize - 1) continue;
                if (!visited[next.y, next.x]) candidates.Add(next);
            }

            if (candidates.Count == 0)
            {
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            Vector2Int chosen = candidates[random.Next(candidates.Count)];
            Vector2Int between = (current + chosen) / 2;
            open[between.y, between.x] = true;
            open[chosen.y, chosen.x] = true;
            visited[chosen.y, chosen.x] = true;
            stack.Add(chosen);
        }

        open[MazeSize - 1, Center] = true;
        open[0, Center] = true;
        return open;
    }

    private static Vector3 CellPosition(int row, int col)
    {
        return new Vector3((col - Center) * CellSize, 0f, (Center - row) * CellSize);
    }

    private static void AddNeonFacade(Transform parent, bool[,] open, int row, int col, float height, Material material)
    {
        Vector3 center = CellPosition(row, col);
        Vector3 offset;
        Vector3 scale;
        if (col > 0 && open[row, col - 1])
        {
            offset = new Vector3(-CellSize * .501f, height * .63f, 0f);
            scale = new Vector3(.055f, Mathf.Min(2.3f, height * .32f), 1.6f);
        }
        else if (col < MazeSize - 1 && open[row, col + 1])
        {
            offset = new Vector3(CellSize * .501f, height * .63f, 0f);
            scale = new Vector3(.055f, Mathf.Min(2.3f, height * .32f), 1.6f);
        }
        else if (row > 0 && open[row - 1, col])
        {
            offset = new Vector3(0f, height * .63f, CellSize * .501f);
            scale = new Vector3(1.6f, Mathf.Min(2.3f, height * .32f), .055f);
        }
        else
        {
            offset = new Vector3(0f, height * .63f, -CellSize * .501f);
            scale = new Vector3(1.6f, Mathf.Min(2.3f, height * .32f), .055f);
        }
        Make("Neon shopfront", PrimitiveType.Cube, parent, center + offset, scale, material, false);
    }

    private static void AddStreetlights(Transform parent, bool[,] open, Material cyan, Material pink)
    {
        int count = 0;
        for (int row = 1; row < MazeSize - 1; row += 4)
        {
            for (int col = 1; col < MazeSize - 1; col += 4)
            {
                if (!open[row, col] || count++ % 2 != 0) continue;
                Vector3 position = CellPosition(row, col) + new Vector3(1.25f, 0f, .4f);
                Make("Neon lamp post", PrimitiveType.Cube, parent, position + Vector3.up * 2.1f, new Vector3(.09f, 4.2f, .09f), cyan, false);
                Make("Neon lamp arm", PrimitiveType.Cube, parent, position + new Vector3(-.3f, 4.1f, 0f), new Vector3(.75f, .08f, .08f), pink, false);
                Make("Neon lamp", PrimitiveType.Cube, parent, position + new Vector3(-.64f, 4.04f, 0f), new Vector3(.42f, .12f, .18f), cyan, false);
            }
        }
    }

    private static void BuildEntrance(Transform parent, Material steel, Material cyan, Material sign)
    {
        Vector3 start = CellPosition(MazeSize - 1, Center);
        Make("Entry arch left", PrimitiveType.Cube, parent, start + new Vector3(-2.2f, 2.3f, 2.6f), new Vector3(.28f, 4.6f, .28f), steel, true);
        Make("Entry arch right", PrimitiveType.Cube, parent, start + new Vector3(2.2f, 2.3f, 2.6f), new Vector3(.28f, 4.6f, .28f), steel, true);
        Make("Entry arch neon", PrimitiveType.Cube, parent, start + new Vector3(0f, 4.5f, 2.6f), new Vector3(4.65f, .16f, .2f), cyan, false);
        WorldLabel(parent, "CITY MAZE  /  FIND HOME", start + new Vector3(0f, 5f, 2.9f), new Color(.38f, .94f, 1f), .43f);
        Make("Entry signal panel", PrimitiveType.Cube, parent, start + new Vector3(0f, 2.6f, 4.3f), new Vector3(3.3f, .82f, .12f), sign, false);
        WorldLabel(parent, "SIGNAL LOST", start + new Vector3(0f, 2.58f, 4.38f), new Color(1f, .28f, .62f), .25f);
    }

    private static Transform BuildHome(Transform parent, Material steel, Material gold, Material white, Material pink)
    {
        Vector3 home = new Vector3(0f, 0f, (Center + 2) * CellSize);
        Make("Home plaza", PrimitiveType.Cube, parent, home + new Vector3(0f, -.06f, 0f), new Vector3(12f, .12f, 9f), steel, true);
        Make("Home left pillar", PrimitiveType.Cube, parent, home + new Vector3(-3.8f, 3f, 1.1f), new Vector3(.45f, 6f, .55f), steel, true);
        Make("Home right pillar", PrimitiveType.Cube, parent, home + new Vector3(3.8f, 3f, 1.1f), new Vector3(.45f, 6f, .55f), steel, true);
        Make("Home luminous lintel", PrimitiveType.Cube, parent, home + new Vector3(0f, 6.05f, 1.1f), new Vector3(8.1f, .5f, .65f), gold, false);
        Make("Home glowing doorway", PrimitiveType.Cube, parent, home + new Vector3(0f, 2.8f, 1.3f), new Vector3(3.2f, 5.5f, .22f), white, false);
        Make("Home door frame left", PrimitiveType.Cube, parent, home + new Vector3(-1.8f, 2.9f, 1.35f), new Vector3(.18f, 5.8f, .25f), gold, false);
        Make("Home door frame right", PrimitiveType.Cube, parent, home + new Vector3(1.8f, 2.9f, 1.35f), new Vector3(.18f, 5.8f, .25f), pink, false);
        Make("Home halo", PrimitiveType.Cylinder, parent, home + new Vector3(0f, .1f, -1.8f), new Vector3(5.4f, .08f, 5.4f), gold, false);
        WorldLabel(parent, "HOME", home + new Vector3(0f, 7.3f, 1.3f), new Color(.8f, 1f, .88f), .76f);

        GameObject beacon = new GameObject("HOME Beacon");
        beacon.transform.SetParent(parent, false);
        beacon.transform.position = home + new Vector3(0f, 0f, -1.5f);
        GameObject beaconRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beaconRing.name = "Home beacon glow";
        beaconRing.transform.SetParent(beacon.transform, false);
        beaconRing.transform.localPosition = new Vector3(0f, .08f, 0f);
        beaconRing.transform.localScale = new Vector3(6.2f, .08f, 6.2f);
        beaconRing.GetComponent<Renderer>().sharedMaterial = gold;
        Object.DestroyImmediate(beaconRing.GetComponent<Collider>());
        return beacon.transform;
    }

    private static void BuildCityHorizon(Transform parent, Material building, Material cyan, Material pink)
    {
        for (int i = 0; i < 16; i++)
        {
            float x = -54f + i * 7.2f;
            float height = 14f + (i * 19 % 17);
            float z = i % 2 == 0 ? -48f : 48f;
            Make("Distant megatower", PrimitiveType.Cube, parent, new Vector3(x, height * .5f, z), new Vector3(6f, height, 7f), building, true);
            Make("Tower vertical neon", PrimitiveType.Cube, parent, new Vector3(x + 2.4f, height * .62f, z - 3.55f), new Vector3(.12f, height * .45f, .08f), i % 2 == 0 ? cyan : pink, false);
        }
    }

    private static void BuildHud(out Text instructions, out Text progress)
    {
        GameObject canvasObject = new GameObject("Level 4 HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;

        GameObject panel = new GameObject("Maze Instruction Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(26f, -26f);
        panelRect.sizeDelta = new Vector2(580f, 158f);
        panel.GetComponent<Image>().color = new Color(.012f, .025f, .045f, .91f);

        Text title = TextAt(panel.transform, "LEVEL 4 — THE WAY HOME", new Vector2(18f, -10f), new Vector2(540f, 30f), 21, new Color(.34f, .95f, 1f));
        title.fontStyle = FontStyle.Bold;
        instructions = TextAt(panel.transform, "", new Vector2(18f, -48f), new Vector2(540f, 70f), 16, Color.white);
        progress = TextAt(panel.transform, "", new Vector2(18f, -121f), new Vector2(540f, 24f), 15, new Color(1f, .79f, .39f));
    }

    private static Text TextAt(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject gameObject = new GameObject("HUD Text", typeof(RectTransform), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = gameObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void WorldLabel(Transform parent, string value, Vector3 position, Color color, float characterSize)
    {
        GameObject gameObject = new GameObject(value, typeof(TextMesh));
        gameObject.transform.SetParent(parent, true);
        gameObject.transform.position = position;
        gameObject.transform.rotation = Quaternion.identity;
        TextMesh text = gameObject.GetComponent<TextMesh>();
        text.text = value;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = characterSize;
        text.fontSize = 48;
        text.color = color;
    }

    private static GameObject Make(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, true);
        gameObject.transform.SetPositionAndRotation(position, Quaternion.identity);
        gameObject.transform.localScale = scale;
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        if (!collider)
        {
            Collider objectCollider = gameObject.GetComponent<Collider>();
            if (objectCollider != null) Object.DestroyImmediate(objectCollider);
        }
        return gameObject;
    }

    private static Material Lit(string name, Color color, Color? emission = null)
    {
        string path = MaterialsPath + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (emission.HasValue && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value * 2f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static AudioClip FindFearTrack()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path).Replace("_", "-").Replace(" ", "-");
            if (name.Equals("freshmansound-fear", System.StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }

    private static void ConnectLevelThree()
    {
        const string levelThreePath = "Assets/Scenes/Level3_NeonCourier.unity";
        if (!File.Exists(levelThreePath)) return;
        Scene active = SceneManager.GetActiveScene();
        Scene levelThree = SceneManager.GetSceneByPath(levelThreePath);
        if (!levelThree.IsValid() || !levelThree.isLoaded)
            levelThree = EditorSceneManager.OpenScene(levelThreePath, OpenSceneMode.Additive);

        foreach (NeonCourierMission mission in Object.FindObjectsByType<NeonCourierMission>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mission.gameObject.scene.path != levelThreePath) continue;
            SerializedObject settings = new SerializedObject(mission);
            settings.FindProperty("nextSceneName").stringValue = "Level4_CyberMaze";
            settings.ApplyModifiedProperties();
            EditorUtility.SetDirty(mission);
        }
        EditorSceneManager.SaveScene(levelThree);
        if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
    }

    private static void AddBuildScene(string path)
    {
        if (!File.Exists(path)) return;
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (!scenes.Any(scene => scene.path == path))
            EditorBuildSettings.scenes = scenes.Concat(new[] { new EditorBuildSettingsScene(path, true) }).ToArray();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
