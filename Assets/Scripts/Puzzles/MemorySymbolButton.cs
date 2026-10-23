using UnityEngine;

namespace TheLastMemory
{
    public sealed class MemorySymbolButton : Interactable
    {
        [SerializeField] private MemorySequencePuzzle puzzle;
        [SerializeField] private int symbolIndex;
        [SerializeField] private Color symbolColor = Color.cyan;

        private Renderer symbolRenderer;
        private Material runtimeMaterial;

        public void Configure(MemorySequencePuzzle owner, int index, Color tint)
        {
            puzzle = owner;
            symbolIndex = index;
            symbolColor = tint;
        }

        private void Awake()
        {
            symbolRenderer = GetComponentInChildren<Renderer>();
            if (symbolRenderer != null)
            {
                runtimeMaterial = symbolRenderer.material;
                runtimeMaterial.EnableKeyword("_EMISSION");
                SetLit(false);
            }
        }

        public override void Interact()
        {
            if (puzzle != null) puzzle.SelectSymbol(symbolIndex);
        }

        public void ShowCue() => SetLit(true);
        public void ClearCue() => SetLit(false);

        private void SetLit(bool lit)
        {
            if (runtimeMaterial == null) return;
            runtimeMaterial.color = lit ? symbolColor * 1.6f : symbolColor * 0.45f;
            runtimeMaterial.SetColor("_EmissionColor", lit ? symbolColor * 2f : symbolColor * 0.08f);
        }
    }
}
