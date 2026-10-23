using System.IO;
using TheLastMemory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RepairLevelOneFlow
{
    private const string LevelOnePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("The Last Memory/Level 1/Repair Controls and Connect Areas")]
    private static void Repair()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.playModeStateChanged -= ContinueAfterPlayMode;
            EditorApplication.playModeStateChanged += ContinueAfterPlayMode;
            EditorApplication.isPlaying = false;
            return;
        }
        if (!File.Exists(LevelOnePath))
        {
            EditorUtility.DisplayDialog("Level 1 scene not found", "Expected Assets/Scenes/SampleScene.unity.", "OK");
            return;
        }
        if (SceneManager.GetActiveScene().path != LevelOnePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(LevelOnePath, OpenSceneMode.Single);
        }

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        EchoSequencePuzzle echo = Object.FindFirstObjectByType<EchoSequencePuzzle>();
        MemorySequencePuzzle memory = Object.FindFirstObjectByType<MemorySequencePuzzle>(FindObjectsInactive.Include);
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject != null) mainCamera = cameraObject.GetComponent<Camera>();
        }
        Transform spawn = null;
        foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (candidate.name == "Memory Trial Spawn") { spawn = candidate; break; }
        }
        if (player == null || echo == null || memory == null || mainCamera == null || spawn == null)
        {
            EditorUtility.DisplayDialog("Level 1 setup is incomplete", "I need PlayerController, Main Camera, the Echo puzzle, the Memory puzzle, and Memory Trial Spawn in SampleScene.", "OK");
            return;
        }

        // Keep one input reader on the player. A duplicate on Main Camera made E fire twice.
        InteractionController playerInteraction = player.GetComponent<InteractionController>();
        if (playerInteraction == null) playerInteraction = Undo.AddComponent<InteractionController>(player.gameObject);
        foreach (InteractionController interaction in Object.FindObjectsByType<InteractionController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (interaction != playerInteraction) Undo.DestroyObjectImmediate(interaction);
        }
        mainCamera.tag = "MainCamera";
        SerializedObject interactionSettings = new SerializedObject(playerInteraction);
        interactionSettings.FindProperty("playerCamera").objectReferenceValue = mainCamera;
        interactionSettings.FindProperty("interactionDistance").floatValue = 12f;
        interactionSettings.ApplyModifiedProperties();

        SerializedObject echoSettings = new SerializedObject(echo);
        echoSettings.FindProperty("timeLimit").floatValue = 50f;
        echoSettings.FindProperty("maxMistakes").intValue = 2;
        echoSettings.FindProperty("nextPuzzle").objectReferenceValue = memory;
        echoSettings.FindProperty("nextStageSpawn").objectReferenceValue = spawn;
        echoSettings.ApplyModifiedProperties();

        SerializedObject memorySettings = new SerializedObject(memory);
        memorySettings.FindProperty("timeLimit").floatValue = 60f;
        memorySettings.FindProperty("maxMistakes").intValue = 2;
        memorySettings.FindProperty("finishesLevelOne").boolValue = true;
        memorySettings.ApplyModifiedProperties();

        GameObject memoryRoot = memory.transform.root.gameObject;
        Transform floorTransform = null;
        foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.name == "Memory Trial Floor") { floorTransform = candidate; break; }
        }
        if (floorTransform == null)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Undo.RegisterCreatedObjectUndo(floor, "Create Level 1 connecting floor");
            floor.name = "Memory Trial Floor";
            floorTransform = floor.transform;
        }
        Undo.RecordObject(floorTransform, "Connect Level 1 areas");
        Undo.SetTransformParent(floorTransform, null, "Connect Level 1 floor");
        Vector3 start = player.transform.position;
        Vector3 end = memory.transform.position;
        start.y = end.y = 0f;
        Vector3 path = end - start;
        float distance = path.magnitude;
        floorTransform.position = (start + end) * 0.5f;
        floorTransform.rotation = path.sqrMagnitude > 0.01f ? Quaternion.LookRotation(path.normalized) : player.transform.rotation;
        floorTransform.localScale = new Vector3(2f, 1f, (distance + 12f) / 10f);
        floorTransform.gameObject.SetActive(true);
        memoryRoot.SetActive(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = player.gameObject;
        EditorUtility.DisplayDialog("Level 1 repaired", "There is now one E-key controller. Its ray ignores the player Capsule. The 50-second echo trial, two-mistake limit, memory handoff, spawn point, and continuous floor are connected. Play SampleScene to try it.", "OK");
    }

    private static void ContinueAfterPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= ContinueAfterPlayMode;
        EditorApplication.delayCall += Repair;
    }
}
