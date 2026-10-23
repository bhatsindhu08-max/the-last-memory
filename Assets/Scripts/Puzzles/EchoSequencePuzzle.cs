using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TheLastMemory
{
    public sealed class EchoSequencePuzzle : MonoBehaviour
    {
        [SerializeField] private EchoToneMachine[] machines;
        [SerializeField, Min(15f)] private float timeLimit = 50f;
        [SerializeField, Min(1)] private int maxMistakes = 2;
        [SerializeField] private MemorySequencePuzzle nextPuzzle;
        [SerializeField] private Transform nextStageSpawn;
        [SerializeField] private string nextSceneName;

        private readonly int[] sequence = { 0, 2, 1, 2 };
        private readonly float[] frequencies = { 440f, 554f, 659f };
        private AudioSource audioSource;
        private AudioClip[] tones;
        private float timeRemaining;
        private int inputIndex;
        private int mistakes;
        private bool acceptingInput;
        private bool complete;
        private bool failed;
        private bool showHud = true;
        private GUIStyle hudTitleStyle;
        private GUIStyle hudBodyStyle;
        private string status = "Listen carefully to the tones.";

        public void Configure(EchoToneMachine[] configuredMachines) => machines = configuredMachines;

        public void ConfigureNextStage(MemorySequencePuzzle puzzle, Transform spawn)
        {
            nextPuzzle = puzzle;
            nextStageSpawn = spawn;
        }

        private void Start()
        {
            timeRemaining = timeLimit;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            tones = new AudioClip[frequencies.Length];
            for (int i = 0; i < frequencies.Length; i++) tones[i] = CreateTone(frequencies[i]);
            StartCoroutine(PresentSequence());
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

        private IEnumerator PresentSequence()
        {
            acceptingInput = false;
            inputIndex = 0;
            status = "Listen to the tone sequence...";
            yield return new WaitForSeconds(0.8f);
            foreach (int machineIndex in sequence)
            {
                if (machines == null || machineIndex >= machines.Length) yield break;
                machines[machineIndex].ShowCue();
                audioSource.PlayOneShot(tones[machineIndex]);
                yield return new WaitForSeconds(0.55f);
                machines[machineIndex].ClearCue();
                yield return new WaitForSeconds(0.25f);
            }
            status = "Repeat the tones: aim at each machine and press E.";
            acceptingInput = true;
        }

        public void PressMachine(int machineIndex)
        {
            if (!acceptingInput || complete || failed) return;
            if (machineIndex == sequence[inputIndex])
            {
                machines[machineIndex].ShowCue();
                audioSource.PlayOneShot(tones[machineIndex]);
                StartCoroutine(ClearMachineCue(machineIndex));
                inputIndex++;
                if (inputIndex >= sequence.Length)
                {
                    complete = true;
                    acceptingInput = false;
                    status = !string.IsNullOrWhiteSpace(nextSceneName)
                        ? "HEARING RESTORED — loading the next memory..."
                        : nextPuzzle != null
                            ? "HEARING RECOVERED — the signal was heard and shown with light. Memory awaits."
                            : "LEVEL 1 COMPLETE — hearing recovered!";
                    PlayerPrefs.SetInt("SenseModule", 1);
                    Debug.Log("Sense Module recovered. Echo puzzle complete.", this);
                    if (!string.IsNullOrWhiteSpace(nextSceneName)) StartCoroutine(TransitionToNextScene());
                    else if (nextPuzzle != null) StartCoroutine(TransitionToNextPuzzle());
                }
            }
            else
            {
                mistakes++;
                status = $"Wrong tone. Trace {mistakes}/{maxMistakes}. Sequence replaying...";
                if (mistakes >= maxMistakes)
                {
                    Fail("Simulation detected you. Press R to retry.");
                    return;
                }
                StartCoroutine(PresentSequence());
            }
        }

        private IEnumerator ClearMachineCue(int index)
        {
            yield return new WaitForSeconds(0.2f);
            if (machines != null && index < machines.Length) machines[index].ClearCue();
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
            timeRemaining = timeLimit;
            status = "Retrying the echo sequence...";
            StartCoroutine(PresentSequence());
        }


        private IEnumerator TransitionToNextScene()
        {
            yield return new WaitForSeconds(2.5f);
            SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }

        private IEnumerator TransitionToNextPuzzle()
        {
            yield return new WaitForSeconds(3f);
            // Turn on the destination floor before moving the player there.
            nextPuzzle.transform.root.gameObject.SetActive(true);
            yield return new WaitForFixedUpdate();
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null && nextStageSpawn != null)
            {
                player.TeleportTo(nextStageSpawn);
            }
            showHud = false;
        }

        private AudioClip CreateTone(float frequency)
        {
            const int sampleRate = 44100;
            const int sampleCount = 11025;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float envelope = Mathf.Min(i / 220f, (sampleCount - i) / 220f, 1f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * 0.25f;
            }
            AudioClip clip = AudioClip.Create("EchoTone_" + frequency, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnGUI()
        {
            if (!showHud) return;
            EnsureHudStyles();

            float width = Mathf.Min(540f, Mathf.Max(300f, Screen.width - 36f));
            GUI.Box(new Rect(18f, 18f, width, 148f),
                "LEVEL 1 — WHO AM I?  |  ECHO TRIAL", hudTitleStyle);
            GUI.Label(new Rect(32f, 50f, width - 28f, 42f),
                $"HOW TO PLAY: Click Game view. Listen to four tones, then aim at the machines and press E in order. " +
                $"{maxMistakes} mistakes or {Mathf.CeilToInt(timeLimit)}s ends the trial. R retries.", hudBodyStyle);
            GUI.Label(new Rect(32f, 96f, width - 28f, 22f), status, hudBodyStyle);
            GUI.Label(new Rect(32f, 122f, width - 28f, 22f),
                $"TIME {Mathf.CeilToInt(timeRemaining)}s   |   SEQUENCE {inputIndex}/{sequence.Length}   |   " +
                $"MISTAKES {mistakes}/{maxMistakes} ({Mathf.Max(0, maxMistakes - mistakes)} left)", hudBodyStyle);
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
