using UnityEngine;

namespace TheLastMemory
{
    public sealed class MessageInteractable : Interactable
    {
        [SerializeField, TextArea] private string message = "Interaction successful.";

        public override void Interact()
        {
            Debug.Log(message, this);
        }
    }
}
