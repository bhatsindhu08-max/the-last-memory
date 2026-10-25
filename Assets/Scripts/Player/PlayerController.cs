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
