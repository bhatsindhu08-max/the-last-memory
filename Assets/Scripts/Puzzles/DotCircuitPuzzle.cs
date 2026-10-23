using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheLastMemory
{
    public sealed class DotCircuitPuzzle : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] circuitNodes;
        [SerializeField] private Transform[] decoyNodes;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Text timerLabel;
        [SerializeField, Min(30f)] private float timeLimit = 60f;
        [SerializeField, Min(1)] private int maxMistakes = 1;
        [SerializeField, Min(0.5f)] private float activationRadius = 2.25f;
        [SerializeField, Min(1f)] private float interactionRadius = 5.5f;
        [SerializeField, Min(1f)] private float questionRevealRadius = 1.8f;
        [SerializeField] private string nextSceneName = "Level3_NeonCourier";
        [SerializeField] private AudioSource ambience;

        private readonly HashSet<int> decoysTouched = new HashSet<int>();
        private readonly string[] questions =
        {
            "How many bones does an average adult human have?",
            "What is the brain of a computer called?",
            "Which organ pumps blood throughout the body?",
            "How many lungs does a typical human have?",
            "Which device displays images and text from a computer?",
            "Which organ controls most body activities and processes information?",
            "Which blood cells help fight infections?",
            "What is the process called when the body converts food into usable energy?"
        };
        private readonly string[,] answers =
        {
            { "A) 206", "B) 208", "C) 210", "D) 201" },
            { "A) RAM", "B) CPU", "C) Mouse", "D) SSD" },
            { "A) Brain", "B) Lungs", "C) Heart", "D) Liver" },
            { "A) One", "B) Two", "C) Three", "D) Four" },
            { "A) Keyboard", "B) Printer", "C) Monitor", "D) Router" },
            { "A) Heart", "B) Brain", "C) Kidney", "D) Stomach" },
            { "A) Platelets", "B) Neurons", "C) Red cells", "D) White cells" },
            { "A) Digestion", "B) Respiration", "C) Metabolism", "D) Circulation" }
        };
        private readonly int[] correctAnswers = { 0, 1, 2, 1, 2, 1, 3, 2 };
        private float timeRemaining;
        private int nextNode;
        private int mistakes;
        private bool complete;
        private bool failed;
        private string status = "Reach checkpoint 01 to reveal the first riddle.";
        private GameObject endOverlay;
        private Text endOverlayText;
        private readonly UnityEngine.UI.Button[] answerButtons = new UnityEngine.UI.Button[4];
        private readonly Text[] answerButtonLabels = new Text[4];
        private float answerFeedbackTime;
        private const int BridgeRows = 8;
        private const float FirstRowZ = 4f;
        private const float RowSpacing = 2.82f;
        private const float TileDepth = 2.7f;
        private const float LaneCenter = 1.52f;
        private readonly Transform[,] bridgePanes = new Transform[BridgeRows, 2];
        private readonly bool[] safeLanes = new bool[BridgeRows];
        private bool crossingUnlocked;
        private bool falling;

        private string Rules => "HOW TO PLAY: Solve the riddle at each row. The safe LEFT or RIGHT pane turns cyan; cross only that pane. There are 8 rows. A wrong answer, unsafe pane, or expired timer breaks the glass and you fall. Press R to retry.";

        public void Configure(Transform targetPlayer, Transform[] orderedNodes, Transform[] wrongNodes, Text status, Text progress, Text timer)
        {
            player = targetPlayer;
            circuitNodes = orderedNodes;
            decoyNodes = wrongNodes;
            statusLabel = status;
            progressLabel = progress;
            timerLabel = timer;
        }

        private void Start()
        {
            timeLimit = 180f;
            timeRemaining = timeLimit;
            maxMistakes = 1;
            BuildGlassBridge();
            if (ambience == null)
            {
                GameObject soundObject = GameObject.Find("Level2Ambience");
                if (soundObject != null) ambience = soundObject.GetComponent<AudioSource>();
            }
            if (ambience != null && ambience.clip != null)
            {
                ambience.loop = true;
                ambience.playOnAwake = true;
                if (!ambience.isPlaying) ambience.Play();
            }
            ConfigureHudLayout();
            BuildAnswerButtons();
            UpdateCheckpointLabels();
            RefreshHud();
        }

        private void BuildGlassBridge()
        {
            GameObject world = GameObject.Find("Level2_DotCircuit_World");
            if (world == null) return;
            Transform root = world.transform.Find("Neon Glass Bridge");
            if (root != null) Destroy(root.gameObject);
            root = new GameObject("Neon Glass Bridge").transform;
            root.SetParent(world.transform, false);

            for (int i = 0; i < world.transform.childCount; i++)
            {
                Transform child = world.transform.GetChild(i);
                if (child == root) continue;
                if (child.name == "Cyber City Backdrop") continue;
                if (child.name == "Start Deck" || child.name == "Circuit Platform" || child.name == "Left Edge Light" || child.name == "Right Edge Light" || child.name.StartsWith("Circuit Node") || child.name.StartsWith("Broken Branch") || child.name.StartsWith("Node label") || child.name == "Circuit Receiver" || child.name == "Circuit Trace")
                    child.gameObject.SetActive(false);
            }

            Material deckMaterial = BridgeMaterial(new Color(.08f, .13f, .2f), new Color(.04f, .11f, .2f));
            Material safeMaterial = BridgeMaterial(new Color(.06f, .8f, 1f), new Color(.02f, .65f, 1f));
            Material unsafeMaterial = BridgeMaterial(new Color(.85f, .08f, .2f), new Color(1f, .015f, .04f));
            Material dormantMaterial = BridgeMaterial(new Color(.15f, .32f, .42f), new Color(.02f, .12f, .18f));

            BridgeCube("Start Deck", root, new Vector3(0f, -.08f, 0f), new Vector3(6.2f, .35f, 5f), deckMaterial);
            Transform goalDeck = BridgeCube("Exit Deck", root, new Vector3(0f, -.08f, FirstRowZ + (BridgeRows - 1) * RowSpacing + RowSpacing), new Vector3(6.2f, .35f, 5f), deckMaterial).transform;
            BridgeLabel(goalDeck, "EXIT");

            circuitNodes = new Transform[BridgeRows];
            decoyNodes = new Transform[0];
            for (int row = 0; row < BridgeRows; row++)
            {
                float z = FirstRowZ + row * RowSpacing;
                GameObject checkpoint = new GameObject("Bridge Riddle Checkpoint " + (row + 1).ToString("00"));
                checkpoint.transform.SetParent(root, false);
                checkpoint.transform.position = new Vector3(0f, 0f, z - 1.55f);
                circuitNodes[row] = checkpoint.transform;
                safeLanes[row] = Random.Range(0, 2) == 0;

                for (int lane = 0; lane < 2; lane++)
                {
                    GameObject pane = BridgeCube("Row " + (row + 1).ToString("00") + " — " + (lane == 0 ? "Left" : "Right") + " Glass", root,
                        new Vector3(lane == 0 ? -LaneCenter : LaneCenter, -.025f, z), new Vector3(2.9f, .16f, TileDepth), dormantMaterial);
                    bridgePanes[row, lane] = pane.transform;
                    pane.GetComponent<Renderer>().sharedMaterial = dormantMaterial;
                }
            }
            SetNodeColor(circuitNodes[0], new Color(.1f, .95f, 1f));
        }

        private static GameObject BridgeCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static Material BridgeMaterial(Color color, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = color };
            color.a = .62f;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission * 1.4f);
            }
            return material;
        }

        private static void BridgeLabel(Transform target, string text)
        {
            GameObject label = new GameObject("Bridge " + text + " Label", typeof(TextMesh));
            label.transform.SetParent(target, false);
            label.transform.localPosition = new Vector3(0f, .24f, 0f);
            label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh mesh = label.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = .45f;
            mesh.color = new Color(.25f, .95f, 1f);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && failed && !falling)
            {
                Retry();
                return;
            }
            if (complete || failed) return;
            ResolvePlayer();
            if (player == null) return;

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                Fail("TIME EXPIRED");
                RefreshHud();
                return;
            }

            answerFeedbackTime = Mathf.Max(0f, answerFeedbackTime - Time.deltaTime);
            CheckRiddleCheckpoint();
            RefreshHud();
        }

        private void CheckRiddleCheckpoint()
        {
            if (circuitNodes == null || nextNode >= BridgeRows || nextNode >= questions.Length) return;
            bool justEnteredNextQuestion = false;
            Transform checkpoint = circuitNodes[nextNode];
            if (checkpoint == null) return;

            if (crossingUnlocked)
            {
                SetPlayerMovementEnabled(true);
                SetAnswerButtonsVisible(false);
                int currentRow = nextNode;
                float rowZ = FirstRowZ + currentRow * RowSpacing;
                float z = player.position.z;
                bool withinRow = z >= rowZ - TileDepth * .5f && z <= rowZ + TileDepth * .5f;

                if (Mathf.Abs(player.position.x) > 3.3f && withinRow)
                {
                    Fail("YOU FELL BESIDE THE GLASS BRIDGE");
                    return;
                }

                if (withinRow)
                {
                    int lane = player.position.x < 0f ? 0 : 1;
                    if (safeLanes[nextNode] != (lane == 0))
                    {
                        BreakPane(nextNode, lane);
                        Fail("WRONG GLASS PANE — YOU FELL");
                        return;
                    }

                    // As soon as the player steps onto a safe pane, reveal the next row's riddle.
                    if (nextNode < BridgeRows - 1 && z >= rowZ - TileDepth * .5f)
                    {
                        nextNode++;
                        crossingUnlocked = false;
                        SetNodeColor(circuitNodes[nextNode], new Color(.1f, .95f, 1f));
                        checkpoint = circuitNodes[nextNode];
                        justEnteredNextQuestion = true;
                    }
                }

                // The final row must be crossed all the way to the exit deck.
                if (currentRow == BridgeRows - 1 && z > rowZ + TileDepth * .5f)
                {
                    nextNode++;
                    crossingUnlocked = false;
                    complete = true;
                    status = "ALL 8 ROWS CROSSED — MEMORY LINK RESTORED. Moving to Level 3.";
                    Debug.Log("Level 2 complete: all eight glass rows crossed.", this);
                    if (!string.IsNullOrEmpty(nextSceneName)) Invoke(nameof(LoadNextScene), 3f);
                    return;
                }

                if (!justEnteredNextQuestion)
                {
                    status = "ROW " + (nextNode + 1) + "/8 — enter the glowing CYAN pane. Red glass is unsafe.";
                    return;
                }
            }

            float distance = HorizontalDistance(player.position, checkpoint.position);
            if (distance > questionRevealRadius && !justEnteredNextQuestion)
            {
                SetPlayerMovementEnabled(true);
                status = "APPROACH GLASS ROW " + (nextNode + 1).ToString("00") + " TO READ ITS RIDDLE.";
                SetAnswerButtonsVisible(false);
                return;
            }

            SetPlayerMovementEnabled(false);
            SetAnswerButtonsVisible(answerFeedbackTime <= 0f);
            for (int i = 0; i < answerButtonLabels.Length; i++)
                if (answerButtonLabels[i] != null) answerButtonLabels[i].text = answers[nextNode, i];
            if (answerFeedbackTime <= 0f)
                status = "RIDDLE " + (nextNode + 1) + "/8\n" + questions[nextNode] + "\nAnswer correctly to reveal this row's safe glass.";
        }

        private void SubmitAnswer(int selected)
        {
            if (failed || complete || crossingUnlocked || nextNode >= BridgeRows) return;
            if (circuitNodes == null || nextNode >= circuitNodes.Length || circuitNodes[nextNode] == null) return;
            if (player == null || answerButtons[0] == null || !answerButtons[0].gameObject.activeSelf) return;
            if (selected != correctAnswers[nextNode])
            {
                mistakes++;
                BreakBridgeRow(nextNode);
                Fail("WRONG ANSWER — THE GLASS BROKE AND YOU FELL");
                return;
            }

            SetNodeColor(circuitNodes[nextNode], new Color(.12f, 1f, .83f));
            Transform safePane = bridgePanes[nextNode, safeLanes[nextNode] ? 0 : 1];
            Transform unsafePane = bridgePanes[nextNode, safeLanes[nextNode] ? 1 : 0];
            SetPaneColor(safePane, new Color(.02f, .85f, 1f));
            SetPaneColor(unsafePane, new Color(.85f, .035f, .12f));
            crossingUnlocked = true;
            SetAnswerButtonsVisible(false);
            SetPlayerMovementEnabled(true);
            string safeSide = safeLanes[nextNode] ? "LEFT" : "RIGHT";
            status = "CORRECT — ROW " + (nextNode + 1) + "/8 SAFE PANE: " + safeSide + ". Cross only the glowing cyan pane.";
            answerFeedbackTime = 0f;
        }

        private void SetPaneColor(Transform pane, Color color)
        {
            if (pane == null) return;
            Renderer renderer = pane.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
        }

        private void BreakPane(int row, int lane)
        {
            if (row < 0 || row >= BridgeRows || lane < 0 || lane > 1) return;
            Transform pane = bridgePanes[row, lane];
            if (pane == null) return;
            SetPaneColor(pane, new Color(1f, .015f, .02f));
            Collider paneCollider = pane.GetComponent<Collider>();
            if (paneCollider != null) paneCollider.enabled = false;
        }

        private void BreakBridgeRow(int row)
        {
            BreakPane(row, 0);
            BreakPane(row, 1);
        }

        private void BuildAnswerButtons()
        {
            if (statusLabel == null) return;
            RectTransform panel = statusLabel.transform.parent as RectTransform;
            if (panel == null) return;

            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                InputSystemUIInputModule module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }

            for (int i = 0; i < answerButtons.Length; i++)
            {
                GameObject buttonObject = new GameObject("Riddle Answer " + (i + 1), typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
                buttonObject.transform.SetParent(panel, false);
                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(i % 2 == 0 ? 26f : 474f, i < 2 ? -292f : -356f);
                rect.sizeDelta = new Vector2(420f, 52f);
                Image image = buttonObject.GetComponent<Image>();
                image.color = new Color(.025f, .18f, .25f, .98f);
                UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(.05f, .45f, .56f, 1f);
                colors.highlightedColor = new Color(.1f, .8f, .9f, 1f);
                colors.pressedColor = new Color(.08f, 1f, .85f, 1f);
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
                int choice = i;
                button.onClick.AddListener(() => SubmitAnswer(choice));

                GameObject labelObject = new GameObject("Answer Text", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8f, 2f);
                labelRect.offsetMax = new Vector2(-8f, -2f);
                Text label = labelObject.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 19;
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.white;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.raycastTarget = false;
                answerButtons[i] = button;
                answerButtonLabels[i] = label;
                buttonObject.SetActive(false);
            }
            if (statusLabel != null) statusLabel.raycastTarget = false;
            if (progressLabel != null) progressLabel.raycastTarget = false;
            if (timerLabel != null) timerLabel.raycastTarget = false;
        }

        private void SetAnswerButtonsVisible(bool visible)
        {
            foreach (UnityEngine.UI.Button button in answerButtons)
                if (button != null && button.gameObject.activeSelf != visible) button.gameObject.SetActive(visible);
        }

        private void SetPlayerMovementEnabled(bool enabled)
        {
            if (player == null) return;
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null && controller.enabled != enabled) controller.enabled = enabled;
        }

        private void UpdateCheckpointLabels()
        {
            if (circuitNodes == null) return;
            for (int i = 0; i < circuitNodes.Length && i < questions.Length; i++)
            {
                GameObject labelObject = GameObject.Find("Node label " + (i + 1).ToString("00"));
                TextMesh label = labelObject != null ? labelObject.GetComponent<TextMesh>() : null;
                if (label != null)
                {
                    label.text = (i + 1).ToString();
                    label.characterSize = 0.12f;
                    label.fontSize = 24;
                    label.color = new Color(.35f, .95f, 1f);
                }
                if (circuitNodes[i] != null) circuitNodes[i].name = "Riddle Checkpoint " + (i + 1).ToString("00");
            }
        }

        private static int ReadAnswerChoice()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return -1;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 0;
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 1;
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 2;
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return 3;
            return -1;
        }

        private void ResolvePlayer()
        {
            if (player != null && player.gameObject.activeInHierarchy) return;
            PlayerController controller = FindFirstObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
        }

        private void CheckNextCircuitNode(bool interactPressed)
        {
            if (circuitNodes == null || nextNode >= circuitNodes.Length) return;
            Transform node = circuitNodes[nextNode];
            if (node == null) return;
            float distance = HorizontalDistance(player.position, node.position);
            if (distance > activationRadius && !(interactPressed && distance <= interactionRadius))
            {
                if (distance <= interactionRadius)
                    status = "NODE " + (nextNode + 1) + " NEARBY — press E or move onto it.";
                return;
            }

            SetNodeColor(node, new Color(.12f, 1, .83f));
            nextNode++;
            if (nextNode >= circuitNodes.Length)
            {
                complete = true;
                status = "CIRCUIT RESTORED — memory link stable";
                Debug.Log("Level 2 complete: dot circuit connected.", this);
                if (!string.IsNullOrEmpty(nextSceneName)) Invoke(nameof(LoadNextScene), 2.5f);
            }
            else
            {
                status = "Node connected. Follow the bright circuit to the next dot.";
                SetNodeColor(circuitNodes[nextNode], new Color(.1f, .95f, 1));
            }
        }

        private void CheckDecoys()
        {
            if (decoyNodes == null) return;
            for (int i = 0; i < decoyNodes.Length; i++)
            {
                Transform decoy = decoyNodes[i];
                if (decoy == null) continue;
                bool inside = HorizontalDistance(player.position, decoy.position) <= activationRadius;
                if (inside && decoysTouched.Add(i))
                {
                    mistakes++;
                    SetNodeColor(decoy, new Color(1, .18f, .3f));
                    Fail("WRONG NODE TOUCHED — circuit locked");
                    return;
                }
                else if (!inside)
                {
                    decoysTouched.Remove(i);
                }
            }
        }

        private void LoadNextScene()
        {
            if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }

        private void Fail(string reason)
        {
            if (failed) return;
            failed = true;
            if ((reason == "TIME EXPIRED" || reason.StartsWith("WRONG ANSWER")) && nextNode < BridgeRows)
                BreakBridgeRow(nextNode);
            status = "GAME OVER — " + reason + ". Press R to retry.";
            SetAnswerButtonsVisible(false);
            SetPlayerMovementEnabled(false);
            if (!falling) StartCoroutine(FallSequence(reason));
        }

        private System.Collections.IEnumerator FallSequence(string reason)
        {
            falling = true;
            CharacterController controller = player != null ? player.GetComponent<CharacterController>() : null;
            if (controller != null) controller.enabled = false;
            Vector3 start = player != null ? player.position : Vector3.zero;
            float elapsed = 0f;
            while (player != null && elapsed < 1.1f)
            {
                elapsed += Time.unscaledDeltaTime;
                player.position = start + Vector3.down * (5f * Mathf.Clamp01(elapsed / 1.1f));
                yield return null;
            }
            ShowEndOverlay("GAME OVER", reason);
            falling = false;
        }

        private void Retry()
        {
            ResolvePlayer();
            if (player != null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                player.SetPositionAndRotation(Vector3.up, Quaternion.identity);
                if (controller != null) controller.enabled = true;
            }
            for (int row = 0; row < BridgeRows; row++)
            {
                safeLanes[row] = Random.Range(0, 2) == 0;
                for (int lane = 0; lane < 2; lane++)
                {
                    Transform pane = bridgePanes[row, lane];
                    if (pane == null) continue;
                    Collider paneCollider = pane.GetComponent<Collider>();
                    if (paneCollider != null) paneCollider.enabled = true;
                    SetPaneColor(pane, new Color(.15f, .32f, .42f));
                }
            }
            nextNode = 0;
            mistakes = 0;
            timeRemaining = timeLimit;
            answerFeedbackTime = 0f;
            crossingUnlocked = false;
            complete = false;
            failed = false;
            SetNodeColor(circuitNodes[0], new Color(.1f, .95f, 1f));
            status = "Reach the first checkpoint to read the riddle.";
            if (ambience != null && !ambience.isPlaying) ambience.Play();
            if (endOverlay != null) endOverlay.SetActive(false);
            SetPlayerMovementEnabled(true);
            RefreshHud();
        }

        private void ConfigureHudLayout()
        {
            if (statusLabel == null) return;
            RectTransform panel = statusLabel.transform.parent as RectTransform;
            if (panel != null)
            {
                panel.anchorMin = panel.anchorMax = new Vector2(.5f, 1f);
                panel.pivot = new Vector2(.5f, 1f);
                panel.anchoredPosition = new Vector2(0f, -20f);
                panel.sizeDelta = new Vector2(920f, 470f);
            }

            Text header = panel != null ? panel.GetComponentInChildren<Text>() : null;
            if (header != null && header != statusLabel)
            {
                header.rectTransform.anchorMin = header.rectTransform.anchorMax = new Vector2(0f, 1f);
                header.rectTransform.pivot = new Vector2(0f, 1f);
                header.text = "LEVEL 2 — NEON GLASS PATH";
                header.rectTransform.anchoredPosition = new Vector2(26f, -18f);
                header.rectTransform.sizeDelta = new Vector2(868f, 42f);
                header.fontSize = 27;
            }

            statusLabel.rectTransform.anchorMin = statusLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            statusLabel.rectTransform.pivot = new Vector2(0f, 1f);
            statusLabel.rectTransform.anchoredPosition = new Vector2(26f, -72f);
            statusLabel.rectTransform.sizeDelta = new Vector2(868f, 190f);
            statusLabel.fontSize = 21;
            statusLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusLabel.verticalOverflow = VerticalWrapMode.Truncate;

            if (progressLabel != null)
            {
                progressLabel.rectTransform.anchorMin = progressLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
                progressLabel.rectTransform.pivot = new Vector2(0f, 1f);
                progressLabel.rectTransform.anchoredPosition = new Vector2(26f, -424f);
                progressLabel.rectTransform.sizeDelta = new Vector2(600f, 30f);
                progressLabel.fontSize = 17;
            }
            if (timerLabel != null)
            {
                timerLabel.rectTransform.anchorMin = timerLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
                timerLabel.rectTransform.pivot = new Vector2(0f, 1f);
                timerLabel.rectTransform.anchoredPosition = new Vector2(710f, -424f);
                timerLabel.rectTransform.sizeDelta = new Vector2(180f, 30f);
                timerLabel.fontSize = 17;
            }
        }

        private void RefreshHud()
        {
            if (statusLabel != null)
            {
                bool showingQuestion = answerButtons[0] != null && answerButtons[0].gameObject.activeSelf;
                statusLabel.text = showingQuestion ? status : Rules + "\n" + status;
            }
            if (progressLabel != null)
            {
                progressLabel.text = "RIDDLE / ROW " + Mathf.Min(nextNode, BridgeRows) + "/8  |  WRONG ANSWERS " + mistakes + "/1";
            }
            if (timerLabel != null)
                timerLabel.text = "TIME " + Mathf.CeilToInt(timeRemaining) + "s" + (complete ? " · DONE" : failed ? " · OVER" : "");
        }

        private void ShowEndOverlay(string title, string reason)
        {
            if (endOverlay == null)
            {
                Canvas canvas = statusLabel != null ? statusLabel.canvas : null;
                if (canvas == null) return;
                endOverlay = new GameObject("Game Over Screen", typeof(RectTransform), typeof(Image));
                endOverlay.transform.SetParent(canvas.transform, false);
                RectTransform fullScreen = endOverlay.GetComponent<RectTransform>();
                fullScreen.anchorMin = Vector2.zero;
                fullScreen.anchorMax = Vector2.one;
                fullScreen.offsetMin = Vector2.zero;
                fullScreen.offsetMax = Vector2.zero;
                Image dim = endOverlay.GetComponent<Image>();
                dim.color = new Color(0f, 0f, 0f, .68f);
                dim.raycastTarget = false;

                GameObject card = new GameObject("Game Over Card", typeof(RectTransform), typeof(Image));
                card.transform.SetParent(endOverlay.transform, false);
                RectTransform cardRect = card.GetComponent<RectTransform>();
                cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f, .5f);
                cardRect.pivot = new Vector2(.5f, .5f);
                cardRect.anchoredPosition = Vector2.zero;
                cardRect.sizeDelta = new Vector2(640f, 250f);
                card.GetComponent<Image>().color = new Color(.035f, .055f, .09f, .98f);
                endOverlayText = CreateOverlayText(card.transform, "", 30);
                endOverlay.transform.SetAsLastSibling();
            }
            endOverlayText.text = title + "\n" + reason + "\n\nPress R to retry";
            endOverlay.SetActive(true);
            endOverlay.transform.SetAsLastSibling();
        }

        private static Text CreateOverlayText(Transform parent, string value, int fontSize)
        {
            GameObject textObject = new GameObject("Game Over Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(22f, 18f);
            rect.offsetMax = new Vector2(-22f, -18f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(1f, .85f, .8f, 1f);
            text.raycastTarget = false;
            return text;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0;
            b.y = 0;
            return Vector3.Distance(a, b);
        }

        private static void SetNodeColor(Transform node, Color color)
        {
            if (node == null) return;
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer == null) return;
            Material material = renderer.material;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 2);
        }
    }
}
