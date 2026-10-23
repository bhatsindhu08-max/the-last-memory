using UnityEngine;

namespace TheLastMemory
{
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] private string prompt = "Interact";
        public string Prompt => prompt;
        public abstract void Interact();
    }
}
