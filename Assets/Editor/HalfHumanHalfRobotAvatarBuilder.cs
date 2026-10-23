using System.IO;
using TheLastMemory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HalfHumanHalfRobotAvatarBuilder
{
    private const string MaterialFolder = "Assets/Art/Materials";

    [MenuItem("The Last Memory/Player/Restore Capsule Player")]
    private static void BuildMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.playModeStateChanged -= ContinueAfterPlayMode;
            EditorApplication.playModeStateChanged += ContinueAfterPlayMode;
            EditorApplication.isPlaying = false;
            return;
        }

        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Player not found", "Open SampleScene and make sure the PlayerController object exists, then run this command again.", "OK");
            return;
        }

        BuildFor(player);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Capsule player restored", "The split character has been removed and the plain capsule visual restored. Player movement is unchanged. Press Play to see it.", "OK");
    }

    private static void ContinueAfterPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= ContinueAfterPlayMode;
        EditorApplication.delayCall += BuildMenu;
    }

    public static void BuildFor(PlayerController player)
    {
        if (player == null) return;

        // Remove the custom split-character visual and any detached capsule left from the old setup.
        Transform old = FindSceneObject("HalfHumanHalfRobot");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        old = FindSceneObject("Capsule");
        if (old != null && old != player.transform) Undo.DestroyObjectImmediate(old.gameObject);

        // Keep the CharacterController on the player object; this capsule is visual only.
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(capsule, "Restore capsule player visual");
        capsule.name = "Capsule";
        capsule.transform.SetParent(player.transform, false);
        capsule.transform.localPosition = Vector3.zero;
        capsule.transform.localRotation = Quaternion.identity;
        capsule.transform.localScale = Vector3.one;
        Collider visualCollider = capsule.GetComponent<Collider>();
        if (visualCollider != null) Object.DestroyImmediate(visualCollider);

        SerializedObject serializedPlayer = new SerializedObject(player);
        serializedPlayer.FindProperty("playerVisual").objectReferenceValue = capsule.transform;
        serializedPlayer.ApplyModifiedProperties();
        EditorUtility.SetDirty(player);
    }

    private static Transform FindSceneObject(string objectName)
    {
        foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            if (candidate.scene.IsValid() && candidate.name == objectName) return candidate.transform;
        return null;
    }

    private static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        Undo.RegisterCreatedObjectUndo(part, "Create " + name);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return part;
    }

    private static Material MakeMaterial(string name, Color baseColor, float metallic, Color? emission = null)
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
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission.Value * 2f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}

[InitializeOnLoad]
internal static class RestoreCapsuleVisualOnce
{
    private const string RestoreKey = "TheLastMemory.CapsuleVisualRestore.20261023";

    static RestoreCapsuleVisualOnce()
    {
        EditorApplication.delayCall += RestoreIfNeeded;
    }

    private static void RestoreIfNeeded()
    {
        if (EditorPrefs.GetBool(RestoreKey, false)) return;
        if (EditorApplication.isPlaying)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            return;
        }
        if (EditorApplication.isCompiling) return;
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity") return;

        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        GameObject avatar = GameObject.Find("HalfHumanHalfRobot");
        if (player == null || avatar == null) return;

        HalfHumanHalfRobotAvatarBuilder.BuildFor(player);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorPrefs.SetBool(RestoreKey, true);
        Debug.Log("Restored the plain capsule player visual in SampleScene.");
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.delayCall += RestoreIfNeeded;
    }
}
