using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TheLastMemory
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Scene References (optional)")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform playerVisual;
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField, Min(0f)] private float lookSensitivity = 0.12f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 60f;
        [SerializeField, Min(0f)] private float gravity = 20f;
        [Header("Camera")]
        [SerializeField, Min(0f)] private float cameraHeight = 1.6f;
        [SerializeField, Min(0f)] private float cameraDistance = 4f;

        private CharacterController characterController;
        private Camera sceneCamera;
        private float pitch = 9f;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            sceneCamera = Camera.main;
            if (cameraPivot == null && sceneCamera != null) cameraPivot = sceneCamera.transform;
            if (playerVisual == null)
            {
                GameObject visual = GameObject.Find("HalfHumanHalfRobot");
                if (visual == null) visual = GameObject.Find("Capsule");
                if (visual != null && visual.transform != transform) playerVisual = visual.transform;
            }

            if (playerVisual != null) BuildCyberneticAvatar(playerVisual);
            StartCoroutine(KeepOnlyCurrentLevelPlayer());
        }

        private System.Collections.IEnumerator KeepOnlyCurrentLevelPlayer()
        {
            // Wait until every loaded scene has run Awake so additive leftovers are visible here.
            yield return null;
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            if (players.Length < 2) yield break;

            Scene activeScene = SceneManager.GetActiveScene();
            PlayerController winner = null;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].gameObject.scene == activeScene)
                {
                    winner = players[i];
                    break;
                }
            }
            if (winner == null) winner = players[0];

            for (int i = 0; i < players.Length; i++)
            {
                PlayerController duplicate = players[i];
                if (duplicate == winner) continue;

                Scene duplicateScene = duplicate.gameObject.scene;
                duplicate.enabled = false;
                CharacterController duplicateCollider = duplicate.GetComponent<CharacterController>();
                if (duplicateCollider != null) duplicateCollider.enabled = false;
                if (duplicate.playerVisual != null) duplicate.playerVisual.gameObject.SetActive(false);

                if (duplicateScene.IsValid() && duplicateScene.isLoaded && duplicateScene != winner.gameObject.scene && SceneManager.sceneCount > 1)
                {
                    GameObject[] roots = duplicateScene.GetRootGameObjects();
                    for (int r = 0; r < roots.Length; r++)
                    {
                        Camera[] cameras = roots[r].GetComponentsInChildren<Camera>(true);
                        for (int c = 0; c < cameras.Length; c++) cameras[c].enabled = false;
                        AudioListener[] listeners = roots[r].GetComponentsInChildren<AudioListener>(true);
                        for (int a = 0; a < listeners.Length; a++) listeners[a].enabled = false;
                    }
                    SceneManager.UnloadSceneAsync(duplicateScene);
                }
                else
                {
                    duplicate.gameObject.SetActive(false);
                }
            }
        }

        private readonly System.Collections.Generic.List<Material> runtimeAvatarMaterials = new System.Collections.Generic.List<Material>();

        private void BuildCyberneticAvatar(Transform visualRoot)
        {
            if (visualRoot.Find("Cybernetic Avatar") != null) return;

            Renderer oldRenderer = visualRoot.GetComponent<Renderer>();
            if (oldRenderer != null) oldRenderer.enabled = false;
            Collider oldCollider = visualRoot.GetComponent<Collider>();
            if (oldCollider != null) oldCollider.enabled = false;

            Transform avatar = new GameObject("Cybernetic Avatar").transform;
            avatar.SetParent(visualRoot, false);

            Material suit = AvatarMaterial(new Color32(74, 80, 85, 255), 0.55f, 0.55f);
            Material armor = AvatarMaterial(new Color32(112, 118, 123, 255), 0.72f, 0.7f);
            Material trim = AvatarMaterial(new Color32(157, 162, 166, 255), 0.65f, 0.72f);
            Material cyan = AvatarMaterial(new Color32(38, 220, 238, 255), 0.45f, 0.8f);
            Material skin = AvatarMaterial(new Color32(145, 149, 152, 255), 0.05f, 0.35f);
            Material hair = AvatarMaterial(new Color32(48, 53, 57, 255), 0.15f, 0.35f);
            Material black = AvatarMaterial(new Color32(37, 41, 44, 255), 0.25f, 0.45f);

            // Compact head, face and high armored collar.
            AvatarPart(avatar, PrimitiveType.Cylinder, "Armored neck seal", new Vector3(0f, 0.54f, 0f), new Vector3(0.13f, 0.11f, 0.13f), armor);
            AvatarPart(avatar, PrimitiveType.Sphere, "Head", new Vector3(0f, 0.74f, 0.015f), new Vector3(0.145f, 0.19f, 0.14f), skin);
            AvatarPart(avatar, PrimitiveType.Sphere, "Short dark hair", new Vector3(0f, 0.86f, -0.005f), new Vector3(0.148f, 0.085f, 0.143f), hair);
            AvatarPart(avatar, PrimitiveType.Cube, "Hairline", new Vector3(0f, 0.82f, 0.125f), new Vector3(0.22f, 0.045f, 0.025f), hair);
            AvatarPart(avatar, PrimitiveType.Sphere, "Left ear", new Vector3(-0.145f, 0.73f, 0f), new Vector3(0.035f, 0.055f, 0.045f), skin);
            AvatarPart(avatar, PrimitiveType.Sphere, "Right ear", new Vector3(0.145f, 0.73f, 0f), new Vector3(0.035f, 0.055f, 0.045f), skin);
            AvatarPart(avatar, PrimitiveType.Cube, "Left cyan eye", new Vector3(-0.052f, 0.755f, 0.14f), new Vector3(0.035f, 0.012f, 0.012f), cyan);
            AvatarPart(avatar, PrimitiveType.Cube, "Right cyan eye", new Vector3(0.052f, 0.755f, 0.14f), new Vector3(0.035f, 0.012f, 0.012f), cyan);
            AvatarPart(avatar, PrimitiveType.Cube, "Nose bridge", new Vector3(0f, 0.725f, 0.15f), new Vector3(0.025f, 0.055f, 0.025f), skin);
            AvatarPart(avatar, PrimitiveType.Cylinder, "Raised cyber collar", new Vector3(0f, 0.48f, 0f), new Vector3(0.19f, 0.09f, 0.19f), trim);
            AvatarPart(avatar, PrimitiveType.Cube, "Collar light", new Vector3(0f, 0.49f, 0.184f), new Vector3(0.08f, 0.018f, 0.012f), cyan);

            // Jacket silhouette, chest plates and a matching back-mounted power unit.
            AvatarPart(avatar, PrimitiveType.Capsule, "Armored jacket", new Vector3(0f, 0.17f, 0f), new Vector3(0.31f, 0.27f, 0.19f), suit);
            AvatarPart(avatar, PrimitiveType.Cube, "Chest plate left", new Vector3(-0.12f, 0.23f, 0.165f), new Vector3(0.16f, 0.19f, 0.065f), armor, new Vector3(0f, 0f, -8f));
            AvatarPart(avatar, PrimitiveType.Cube, "Chest plate right", new Vector3(0.12f, 0.23f, 0.165f), new Vector3(0.16f, 0.19f, 0.065f), armor, new Vector3(0f, 0f, 8f));
            AvatarPart(avatar, PrimitiveType.Cube, "Chest center seam", new Vector3(0f, 0.16f, 0.205f), new Vector3(0.025f, 0.22f, 0.018f), trim);
            AvatarPart(avatar, PrimitiveType.Cube, "Chest cyan indicator", new Vector3(0f, 0.30f, 0.208f), new Vector3(0.055f, 0.018f, 0.012f), cyan);
            AvatarPart(avatar, PrimitiveType.Cube, "Back power pack", new Vector3(0f, 0.19f, -0.185f), new Vector3(0.25f, 0.31f, 0.075f), armor);
            AvatarPart(avatar, PrimitiveType.Cube, "Back center plate", new Vector3(0f, 0.19f, -0.23f), new Vector3(0.12f, 0.24f, 0.025f), trim);
            AvatarPart(avatar, PrimitiveType.Cube, "Back left cyan vent", new Vector3(-0.085f, 0.20f, -0.235f), new Vector3(0.025f, 0.15f, 0.012f), cyan);
            AvatarPart(avatar, PrimitiveType.Cube, "Back right cyan vent", new Vector3(0.085f, 0.20f, -0.235f), new Vector3(0.025f, 0.15f, 0.012f), cyan);
            AvatarPart(avatar, PrimitiveType.Cube, "Belt", new Vector3(0f, -0.16f, 0f), new Vector3(0.34f, 0.075f, 0.21f), black);
            AvatarPart(avatar, PrimitiveType.Cube, "Belt buckle", new Vector3(0f, -0.16f, 0.115f), new Vector3(0.09f, 0.055f, 0.025f), trim);
            AvatarPart(avatar, PrimitiveType.Cube, "Buckle light", new Vector3(0f, -0.16f, 0.132f), new Vector3(0.04f, 0.012f, 0.01f), cyan);

            // Shoulder armor, articulated sleeves, forearm guards and gloves.
            AvatarPart(avatar, PrimitiveType.Sphere, "Left shoulder guard", new Vector3(-0.34f, 0.33f, 0f), new Vector3(0.19f, 0.16f, 0.21f), armor);
            AvatarPart(avatar, PrimitiveType.Sphere, "Right shoulder guard", new Vector3(0.34f, 0.33f, 0f), new Vector3(0.19f, 0.16f, 0.21f), armor);
            AvatarPart(avatar, PrimitiveType.Capsule, "Left upper sleeve", new Vector3(-0.41f, 0.11f, 0f), new Vector3(0.115f, 0.22f, 0.12f), suit, new Vector3(0f, 0f, -8f));
            AvatarPart(avatar, PrimitiveType.Capsule, "Right upper sleeve", new Vector3(0.41f, 0.11f, 0f), new Vector3(0.115f, 0.22f, 0.12f), suit, new Vector3(0f, 0f, 8f));
            AvatarPart(avatar, PrimitiveType.Capsule, "Left cyber forearm", new Vector3(-0.46f, -0.19f, 0.01f), new Vector3(0.12f, 0.20f, 0.13f), armor, new Vector3(0f, 0f, -5f));
            AvatarPart(avatar, PrimitiveType.Capsule, "Right cyber forearm", new Vector3(0.46f, -0.19f, 0.01f), new Vector3(0.12f, 0.20f, 0.13f), armor, new Vector3(0f, 0f, 5f));
            AvatarPart(avatar, PrimitiveType.Cube, "Left forearm light", new Vector3(-0.46f, -0.20f, 0.14f), new Vector3(0.035f, 0.11f, 0.015f), cyan);
            AvatarPart(avatar, PrimitiveType.Cube, "Right forearm light", new Vector3(0.46f, -0.20f, 0.14f), new Vector3(0.035f, 0.11f, 0.015f), cyan);
            AvatarPart(avatar, PrimitiveType.Sphere, "Left glove", new Vector3(-0.47f, -0.43f, 0.015f), new Vector3(0.105f, 0.10f, 0.11f), black);
            AvatarPart(avatar, PrimitiveType.Sphere, "Right glove", new Vector3(0.47f, -0.43f, 0.015f), new Vector3(0.105f, 0.10f, 0.11f), black);

            // Reinforced trousers, knee plates, shin guards and boots.
            AvatarPart(avatar, PrimitiveType.Capsule, "Left trouser leg", new Vector3(-0.16f, -0.39f, 0f), new Vector3(0.14f, 0.22f, 0.15f), suit);
            AvatarPart(avatar, PrimitiveType.Capsule, "Right trouser leg", new Vector3(0.16f, -0.39f, 0f), new Vector3(0.14f, 0.22f, 0.15f), suit);
            AvatarPart(avatar, PrimitiveType.Sphere, "Left knee armor", new Vector3(-0.16f, -0.57f, 0.11f), new Vector3(0.13f, 0.12f, 0.06f), armor);
            AvatarPart(avatar, PrimitiveType.Sphere, "Right knee armor", new Vector3(0.16f, -0.57f, 0.11f), new Vector3(0.13f, 0.12f, 0.06f), armor);
            AvatarPart(avatar, PrimitiveType.Capsule, "Left shin guard", new Vector3(-0.16f, -0.76f, 0f), new Vector3(0.12f, 0.20f, 0.135f), armor);
            AvatarPart(avatar, PrimitiveType.Capsule, "Right shin guard", new Vector3(0.16f, -0.76f, 0f), new Vector3(0.12f, 0.20f, 0.135f), armor);
            AvatarPart(avatar, PrimitiveType.Cube, "Left shin light", new Vector3(-0.16f, -0.75f, 0.13f), new Vector3(0.025f, 0.12f, 0.015f), cyan);
            AvatarPart(avatar, PrimitiveType.Cube, "Right shin light", new Vector3(0.16f, -0.75f, 0.13f), new Vector3(0.025f, 0.12f, 0.015f), cyan);
            AvatarPart(avatar, PrimitiveType.Cube, "Left boot", new Vector3(-0.16f, -0.91f, 0.055f), new Vector3(0.15f, 0.16f, 0.25f), black);
            AvatarPart(avatar, PrimitiveType.Cube, "Right boot", new Vector3(0.16f, -0.91f, 0.055f), new Vector3(0.15f, 0.16f, 0.25f), black);
            AvatarPart(avatar, PrimitiveType.Cube, "Left boot toe plate", new Vector3(-0.16f, -0.92f, 0.16f), new Vector3(0.13f, 0.035f, 0.035f), trim);
            AvatarPart(avatar, PrimitiveType.Cube, "Right boot toe plate", new Vector3(0.16f, -0.92f, 0.16f), new Vector3(0.13f, 0.035f, 0.035f), trim);
        }

        private Material AvatarMaterial(Color color, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = color };
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            runtimeAvatarMaterials.Add(material);
            return material;
        }

        private static void AvatarPart(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Material material, Vector3 localRotation = default)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localRotation);
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeAvatarMaterials.Count; i++)
                if (runtimeAvatarMaterials[i] != null) Destroy(runtimeAvatarMaterials[i]);
            runtimeAvatarMaterials.Clear();
        }

        public void TeleportTo(Transform destination)
        {
            if (destination == null) return;
            bool controllerWasEnabled = characterController != null && characterController.enabled;
            if (controllerWasEnabled) characterController.enabled = false;
            transform.SetPositionAndRotation(destination.position, destination.rotation);
            verticalVelocity = 0f;
            if (controllerWasEnabled) characterController.enabled = true;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 move = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            }
            move = Vector2.ClampMagnitude(move, 1f);

            if (characterController.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity -= gravity * Time.deltaTime;
            Vector3 direction = transform.right * move.x + transform.forward * move.y;
            characterController.Move((direction * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 look = mouse.delta.ReadValue() * lookSensitivity;
                transform.Rotate(Vector3.up, look.x, Space.World);
                pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            }

            if (playerVisual != null)
            {
                playerVisual.position = transform.position;
                playerVisual.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }

            if (cameraPivot != null)
            {
                cameraPivot.position = transform.position + Vector3.up * cameraHeight - transform.forward * cameraDistance;
                cameraPivot.rotation = Quaternion.Euler(pitch, transform.eulerAngles.y, 0f);
            }
        }
    }
}
