using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TheLastMemory;

public static class CreateEchoPuzzle
{
    [MenuItem("The Last Memory/Create Level 1 Echo Puzzle")]
    private static void Create()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Stop Play Mode before creating the puzzle.", "OK");
            return;
        }
        if (GameObject.Find("Level1_EchoPuzzle") != null)
        {
            EditorUtility.DisplayDialog("Puzzle already exists", "Level1_EchoPuzzle is already in this scene. Delete it first if you want to rebuild it.", "OK");
            return;
        }

        GameObject player = GameObject.Find("PlayerController");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Player not found", "This scene needs the PlayerController object first. Run Repair Player and Interaction Setup, then try again.", "OK");
            return;
        }

        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject != null) playerCamera = cameraObject.GetComponent<Camera>();
        }
        if (playerCamera == null)
        {
            EditorUtility.DisplayDialog("Camera not found", "This scene needs a Main Camera.", "OK");
            return;
        }
        playerCamera.tag = "MainCamera";

        InteractionController interaction = player.GetComponent<InteractionController>();
        if (interaction == null) interaction = Undo.AddComponent<InteractionController>(player);
        SerializedObject interactionObject = new SerializedObject(interaction);
        interactionObject.FindProperty("playerCamera").objectReferenceValue = playerCamera;
        interactionObject.FindProperty("interactionDistance").floatValue = 12f;
        interactionObject.ApplyModifiedProperties();

        GameObject root = new GameObject("Level1_EchoPuzzle");
        Undo.RegisterCreatedObjectUndo(root, "Create Level 1 echo puzzle");
        EchoSequencePuzzle puzzle = Undo.AddComponent<EchoSequencePuzzle>(root);
        SerializedObject puzzleObject = new SerializedObject(puzzle);
        puzzleObject.FindProperty("timeLimit").floatValue = 50f;
        puzzleObject.FindProperty("maxMistakes").intValue = 2;
        puzzleObject.ApplyModifiedProperties();

        EchoToneMachine[] machines = new EchoToneMachine[3];
        Color[] colors = { new Color(0.1f, 0.8f, 1f), new Color(0.2f, 1f, 0.55f), new Color(0.5f, 0.45f, 1f) };
        Vector3 lineCenter = player.transform.position + player.transform.forward * 3.5f;
        Vector3 right = player.transform.right;
        for (int i = 0; i < machines.Length; i++)
        {
            GameObject machine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(machine, "Create echo tone machine");
            machine.name = "Tone Machine " + (i + 1);
            machine.transform.SetParent(root.transform, true);
            machine.transform.position = lineCenter + right * ((i - 1) * 1.7f);
            machine.transform.position = new Vector3(machine.transform.position.x, 0.8f, machine.transform.position.z);
            machine.transform.localScale = new Vector3(0.8f, 1.6f, 0.8f);

            EchoToneMachine toneMachine = Undo.AddComponent<EchoToneMachine>(machine);
            toneMachine.Configure(puzzle, i, colors[i]);
            machines[i] = toneMachine;
        }
        puzzle.Configure(machines);

        GameObject testCube = GameObject.Find("InteractionTestCube");
        if (testCube != null) Undo.DestroyObjectImmediate(testCube);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Echo puzzle created", "Press Play. Watch and listen to the four machine cues, then aim at the machines and press E in the same order. You have 50 seconds and two mistakes. Press R to retry after failure.", "OK");
    }
}
