using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TheLastMemory;

public static class RepairPlayerSetup
{
    [MenuItem("The Last Memory/Repair Player and Interaction Setup")]
    private static void Repair()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Stop Play Mode, then run this setup command again.", "OK");
            return;
        }

        GameObject player = GameObject.Find("PlayerController");
        GameObject visual = GameObject.Find("Capsule");
        if (player == null || visual == null)
        {
            EditorUtility.DisplayDialog("Setup needs the existing objects", "The open scene must contain objects named PlayerController and Capsule.", "OK");
            return;
        }

        Undo.RecordObject(player.transform, "Repair player setup");
        Undo.RecordObject(visual.transform, "Repair player setup");
        player.transform.position = visual.transform.position;

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController == null)
            Undo.AddComponent<CharacterController>(player);

        PlayerController movement = player.GetComponent<PlayerController>();
        if (movement == null)
            movement = Undo.AddComponent<PlayerController>(player);

        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        CapsuleCollider visualCollider = visual.GetComponent<CapsuleCollider>();
        if (visualCollider != null)
            Undo.DestroyObjectImmediate(visualCollider);

        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject != null) playerCamera = cameraObject.GetComponent<Camera>();
        }

        if (playerCamera == null)
        {
            EditorUtility.DisplayDialog("Camera not found", "The open scene needs a Camera component on an object named Main Camera.", "OK");
            return;
        }

        playerCamera.tag = "MainCamera";
        playerCamera.transform.SetParent(player.transform, true);
        SerializedObject movementObject = new SerializedObject(movement);
        movementObject.FindProperty("cameraPivot").objectReferenceValue = playerCamera.transform;
        movementObject.FindProperty("playerVisual").objectReferenceValue = visual.transform;
        movementObject.ApplyModifiedProperties();

        InteractionController interaction = player.GetComponent<InteractionController>();
        if (interaction == null)
            interaction = Undo.AddComponent<InteractionController>(player);
        SerializedObject interactionObject = new SerializedObject(interaction);
        interactionObject.FindProperty("playerCamera").objectReferenceValue = playerCamera;
        interactionObject.FindProperty("interactionDistance").floatValue = 8f;
        interactionObject.ApplyModifiedProperties();

        GameObject testTarget = GameObject.Find("InteractionTestCube");
        if (testTarget == null)
        {
            testTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(testTarget, "Create interaction test cube");
            testTarget.name = "InteractionTestCube";
        }
        Undo.RecordObject(testTarget.transform, "Position interaction test cube");
        testTarget.transform.position = player.transform.position + player.transform.forward * 2.5f;
        testTarget.transform.localScale = Vector3.one * 0.8f;
        if (testTarget.GetComponent<BoxCollider>() == null)
            Undo.AddComponent<BoxCollider>(testTarget);
        if (testTarget.GetComponent<MessageInteractable>() == null)
            Undo.AddComponent<MessageInteractable>(testTarget);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = player;
        EditorUtility.DisplayDialog("Player setup repaired", "Press Play, click the Game view, aim at the cube in front of the player, and press E. The Console should show: Interaction successful.", "OK");
    }
}
