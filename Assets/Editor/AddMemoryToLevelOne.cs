using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using TheLastMemory;

public static class AddMemoryToLevelOne
{
    [MenuItem("The Last Memory/Level 1/Set Music to 50s and Add Memory Trial")]
    private static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Stop Play Mode before changing the level.", "OK");
            return;
        }
        const string levelOnePath = "Assets/Scenes/SampleScene.unity";
        if (!File.Exists(levelOnePath))
        {
            EditorUtility.DisplayDialog("Level 1 scene not found", "Expected Assets/Scenes/SampleScene.unity in this project.", "OK");
            return;
        }
        if (SceneManager.GetActiveScene().path != levelOnePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(levelOnePath, OpenSceneMode.Single);
        }
        EchoSequencePuzzle echo = Object.FindFirstObjectByType<EchoSequencePuzzle>();
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (echo == null || player == null)
        {
            EditorUtility.DisplayDialog("Open Level 1 first", "Open Assets/Scenes/SampleScene.unity. It must contain the working Echo puzzle and PlayerController.", "OK");
            return;
        }
        if (Object.FindObjectsByType<MemorySequencePuzzle>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            EditorUtility.DisplayDialog("Memory trial already added", "Level1_MemoryTrial already exists in this scene.", "OK");
            return;
        }

        SerializedObject echoObject = new SerializedObject(echo);
        echoObject.FindProperty("timeLimit").floatValue = 50f;
        echoObject.FindProperty("maxMistakes").intValue = 2;
        echoObject.ApplyModifiedProperties();

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject != null) mainCamera = cameraObject.GetComponent<Camera>();
        }
        InteractionController interaction = player.GetComponent<InteractionController>();
        if (interaction == null) interaction = Undo.AddComponent<InteractionController>(player.gameObject);
        if (mainCamera != null)
        {
            mainCamera.tag = "MainCamera";
            SerializedObject interactionObject = new SerializedObject(interaction);
            interactionObject.FindProperty("playerCamera").objectReferenceValue = mainCamera;
            interactionObject.FindProperty("interactionDistance").floatValue = 12f;
            interactionObject.ApplyModifiedProperties();
        }

        GameObject memoryRoot = new GameObject("Level1_MemoryTrial");
        Undo.RegisterCreatedObjectUndo(memoryRoot, "Add Level 1 memory trial");
        Vector3 forward = player.transform.forward;
        Vector3 right = player.transform.right;
        Vector3 chamberCenter = player.transform.position + forward * 22f;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(floor, "Create memory trial floor");
        floor.name = "Memory Trial Floor";
        floor.transform.SetParent(memoryRoot.transform, true);
        floor.transform.position = new Vector3(chamberCenter.x, 0f, chamberCenter.z);
        floor.transform.localScale = new Vector3(2f, 1f, 3f);

        GameObject managerObject = new GameObject("MemorySequencePuzzle");
        Undo.RegisterCreatedObjectUndo(managerObject, "Create memory puzzle manager");
        managerObject.transform.SetParent(memoryRoot.transform, true);
        managerObject.transform.position = chamberCenter;
        MemorySequencePuzzle puzzle = Undo.AddComponent<MemorySequencePuzzle>(managerObject);
        SerializedObject puzzleSettings = new SerializedObject(puzzle);
        puzzleSettings.FindProperty("timeLimit").floatValue = 60f;
        puzzleSettings.FindProperty("maxMistakes").intValue = 2;
        puzzleSettings.FindProperty("finishesLevelOne").boolValue = true;
        puzzleSettings.ApplyModifiedProperties();

        PrimitiveType[] shapes = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Cylinder, PrimitiveType.Capsule };
        Color[] colors = { new Color(0.1f, 0.85f, 1f), new Color(0.2f, 1f, 0.45f), new Color(0.65f, 0.35f, 1f), new Color(1f, 0.25f, 0.6f) };
        MemorySymbolButton[] buttons = new MemorySymbolButton[4];
        for (int i = 0; i < buttons.Length; i++)
        {
            Vector3 position = chamberCenter + right * ((i - 1.5f) * 2.2f);
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(pedestal, "Create memory symbol pedestal");
            pedestal.name = "Memory Symbol Pedestal " + (i + 1);
            pedestal.transform.SetParent(memoryRoot.transform, true);
            pedestal.transform.position = new Vector3(position.x, 0.35f, position.z);
            pedestal.transform.localScale = new Vector3(1.5f, 0.7f, 1.5f);

            GameObject symbol = GameObject.CreatePrimitive(shapes[i]);
            Undo.RegisterCreatedObjectUndo(symbol, "Create memory symbol");
            symbol.name = "Memory Symbol " + (i + 1) + " - " + shapes[i];
            symbol.transform.SetParent(memoryRoot.transform, true);
            symbol.transform.position = new Vector3(position.x, 1.2f, position.z);
            symbol.transform.localScale = shapes[i] == PrimitiveType.Cylinder
                ? new Vector3(0.8f, 0.85f, 0.8f)
                : new Vector3(0.9f, 0.9f, 0.9f);
            MemorySymbolButton button = Undo.AddComponent<MemorySymbolButton>(symbol);
            button.Configure(puzzle, i, colors[i]);
            buttons[i] = button;
        }
        puzzle.Configure(buttons);

        GameObject spawn = new GameObject("Memory Trial Spawn");
        Undo.RegisterCreatedObjectUndo(spawn, "Create memory trial spawn");
        spawn.transform.SetParent(memoryRoot.transform, true);
        spawn.transform.SetPositionAndRotation(player.transform.position + forward * 15f, player.transform.rotation);

        SerializedObject echoSettings = new SerializedObject(echo);
        echoSettings.FindProperty("nextPuzzle").objectReferenceValue = puzzle;
        echoSettings.FindProperty("nextStageSpawn").objectReferenceValue = spawn.transform;
        echoSettings.ApplyModifiedProperties();

        memoryRoot.SetActive(false);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = echo.gameObject;
        EditorUtility.DisplayDialog("Level 1 updated", "The sound trial now has 50 seconds and two mistakes. After success, a message confirms the sound and visual cues, then the player moves to the memory trial. Complete its three rounds to finish Level 1.", "OK");
    }
}
