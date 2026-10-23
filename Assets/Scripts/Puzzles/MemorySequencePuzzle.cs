using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastMemory
{
    public sealed class MemorySequencePuzzle : MonoBehaviour
    {
        [SerializeField] private MemorySymbolButton[] buttons;
        [SerializeField, Min(15f)] private float timeLimit = 90f;
        [SerializeField, Min(1)] private int maxMistakes = 3;
        [SerializeField] private bool finishesLevelOne;

        private readonly int[] roundLengths = { 2, 3, 4 };
        private readonly string[] symbolNames = { "CYAN CUBE", "GREEN SPHERE", "VIOLET CYLINDER", "PINK CAPSULE" };
        private int[] sequence;
        private int roundIndex;
        private int inputIndex;
        private int mistakes;
        private float timeRemaining;
        private bool acceptingInput;
        private bool complete;
        private bool failed;
        private GUIStyle hudTitleStyle;
        private GUIStyle hudBodyStyle;
        private string status = "Watch the symbols.";

        public void Configure(MemorySymbolButton[] configuredButtons) => buttons = configuredButtons;

        public void ConfigureAsLevelOneFinale(bool isFinale) => finishesLevelOne = isFinale;

        private void Start()
        {
            timeRemaining = timeLimit;
            BeginRound();
        }

        private void Update()
        {
            if (!complete && !failed)
            {
                timeRemaining -= Time.deltaTime;
                if (timeRemaining <= 0f)
                {
                    timeRemaining = 0f;
                    Fail("Time ran out. Press R to retry.");
                }
            }
            if (failed && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                Retry();
        }

        private void BeginRound()
        {
            inputIndex = 0;
            sequence = new int[roundLengths[roundIndex]];
            for (int i = 0; i < sequence.Length; i++) sequence[i] = Random.Range(0, buttons.Length);
            StartCoroutine(ShowSequence());
        }

        private IEnumerator ShowSequence()
        {
            acceptingInput = false;
            status = $"Round {roundIndex + 1} of 3 — watch the symbols...";
            yield return new WaitForSeconds(0.7f);
            foreach (int index in sequence)
            {
                buttons[index].ShowCue();
                yield return new WaitForSeconds(0.55f);
                buttons[index].ClearCue();
                yield return new WaitForSeconds(0.25f);
            }
            status = $"Round {roundIndex + 1}: repeat the sequence. Aim and press E.";
            acceptingInput = true;
        }

        public void SelectSymbol(int symbolIndex)
        {
            if (!acceptingInput || complete || failed) return;
            if (symbolIndex == sequence[inputIndex])
            {
                buttons[symbolIndex].ShowCue();
                StartCoroutine(ClearButton(symbolIndex));
                inputIndex++;
                if (inputIndex >= sequence.Length)
                {
                    roundIndex++;
                    if (roundIndex >= roundLengths.Length)
                    {
                        complete = true;
                        acceptingInput = false;
                        status = finishesLevelOne
                            ? "LEVEL 1 COMPLETE — Hearing and Memory recovered!"
                            : "MEMORY MODULE RECOVERED. Puzzle complete!";
                        PlayerPrefs.SetInt("MemoryModule", 1);
                        if (finishesLevelOne) PlayerPrefs.SetInt("Level1Complete", 1);
                        Debug.Log("Memory Module recovered. Sequence puzzle complete.", this);
                    }
                    else
                    {
                        status = $"Round complete! Round {roundIndex + 1} is longer.";
                        StartCoroutine(NextRound());
                    }
                }
            }
            else
            {
                mistakes++;
                if (mistakes >= maxMistakes)
                {
                    Fail("Simulation detected you. Press R to retry.");
                    return;
                }
                inputIndex = 0;
                status = $"Wrong symbol. Trace {mistakes}/{maxMistakes}. Watch this round again...";
                StopAllCoroutines();
                StartCoroutine(ShowSequence());
            }
        }

        private IEnumerator NextRound()
        {
            acceptingInput = false;
            yield return new WaitForSeconds(1f);
            BeginRound();
        }

        private IEnumerator ClearButton(int index)
        {
            yield return new WaitForSeconds(0.2f);
            if (buttons != null && index < buttons.Length) buttons[index].ClearCue();
        }

        private void Fail(string reason)
        {
            StopAllCoroutines();
            failed = true;
            acceptingInput = false;
            status = reason;
        }

        private void Retry()
        {
            StopAllCoroutines();
            complete = false;
            failed = false;
            mistakes = 0;
            roundIndex = 0;
            timeRemaining = timeLimit;
            BeginRound();
        }

        private void OnGUI()
        {
            EnsureHudStyles();
            float width = Mathf.Min(600f, Mathf.Max(320f, Screen.width - 36f));
            string title = finishesLevelOne
                ? "LEVEL 1 — MEMORY TRIAL  |  FORGOTTEN SEQUENCE"
                : "MEMORY MODULE  |  FORGOTTEN SEQUENCE";
            GUI.Box(new Rect(18f, 18f, width, 190f), title, hudTitleStyle);
            GUI.Label(new Rect(32f, 50f, width - 28f, 42f),
                $"HOW TO PLAY: Click Game view. Watch the symbols, then aim and press E in order. " +
                $"Complete {roundLengths.Length} rounds. {maxMistakes} mistakes or {Mathf.CeilToInt(timeLimit)}s ends the trial; R retries.", hudBodyStyle);
            GUI.Label(new Rect(32f, 96f, width - 28f, 22f), status, hudBodyStyle);
            GUI.Label(new Rect(32f, 122f, width - 28f, 22f),
                $"TIME {Mathf.CeilToInt(timeRemaining)}s   |   ROUND {Mathf.Min(roundIndex + 1, roundLengths.Length)}/{roundLengths.Length}   |   " +
                $"MISTAKES {mistakes}/{maxMistakes} ({Mathf.Max(0, maxMistakes - mistakes)} left)", hudBodyStyle);
            GUI.Label(new Rect(32f, 148f, width - 28f, 36f),
                "Symbols: Cyan Cube • Green Sphere • Violet Cylinder • Pink Capsule", hudBodyStyle);
        }

        private void EnsureHudStyles()
        {
            if (hudTitleStyle != null) return;
            hudTitleStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(14, 8, 0, 0)
            };
            hudBodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
        }
    }
}
