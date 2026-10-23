using System.Collections;
using UnityEngine;

namespace TheLastMemory
{
    public sealed class EchoToneMachine : Interactable
    {
        [SerializeField] private EchoSequencePuzzle puzzle;
        [SerializeField] private int machineIndex;
        [SerializeField] private Color machineColor = Color.cyan;

        private Renderer machineRenderer;
        private Material runtimeMaterial;

        public void Configure(EchoSequencePuzzle owner, int index, Color tint)
        {
            puzzle = owner;
            machineIndex = index;
            machineColor = tint;
        }

        private void Awake()
        {
            machineRenderer = GetComponentInChildren<Renderer>();
            if (machineRenderer != null)
            {
                runtimeMaterial = machineRenderer.material;
                runtimeMaterial.EnableKeyword("_EMISSION");
                SetLit(false);
            }
        }

        public override void Interact()
        {
            if (puzzle != null) puzzle.PressMachine(machineIndex);
        }

        public void ShowCue()
        {
            SetLit(true);
            StopAllCoroutines();
        }

        public void ClearCue() => SetLit(false);

        private void SetLit(bool lit)
        {
            if (runtimeMaterial == null) return;
            Color color = lit ? machineColor * 1.6f : machineColor * 0.45f;
            runtimeMaterial.color = color;
            runtimeMaterial.SetColor("_EmissionColor", lit ? machineColor * 2f : machineColor * 0.08f);
        }
    }
}
