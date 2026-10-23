using System.IO;
using TheLastMemory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class RebuildAuditoryLevelOne
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ArtFolder = "Assets/Art";
    private const string MaterialFolder = "Assets/Art/Materials";

    [MenuItem("The Last Memory/Level 1/Build Auditory Level One")]
    private static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.playModeStateChanged -= ContinueAfterPlayMode;
            EditorApplication.playModeStateChanged += ContinueAfterPlayMode;
            EditorApplication.isPlaying = false;
            return;
        }
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        EchoSequencePuzzle echo = Object.FindAnyObjectByType<EchoSequencePuzzle>();
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject != null) mainCamera = cameraObject.GetComponent<Camera>();
        }
        if (player == null || echo == null || mainCamera == null)
        {
            EditorUtility.DisplayDialog("Open the existing Level 1 scene", "SampleScene needs its PlayerController, Main Camera and working Echo puzzle before I can rebuild its theme.", "OK");
            return;
        }

        EnsureFolder(ArtFolder);
        EnsureFolder(MaterialFolder);

        // The old scene is already copied to Backups/SampleScene-before-auditory-rebuild-2026-10-23.unity.
        RemoveByName("Level1_MemoryTrial");
        RemoveByName("Memory Trial Floor");
        RemoveByName("MemorySequencePuzzle");
        RemoveByName("Plane");
        RemoveByName("Level1_Auditory_Environment");

        Material ground = MakeMaterial("Auditory_Ground", new Color32(6, 24, 28, 255));
        Material wall = MakeMaterial("Auditory_Backdrop", new Color32(4, 17, 20, 255));
        Material silhouette = MakeMaterial("Auditory_Crowd", new Color32(5, 11, 16, 255));
        Material cyan = MakeMaterial("Auditory_CyanGlow", new Color32(25, 235, 239, 255), new Color(0.05f, 0.95f, 1f));
        Material machineCyan = MakeMaterial("Tone_Cyan", new Color32(15, 112, 126, 255), new Color(0.03f, 0.65f, 0.85f));
        Material machineGreen = MakeMaterial("Tone_Green", new Color32(18, 105, 80, 255), new Color(0.05f, 0.75f, 0.48f));
        Material machineViolet = MakeMaterial("Tone_Violet", new Color32(65, 52, 104, 255), new Color(0.38f, 0.20f, 0.9f));

        GameObject theme = new GameObject("Level1_Auditory_Environment");
        Undo.RegisterCreatedObjectUndo(theme, "Build auditory environment");
        Transform themeRoot = theme.transform;

        CreatePrimitive("Stage Floor", PrimitiveType.Plane, themeRoot, new Vector3(0f, 0f, 10f), new Vector3(4f, 1f, 6f), ground, true);
        CreatePrimitive("Rear Wall", PrimitiveType.Cube, themeRoot, new Vector3(0f, 6f, 22f), new Vector3(56f, 12f, 1f), wall, false);
        CreatePrimitive("Left Wall", PrimitiveType.Cube, themeRoot, new Vector3(-28f, 6f, 9f), new Vector3(1f, 12f, 26f), wall, false);
        CreatePrimitive("Right Wall", PrimitiveType.Cube, themeRoot, new Vector3(28f, 6f, 9f), new Vector3(1f, 12f, 26f), wall, false);

        BuildCrowd(themeRoot, silhouette);
        BuildMemoryFigure(themeRoot, cyan);

        player.transform.SetPositionAndRotation(new Vector3(0f, 1f, 0f), Quaternion.identity);
        HalfHumanHalfRobotAvatarBuilder.BuildFor(player);

        InteractionController playerInteraction = player.GetComponent<InteractionController>();
        if (playerInteraction == null) playerInteraction = Undo.AddComponent<InteractionController>(player.gameObject);
        foreach (InteractionController interaction in Object.FindObjectsByType<InteractionController>(FindObjectsInactive.Include))
        {
            if (interaction != playerInteraction) Undo.DestroyObjectImmediate(interaction);
        }
        mainCamera.tag = "MainCamera";
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color32(3, 14, 17, 255);
        SerializedObject interactionSettings = new SerializedObject(playerInteraction);
        interactionSettings.FindProperty("playerCamera").objectReferenceValue = mainCamera;
        interactionSettings.FindProperty("interactionDistance").floatValue = 12f;
        interactionSettings.ApplyModifiedProperties();

        Material[] tones = { machineCyan, machineGreen, machineViolet };
        for (int i = 0; i < 3; i++)
        {
            GameObject machine = GameObject.Find("Tone Machine " + (i + 1));
            if (machine == null) continue;
            Undo.RecordObject(machine.transform, "Arrange tone machines");
            machine.transform.SetPositionAndRotation(new Vector3((i - 1) * 2.6f, 0.95f, 6.5f), Quaternion.identity);
            machine.transform.localScale = new Vector3(1.2f, 1.9f, 1.2f);
            Renderer renderer = machine.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.sharedMaterial = tones[i];
            AddToneLabel(themeRoot, i + 1, machine.transform.position + Vector3.up * 1.25f);
        }

        SerializedObject echoSettings = new SerializedObject(echo);
        echoSettings.FindProperty("timeLimit").floatValue = 50f;
        echoSettings.FindProperty("maxMistakes").intValue = 2;
        echoSettings.FindProperty("nextPuzzle").objectReferenceValue = null;
        echoSettings.FindProperty("nextStageSpawn").objectReferenceValue = null;
        echoSettings.ApplyModifiedProperties();

        Light keyLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
        if (keyLight != null)
        {
            Undo.RecordObject(keyLight, "Set auditory level lighting");
            keyLight.color = new Color32(115, 190, 198, 255);
            keyLight.intensity = 0.65f;
            keyLight.transform.rotation = Quaternion.Euler(34f, -28f, 0f);
        }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(11, 28, 32, 255);
        AddCyanSpotlight(themeRoot, new Vector3(0f, 7f, 14f), new Vector3(16f, 0f, 0f));

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = theme;
        EditorUtility.DisplayDialog("Auditory Level 1 is ready", "SampleScene now contains only the sound-sequence game. It has a dark teal crowd setting, a glowing cyan memory figure, three tone machines, a 50-second timer, and a two-mistake loss limit. Press Play to begin.", "OK");
    }

    private static void ContinueAfterPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= ContinueAfterPlayMode;
        EditorApplication.delayCall += Build;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static Material MakeMaterial(string name, Color baseColor, Color? emissionColor = null)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = baseColor;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
        if (emissionColor.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emissionColor.Value * 1.4f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void RemoveByName(string exactName)
    {
        foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (candidate.name == exactName)
            {
                Undo.DestroyObjectImmediate(candidate);
                return;
            }
        }
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        Undo.RegisterCreatedObjectUndo(item, "Create " + name);
        item.name = name;
        item.transform.SetParent(parent, true);
        item.transform.SetPositionAndRotation(position, Quaternion.identity);
        item.transform.localScale = scale;
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        if (!keepCollider)
        {
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
        }
        return item;
    }

    private static void BuildCrowd(Transform parent, Material material)
    {
        float[] columns = { -13f, -10f, -7.5f, -5.3f, -4.1f, 4.1f, 5.3f, 7.5f, 10f, 13f };
        for (int row = 0; row < 2; row++)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                float x = columns[i] + ((i + row) % 2 == 0 ? 0f : 0.7f);
                float z = 15.2f + row * 2.8f;
                float height = 1.6f + ((i * 3 + row) % 4) * 0.12f;
                GameObject body = CreatePrimitive("Crowd silhouette", PrimitiveType.Capsule, parent,
                    new Vector3(x, height * 0.58f, z), new Vector3(0.78f, height * 0.6f, 0.64f), material, false);
                body.transform.rotation = Quaternion.Euler(0f, (i % 3 - 1) * 8f, 0f);
                CreatePrimitive("Crowd silhouette head", PrimitiveType.Sphere, parent,
                    new Vector3(x, height + 0.35f, z), new Vector3(0.7f, 0.75f, 0.68f), material, false);
            }
        }
    }

    private static void BuildMemoryFigure(Transform parent, Material glow)
    {
        Vector3 headCenter = new Vector3(0f, 5.8f, 20.7f);
        CreatePrimitive("Faceless memory figure", PrimitiveType.Sphere, parent, headCenter,
            new Vector3(1.55f, 2.05f, 0.72f), glow, false);
        CreatePrimitive("Memory figure shoulders", PrimitiveType.Sphere, parent, new Vector3(0f, 3.45f, 20.8f),
            new Vector3(3.7f, 1.05f, 0.65f), MakeMaterial("Auditory_FigureBody", new Color32(9, 61, 67, 255), new Color(0.02f, 0.2f, 0.23f)), false);

        for (int ring = 0; ring < 5; ring++)
        {
            GameObject lineObject = new GameObject("Memory signal orbit " + (ring + 1));
            Undo.RegisterCreatedObjectUndo(lineObject, "Create memory signal orbit");
            lineObject.transform.SetParent(parent, false);
            lineObject.transform.position = headCenter;
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 81;
            line.startWidth = line.endWidth = 0.035f + (ring % 2) * 0.012f;
            line.sharedMaterial = glow;
            line.startColor = line.endColor = new Color(0.18f, 1f, 1f, 0.88f);
            Quaternion tilt = Quaternion.Euler(ring * 31f - 55f, ring * 43f, ring * 27f);
            float radiusX = 1.15f + ring * 0.11f;
            float radiusY = 1.7f + ring * 0.08f;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / 80f * Mathf.PI * 2f;
                Vector3 point = new Vector3(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY, Mathf.Sin(angle * 2.4f + ring) * 0.35f);
                line.SetPosition(i, tilt * point);
            }
        }
        Vector3[] sparks =
        {
            new Vector3(-2.2f, 7.1f, 20.6f), new Vector3(2.3f, 6.7f, 20.8f),
            new Vector3(-1.9f, 4.5f, 20.7f), new Vector3(1.8f, 7.8f, 20.8f),
            new Vector3(0.2f, 8.1f, 20.8f), new Vector3(-2.7f, 6.0f, 20.8f)
        };
        foreach (Vector3 spark in sparks)
            CreatePrimitive("Memory spark", PrimitiveType.Sphere, parent, spark, Vector3.one * 0.12f, glow, false);
    }

    private static void AddToneLabel(Transform parent, int number, Vector3 position)
    {
        GameObject label = new GameObject("Tone label " + number);
        Undo.RegisterCreatedObjectUndo(label, "Label tone machine");
        label.transform.SetParent(parent, true);
        label.transform.position = position;
        TextMesh text = label.AddComponent<TextMesh>();
        text.text = number.ToString();
        text.fontSize = 96;
        text.characterSize = 0.035f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color32(220, 255, 255, 255);
    }

    private static void AddCyanSpotlight(Transform parent, Vector3 position, Vector3 euler)
    {
        GameObject lightObject = new GameObject("Cyan memory light");
        Undo.RegisterCreatedObjectUndo(lightObject, "Create cyan memory light");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color32(35, 235, 255, 255);
        light.intensity = 5f;
        light.range = 20f;
        light.spotAngle = 70f;
    }
}
