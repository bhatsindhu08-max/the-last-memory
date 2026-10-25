using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheLastMemory
{
    /// <summary>Level 3's neon alley laser retrieval challenge.</summary>
    public sealed class NeonCourierMission : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] dataCaches;
        [SerializeField] private Transform[] securityZones;
        [SerializeField] private Transform extraction;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Text timerLabel;
        [SerializeField, Min(30f)] private float timeLimit = 120f;
        [SerializeField, Min(1)] private int cachesRequired = 5;
        [SerializeField, Min(1)] private int maxAlerts = 3;
        [SerializeField, Min(0.5f)] private float pickupRadius = 6.5f;
        [SerializeField, Min(1f)] private float pickupPromptRadius = 14f;
        [SerializeField, Min(0.5f)] private float alertRadius = 0.55f;
        [SerializeField] private AudioSource ambience;
        [SerializeField] private string nextSceneName = "";

        private readonly List<Transform> beamSegments = new List<Transform>();
        private readonly List<Transform> pickups = new List<Transform>();
        private readonly List<Vector3> pickupStartPositions = new List<Vector3>();
        private readonly HashSet<int> collected = new HashSet<int>();
        private Transform world;
        private Transform home;
        private readonly List<Transform> carriedItems = new List<Transform>();
        private Material laserMaterial;
        private Material warningMaterial;
        private Material pickupMaterial;
        private Material homeMaterial;
        private GameObject endOverlay;
        private Text endOverlayText;
        private float timeRemaining;
        private float patternClock;
        private float hitCooldown;
        private int patternIndex;
        private int score;
        private int alerts;
        private bool failed;
        private bool complete;
        private string status = "LEVEL 3 — COLLECT YOUR MEMORIES BEFORE THE LASERS CATCH YOU!";

        // Five shifting laser barriers extend the hazard through more of the alley.
        private static readonly float[] RowZ = { 25f, 31f, 37f, 43f, 49f };
        private static readonly float[,] GapPatterns =
        {
            { -4.3f, 0f, 4.3f, 0f, -4.3f },
            { 4.3f, 0f, -4.3f, 4.3f, 0f },
            { 0f, 4.3f, 0f, -4.3f, 4.3f },
            { -4.3f, 4.3f, -4.3f, 0f, 4.3f }
        };
        private const float GapHalfWidth = 1.35f;
        private const float ArenaHalfWidth = 8f;
        private const float PatternDuration = 5.8f;
        private const float MinimumPatternDuration = 3.25f;
        private const float WarningDuration = 1f;
        private const float HomeRadius = 2.6f;
        private const int PickupCount = 5;

        public void Configure(Transform targetPlayer, Transform[] caches, Transform[] zones, Transform exit, Text statusText, Text progressText, Text timerText)
        {
            player = targetPlayer;
            dataCaches = caches;
            securityZones = zones;
            extraction = exit;
            statusLabel = statusText;
            progressLabel = progressText;
            timerLabel = timerText;
        }

        private void Start()
        {
            timeRemaining = timeLimit;
            maxAlerts = 1;
            FindSceneObjects();
            PrepareArena();
            BuildLaserGrid();
            PlacePickups();
            SetupHud();
            if (ambience == null)
            {
                GameObject soundObject = GameObject.Find("Level 3 — Neon Alley Music");
                if (soundObject != null) ambience = soundObject.GetComponent<AudioSource>();
            }
            if (ambience != null && ambience.clip != null && !ambience.isPlaying) ambience.Play();
            RefreshHud();
        }

        private void FindSceneObjects()
        {
            if (player == null)
            {
                PlayerController controller = FindFirstObjectByType<PlayerController>();
                if (controller != null) player = controller.transform;
            }
            if (dataCaches != null && dataCaches.Length > 0 && dataCaches[0] != null)
                world = dataCaches[0].parent;
            if (world == null && securityZones != null && securityZones.Length > 0 && securityZones[0] != null)
                world = securityZones[0].parent;
        }

        private void PrepareArena()
        {
            if (world == null) return;

            // Remove the old scan mission's obstacles and decoration while preserving the alley scene.
            foreach (Transform zone in securityZones)
                if (zone != null) zone.gameObject.SetActive(false);
            if (extraction != null) extraction.gameObject.SetActive(false);

            for (int i = 0; i < world.childCount; i++)
            {
                Transform child = world.GetChild(i);
                if (child.name.StartsWith("Street barrier") || child.name == "Cyan Safe Route Markers" ||
                    child.name == "Drive pedestal" || child.name.StartsWith("DATA ") || child.name == "Scan beacon")
                    child.gameObject.SetActive(false);
            }

            // Place five existing scene objects as the collectible cores; this keeps the scene wired
            // to its current assets and avoids spawning a second player or another scene.
            Vector3[] positions =
            {
                new Vector3(-6f, 0.95f, 13f),
                new Vector3(6f, 0.95f, 20f),
                new Vector3(-4.5f, 0.95f, 45f),
                new Vector3(6f, 0.95f, 57f),
                new Vector3(-3.5f, 0.95f, 63f)
            };
            pickups.Clear();
            pickupStartPositions.Clear();
            if (dataCaches != null)
            {
                for (int i = 0; i < dataCaches.Length && i < PickupCount; i++)
                {
                    Transform item = dataCaches[i];
                    if (item == null) continue;
                    item.gameObject.SetActive(true);
                    item.SetParent(world, true);
                    item.position = positions[i];
                    pickupStartPositions.Add(item.position);
                    item.localScale = Vector3.one * 0.75f;
                    item.rotation = Quaternion.identity;
                    Collider collider = item.GetComponent<Collider>();
                    if (collider != null) collider.enabled = false;
                    Renderer renderer = item.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                        renderer.sharedMaterial = GetPickupMaterial();
                    }
                    item.name = "Memory " + (i + 1);
                    pickups.Add(item);
                    CreateCoreLabel(item, i + 1);
                }
            }
            ResolvePlayer();
            if (player != null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null && player.position.z < 0.5f)
                {
                    controller.enabled = false;
                    player.SetPositionAndRotation(new Vector3(0f, 1f, 0f), Quaternion.identity);
                    controller.enabled = true;
                }
            }
            BuildHomeZone();
        }

        private void BuildHomeZone()
        {
            home = new GameObject("Core Return Zone").transform;
            home.SetParent(world, false);
            home.position = new Vector3(0f, 0.04f, 1.5f);
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Neon home pad";
            ring.transform.SetParent(home, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(5.2f, 0.05f, 5.2f);
            DisableCollider(ring);
            ring.GetComponent<Renderer>().sharedMaterial = GetHomeMaterial();
            CreateWorldLabel("START / RETURN MEMORIES", home.position + Vector3.up * 0.3f, new Color32(70, 245, 255, 255));
        }

        private void BuildLaserGrid()
        {
            if (world == null) return;
            GetLaserMaterials();
            beamSegments.Clear();
            for (int row = 0; row < RowZ.Length; row++)
            {
                for (int side = 0; side < 2; side++)
                {
                    GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    beam.name = "Shifting laser row " + (row + 1) + " segment " + (side + 1);
                    beam.transform.SetParent(world, false);
                    DisableCollider(beam);
                    beam.GetComponent<Renderer>().sharedMaterial = laserMaterial;
                    beamSegments.Add(beam.transform);
                }
            }
            ApplyLaserPattern();
        }

        private void PlacePickups()
        {
            foreach (Transform item in pickups)
            {
                if (item == null) continue;
                item.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && (failed || complete))
            {
                Retry();
                return;
            }
            if (failed || complete) return;
            ResolvePlayer();
            if (player == null) return;

            timeRemaining -= Time.deltaTime;
            patternClock += Time.deltaTime;
            hitCooldown = Mathf.Max(0f, hitCooldown - Time.deltaTime);
            UpdateLaserPattern();
            CheckPickupsAndDelivery();
            if (!complete && !failed) CheckLaserContact();

            if (!failed && !complete && timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                Fail("TIME UP — final score " + score);
            }
            RefreshHud();
        }

        private void ResolvePlayer()
        {
            if (player != null && player.gameObject.activeInHierarchy) return;
            PlayerController controller = FindFirstObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
        }

        private void CheckPickupsAndDelivery()
        {
            if (carriedItems.Count > 0 && FlatDistance(player.position, home.position) <= HomeRadius)
            {
                int deliveredNow = carriedItems.Count;
                foreach (Transform item in carriedItems)
                {
                    if (item == null) continue;
                    item.SetParent(world, true);
                    item.gameObject.SetActive(false);
                }
                carriedItems.Clear();
                score += deliveredNow * 100;
                status = deliveredNow + " MEMOR" + (deliveredNow == 1 ? "Y" : "IES") + " RETURNED +" + (deliveredNow * 100) + ". Find the rest.";
                if (collected.Count >= PickupCount && score < PickupCount * 100)
                    status = "ALL MEMORIES FOUND — return every carried memory to START to open Level 4.";
                if (collected.Count >= PickupCount && score >= PickupCount * 100)
                {
                    complete = true;
                    status = "ARENA CLEARED — all memories returned! The final signal to Level 4 is opening...";
                    ShowEndOverlay("LEVEL 3 COMPLETE", "All five memories are back at START. Loading Level 4...", false);
                    StartCoroutine(LoadNextSceneAfterDelay());
                }
            }

            Keyboard keyboard = Keyboard.current;
            bool interactPressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            Transform nearestItem = null;
            int nearestIndex = -1;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < pickups.Count; i++)
            {
                Transform item = pickups[i];
                if (item == null || collected.Contains(i) || !item.gameObject.activeSelf) continue;
                float distance = FlatDistance(player.position, item.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestItem = item;
                    nearestIndex = i;
                }
            }

            if (nearestItem == null) return;
            if (nearestDistance <= pickupPromptRadius)
            {
                status = "MEMORY " + (nearestIndex + 1) + " NEARBY — press E or move closer to collect it.";
                if (nearestDistance <= pickupRadius || interactPressed)
                {
                    collected.Add(nearestIndex);
                    int carrySlot = carriedItems.Count;
                    carriedItems.Add(nearestItem);
                    nearestItem.SetParent(player, false);
                    nearestItem.localPosition = new Vector3(0.58f + (carrySlot % 3) * 0.28f, -0.38f - (carrySlot / 3) * 0.28f, 0.3f);
                    nearestItem.localRotation = Quaternion.Euler(15f, 35f, 20f);
                    nearestItem.localScale = Vector3.one * 0.32f;
                    SetCoreLabelVisible(nearestItem, false);
                    nearestItem.gameObject.SetActive(true);
                    status = collected.Count >= PickupCount
                        ? "ALL 5 MEMORIES FOUND — return them to START to open Level 4."
                        : "MEMORY " + (nearestIndex + 1) + " COLLECTED — " + carriedItems.Count + " carried. Keep searching or return to START to bank them.";
                }
            }
        }

        private void CheckLaserContact()
        {
            float patternDuration = CurrentPatternDuration();
            if (hitCooldown > 0f) return;
            float gapMargin = GapHalfWidth + 0.58f;
            for (int row = 0; row < RowZ.Length; row++)
            {
                if (Mathf.Abs(player.position.z - RowZ[row]) > 0.48f) continue;
                float gapCenter = GapPatterns[patternIndex, row];
                if (Mathf.Abs(player.position.x - gapCenter) < gapMargin) continue;

                alerts = maxAlerts;
                timeRemaining = Mathf.Max(0f, timeRemaining - 8f);
                Fail("LASER TOUCHED — final score " + score);
                return;
            }
        }

        private void UpdateLaserPattern()
        {
            float patternDuration = CurrentPatternDuration();
            if (patternClock >= patternDuration)
            {
                patternClock -= patternDuration;
                patternIndex = (patternIndex + Random.Range(1, GapPatterns.GetLength(0))) % GapPatterns.GetLength(0);
                ApplyLaserPattern();
            }
            bool warning = patternClock >= patternDuration - WarningDuration;
            Material shownMaterial = warning ? warningMaterial : laserMaterial;
            for (int i = 0; i < beamSegments.Count; i++)
            {
                Renderer renderer = beamSegments[i] != null ? beamSegments[i].GetComponent<Renderer>() : null;
                if (renderer != null) renderer.sharedMaterial = shownMaterial;
            }
            if (warning && status.StartsWith("Search") || warning && status.StartsWith("CORE"))
                status = "LASERS SHIFTING — orange means the pattern changes soon.";
        }

        private float CurrentPatternDuration()
        {
            int delivered = score / 100;
            return Mathf.Max(MinimumPatternDuration, PatternDuration - delivered * 0.65f);
        }

        private void ApplyLaserPattern()
        {
            for (int row = 0; row < RowZ.Length; row++)
            {
                float gap = GapPatterns[patternIndex, row];
                float leftLength = gap - GapHalfWidth + ArenaHalfWidth;
                float rightLength = ArenaHalfWidth - (gap + GapHalfWidth);
                int first = row * 2;
                SetBeam(beamSegments[first], -ArenaHalfWidth + leftLength * 0.5f, RowZ[row], Mathf.Max(0.01f, leftLength));
                SetBeam(beamSegments[first + 1], gap + GapHalfWidth + rightLength * 0.5f, RowZ[row], Mathf.Max(0.01f, rightLength));
            }
        }

        private static void SetBeam(Transform beam, float x, float z, float length)
        {
            beam.position = new Vector3(x, 0.62f, z);
            beam.localScale = new Vector3(length, 0.075f, 0.11f);
        }

        private void SetupHud()
        {
            if (statusLabel == null) return;
            RectTransform panel = statusLabel.transform.parent as RectTransform;
            if (panel != null) panel.sizeDelta = new Vector2(700f, 210f);
            Text header = panel != null ? panel.GetComponentInChildren<Text>() : null;
            if (header != null && header != statusLabel)
            {
                header.text = "LEVEL 3 — COLLECT YOUR MEMORIES";
                header.rectTransform.anchoredPosition = new Vector2(18f, -12f);
                header.rectTransform.sizeDelta = new Vector2(660f, 30f);
                header.fontSize = 21;
            }
            statusLabel.rectTransform.anchoredPosition = new Vector2(18f, -48f);
            statusLabel.rectTransform.sizeDelta = new Vector2(660f, 105f);
            statusLabel.fontSize = 15;
            statusLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusLabel.verticalOverflow = VerticalWrapMode.Overflow;
            if (progressLabel != null)
            {
                progressLabel.rectTransform.anchoredPosition = new Vector2(18f, -166f);
                progressLabel.rectTransform.sizeDelta = new Vector2(450f, 28f);
            }
            if (timerLabel != null)
            {
                timerLabel.rectTransform.anchoredPosition = new Vector2(500f, -166f);
                timerLabel.rectTransform.sizeDelta = new Vector2(160f, 28f);
            }
        }

        private void Fail(string reason)
        {
            if (failed) return;
            failed = true;
            status = "GAME OVER — " + reason + ". Press R to retry.";
            ShowEndOverlay("GAME OVER", reason);
        }

        private IEnumerator LoadNextSceneAfterDelay()
        {
            yield return new WaitForSecondsRealtime(3.5f);

            string targetScene = string.IsNullOrWhiteSpace(nextSceneName) ? "Level4_CyberMaze" : nextSceneName;
            int sceneBuildIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + targetScene + ".unity");
            if (sceneBuildIndex >= 0)
                SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Single);
            else if (Application.CanStreamedLevelBeLoaded(targetScene))
                SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
            else
            {
                const string message = "Level 4 could not be loaded. Check that Level4_CyberMaze is enabled in Build Settings.";
                Debug.LogError(message, this);
                if (endOverlayText != null)
                    endOverlayText.text = "LEVEL 3 COMPLETE\n" + message + "\n\nPress R to replay Level 3";
            }
        }

        private void RefreshHud()
        {
            if (statusLabel != null)
                statusLabel.text = "LEVEL 3 — COLLECT YOUR MEMORIES BEFORE THE LASERS CATCH YOU!\nHOW TO PLAY: Move with WASD + mouse. Collect MEMORY 1–5 by walking close or pressing E, then return to START to bank them for points. Cross each laser row through its gap. Orange means the gaps shift soon. Touch a laser or run out of time and the game ends. Press R to retry.\n\n" + status;
            if (progressLabel != null)
                progressLabel.text = "MEMORIES " + Mathf.Min(collected.Count, PickupCount) + "/" + PickupCount + "   |   RETURNED " + (score / 100) + "   |   SCORE " + score + "   |   STRIKES " + alerts + "/" + maxAlerts;
            if (timerLabel != null)
                timerLabel.text = "TIME " + Mathf.CeilToInt(timeRemaining) + "s" + (complete ? " · CLEAR" : failed ? " · OVER" : "");
        }

        private void Retry()
        {
            ResolvePlayer();
            if (player != null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                player.SetPositionAndRotation(new Vector3(0f, 1f, 0f), Quaternion.identity);
                if (controller != null) controller.enabled = true;
            }
            collected.Clear();
            for (int i = 0; i < pickups.Count; i++)
            {
                Transform item = pickups[i];
                if (item == null) continue;
                item.SetParent(world, true);
                if (i < pickupStartPositions.Count) item.position = pickupStartPositions[i];
                item.localScale = Vector3.one * 0.75f;
                item.rotation = Quaternion.identity;
                SetCoreLabelVisible(item, true);
                item.gameObject.SetActive(true);
            }
            carriedItems.Clear();
            if (endOverlay != null) endOverlay.SetActive(false);
            score = 0;
            alerts = 0;
            hitCooldown = 0f;
            timeRemaining = timeLimit;
            patternClock = 0f;
            patternIndex = Random.Range(0, GapPatterns.GetLength(0));
            failed = false;
            complete = false;
            status = "LEVEL 3 — COLLECT YOUR MEMORIES BEFORE THE LASERS CATCH YOU!";
            alerts = 0;
            ApplyLaserPattern();
            RefreshHud();
        }

        private void ShowEndOverlay(string title, string reason, bool showRetry = true)
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
            }
            endOverlayText.text = title + "\n" + reason + (showRetry ? "\n\nPress R to retry" : "");
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

        private void CreateCoreLabel(Transform item, int number)
        {
            GameObject labelObject = new GameObject("MEMORY " + number, typeof(TextMesh));
            labelObject.transform.SetParent(item, false);
            labelObject.transform.localPosition = Vector3.up * 0.85f;
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh label = labelObject.GetComponent<TextMesh>();
            label.text = "MEMORY " + number + "\nWALK CLOSE OR PRESS E";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.22f;
            label.fontSize = 40;
            label.color = new Color32(105, 255, 255, 255);
        }

        private static void SetCoreLabelVisible(Transform item, bool visible)
        {
            TextMesh[] labels = item.GetComponentsInChildren<TextMesh>(true);
            foreach (TextMesh label in labels) label.gameObject.SetActive(visible);
        }

        private void CreateWorldLabel(string text, Vector3 position, Color color)
        {
            GameObject labelObject = new GameObject(text, typeof(TextMesh));
            labelObject.transform.SetParent(world, true);
            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh label = labelObject.GetComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.22f;
            label.fontSize = 40;
            label.color = color;
        }

        private Material GetPickupMaterial()
        {
            if (pickupMaterial == null) pickupMaterial = NeonMaterial("L3_Core_Cyan", new Color32(20, 205, 240, 255), new Color(0.05f, 0.9f, 1f));
            return pickupMaterial;
        }

        private Material GetHomeMaterial()
        {
            if (homeMaterial == null) homeMaterial = NeonMaterial("L3_Home_Green", new Color32(20, 190, 135, 255), new Color(0.04f, 0.9f, 0.45f));
            return homeMaterial;
        }

        private void GetLaserMaterials()
        {
            if (laserMaterial == null) laserMaterial = NeonMaterial("L3_Laser_Red", new Color32(240, 22, 50, 255), new Color(1f, 0.015f, 0.025f));
            if (warningMaterial == null) warningMaterial = NeonMaterial("L3_Laser_Warning", new Color32(255, 150, 30, 255), new Color(1f, 0.38f, 0.03f));
        }

        private static Material NeonMaterial(string name, Color color, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission * 2f);
            }
            return material;
        }

        private static void DisableCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
