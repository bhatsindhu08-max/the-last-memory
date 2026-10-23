using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TheLastMemory;

public static class ConnectLevelOneAreas
{
    private const string LevelOnePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("The Last Memory/Level 1/Connect Senses and Memory Areas")]
    private static void Connect()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Stop Play Mode before connecting the areas.", "OK");
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
        MemorySequencePuzzle memoryPuzzle = Object.FindFirstObjectByType<MemorySequencePuzzle>(FindObjectsInactive.Include);
        if (player == null || memoryPuzzle == null)
        {
            EditorUtility.DisplayDialog("Level 1 setup is incomplete", "The scene needs PlayerController and the inactive MemorySequencePuzzle. Run the Level 1 setup command first.", "OK");
            return;
        }

        Transform floorTransform = null;
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == "Memory Trial Floor")
            {
                floorTransform = candidate;
                break;
            }
        }
        GameObject floor;
        if (floorTransform == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Undo.RegisterCreatedObjectUndo(floor, "Create connected Level 1 floor");
            floor.name = "Memory Trial Floor";
            floorTransform = floor.transform;
        }
        else
        {
            floor = floorTransform.gameObject;
            Undo.RecordObject(floorTransform, "Connect Level 1 floor");
            Undo.SetTransformParent(floorTransform, null, "Connect Level 1 floor");
        }

        Vector3 start = player.transform.position;
        Vector3 finish = memoryPuzzle.transform.position;
        Vector3 midpoint = (start + finish) * 0.5f;
        midpoint.y = 0f;
        floorTransform.position = midpoint;
        floorTransform.rotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
        floorTransform.localScale = new Vector3(2f, 1f, 5f);
        floor.SetActive(true);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = floor;
        EditorUtility.DisplayDialog("Level 1 areas connected", "A continuous floor now spans the player start and memory trial. The transition activates the destination area before teleporting the player.", "OK");
    }
}
