using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastMemory
{
    public sealed class InteractionController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0.5f)] private float interactionDistance = 8f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private Interactable currentTarget;
        private Transform playerVisual;
        public Interactable CurrentTarget => currentTarget;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            // The visible Capsule is a separate root object in this scene. Its collider
            // sits between the over-the-shoulder camera and the puzzle targets.
            GameObject visual = GameObject.Find("Capsule");
            if (visual != null && visual.transform != transform) playerVisual = visual.transform;
        }

        private void Update()
        {
            currentTarget = null;
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, interactionLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                // Ignore the player body's own colliders so they do not block the interaction ray.
                if (IsPlayerCollider(hit.transform)) continue;

                currentTarget = hit.collider.GetComponentInParent<Interactable>();
                break; // A wall or other solid object blocks interaction with anything behind it.
            }

            if (currentTarget != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                currentTarget.Interact();
        }

        private bool IsPlayerCollider(Transform hitTransform)
        {
            if (hitTransform == transform || hitTransform.IsChildOf(transform)) return true;
            return playerVisual != null &&
                   (hitTransform == playerVisual || hitTransform.IsChildOf(playerVisual));
        }
    }
}
