using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TheLastMemory
{
    /// <summary>Final story level: find home in the neon city and restore the machine's humanity.</summary>
    public sealed class CyberMazeMission : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform playerVisual;
        [SerializeField] private Transform homeBeacon;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text progressLabel;
        [SerializeField] private AudioSource ambience;
        [SerializeField, Min(1f)] private float homeRadius = 3.1f;

        private PlayerController controller;
        private bool ending;
        private GameObject endOverlay;
        private Text endingText;
        private readonly Queue<GameObject> breadcrumbs = new Queue<GameObject>();
        private readonly Queue<GameObject> returnFlags = new Queue<GameObject>();
        private Material breadcrumbMaterial;
        private Material flagMaterial;
        private Vector3 lastBreadcrumbPosition;
        private float nextHintUpdate;
        private const float BreadcrumbSpacing = 2.5f;
        private const int MaxBreadcrumbs = 180;
        private const int MaxFlags = 24;

        public void Configure(Transform targetPlayer, Transform visual, Transform home, Text status, Text progress, AudioSource music)
        {
            player = targetPlayer;
            playerVisual = visual;
            homeBeacon = home;
            statusLabel = status;
            progressLabel = progress;
            ambience = music;
        }

        private void Start()
        {
            ResolveReferences();
            if (ambience != null && ambience.clip != null)
            {
                ambience.loop = true;
                if (!ambience.isPlaying) ambience.Play();
            }
            if (statusLabel != null)
                statusLabel.text = "Find the bright HOME beacon at the end of the city maze. Follow the streets through the buildings; no riddles or laser traps remain.";
            if (player != null)
            {
                lastBreadcrumbPosition = player.position;
                DropBreadcrumb(lastBreadcrumbPosition);
            }
            if (progressLabel != null) progressLabel.text = "CYAN TRAIL: YOUR ROUTE  •  F: PLANT A RETURN FLAG";
        }

        private void Update()
        {
            if (ending)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            ResolveReferences();
            if (player == null || homeBeacon == null) return;

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                PlantReturnFlag();

            Vector3 travelled = player.position - lastBreadcrumbPosition;
            travelled.y = 0f;
            if (travelled.sqrMagnitude >= BreadcrumbSpacing * BreadcrumbSpacing)
            {
                lastBreadcrumbPosition = player.position;
                DropBreadcrumb(player.position);
            }

            if (Time.unscaledTime >= nextHintUpdate)
            {
                UpdateHomeHint();
                nextHintUpdate = Time.unscaledTime + .35f;
            }

            Vector3 delta = player.position - homeBeacon.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= homeRadius * homeRadius) BeginEnding();
        }

        private void ResolveReferences()
        {
            if (player == null)
            {
                controller = FindFirstObjectByType<PlayerController>();
                if (controller != null) player = controller.transform;
            }
            else if (controller == null) controller = player.GetComponent<PlayerController>();

            if (playerVisual == null && player != null)
            {
                Transform capsule = player.Find("Capsule");
                if (capsule != null) playerVisual = capsule;
            }
            if (homeBeacon == null)
            {
                GameObject beacon = GameObject.Find("HOME Beacon");
                if (beacon != null) homeBeacon = beacon.transform;
            }
        }


        private void UpdateHomeHint()
        {
            if (progressLabel == null || player == null || homeBeacon == null) return;
            Vector3 toHome = homeBeacon.position - player.position;
            toHome.y = 0f;
            float distance = toHome.magnitude;
            Vector3 forward = player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            float ahead = Vector3.Dot(toHome, forward);
            float side = Vector3.Dot(toHome, right);
            string direction;
            if (Mathf.Abs(ahead) > Mathf.Abs(side)) direction = ahead >= 0f ? "AHEAD" : "BEHIND";
            else direction = side >= 0f ? "RIGHT" : "LEFT";
            progressLabel.text = $"HOME  •  {distance:0}m {direction}  •  CYAN TRAIL: YOUR ROUTE  •  F: FLAG";
        }

        private void DropBreadcrumb(Vector3 position)
        {
            if (breadcrumbMaterial == null)
                breadcrumbMaterial = MakeMaterial(new Color32(35, 240, 255, 255), new Color(.05f, .8f, 1f));
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Maze Breadcrumb";
            marker.transform.position = new Vector3(position.x, .045f, position.z);
            marker.transform.localScale = new Vector3(.52f, .035f, .52f);
            marker.GetComponent<Renderer>().sharedMaterial = breadcrumbMaterial;
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            breadcrumbs.Enqueue(marker);
            while (breadcrumbs.Count > MaxBreadcrumbs)
            {
                GameObject oldest = breadcrumbs.Dequeue();
                if (oldest != null) Destroy(oldest);
            }
        }

        private void PlantReturnFlag()
        {
            if (flagMaterial == null)
                flagMaterial = MakeMaterial(new Color32(255, 190, 60, 255), new Color(1f, .42f, .04f));
            Vector3 ground = new Vector3(player.position.x, .04f, player.position.z);
            GameObject flag = new GameObject($"Return Flag {returnFlags.Count + 1}");
            GameObject baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRing.name = "Flag Base";
            baseRing.transform.SetParent(flag.transform, false);
            baseRing.transform.localPosition = Vector3.zero;
            baseRing.transform.localScale = new Vector3(.8f, .045f, .8f);
            baseRing.GetComponent<Renderer>().sharedMaterial = flagMaterial;
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Flag Mast";
            mast.transform.SetParent(flag.transform, false);
            mast.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            mast.transform.localScale = new Vector3(.07f, 1.25f, .07f);
            mast.GetComponent<Renderer>().sharedMaterial = flagMaterial;
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "Flag Banner";
            banner.transform.SetParent(flag.transform, false);
            banner.transform.localPosition = new Vector3(.34f, 2.12f, 0f);
            banner.transform.localScale = new Vector3(.68f, .36f, .06f);
            banner.GetComponent<Renderer>().sharedMaterial = flagMaterial;
            foreach (Collider col in flag.GetComponentsInChildren<Collider>()) Destroy(col);
            flag.transform.position = ground;
            returnFlags.Enqueue(flag);
            while (returnFlags.Count > MaxFlags)
            {
                GameObject oldest = returnFlags.Dequeue();
                if (oldest != null) Destroy(oldest);
            }
            if (statusLabel != null) statusLabel.text = "RETURN FLAG PLANTED  •  Follow the cyan trail back here";
        }

        private void BeginEnding()
        {
            ending = true;
            if (controller != null) controller.enabled = false;
            CharacterController collider = player != null ? player.GetComponent<CharacterController>() : null;
            if (collider != null) collider.enabled = false;
            if (ambience != null) ambience.Stop();

            if (playerVisual != null)
            {
                Transform machine = playerVisual.Find("Cybernetic Avatar");
                if (machine != null) machine.gameObject.SetActive(false);
                Renderer oldMesh = playerVisual.GetComponent<Renderer>();
                if (oldMesh != null) oldMesh.enabled = false;
                CreateHumanForm(playerVisual);
            }

            if (statusLabel != null) statusLabel.text = "The signal is familiar. The machine remembers who it was.";
            if (progressLabel != null) progressLabel.text = "HOME REACHED  •  MEMORY RESTORED";
            ShowEnding();
        }

        private static void CreateHumanForm(Transform parent)
        {
            Transform prior = parent.Find("Human Form");
            if (prior != null) return;
            Transform human = new GameObject("Human Form").transform;
            human.SetParent(parent, false);

            Material skin = MakeMaterial(new Color32(168, 157, 145, 255));
            Material jacket = MakeMaterial(new Color32(75, 91, 102, 255));
            Material pants = MakeMaterial(new Color32(39, 48, 59, 255));
            Material hair = MakeMaterial(new Color32(32, 35, 39, 255));
            Material eye = MakeMaterial(new Color32(80, 205, 215, 255), new Color(.05f, .55f, .65f));

            Part(human, PrimitiveType.Capsule, "Human jacket", new Vector3(0f, .13f, 0f), new Vector3(.34f, .29f, .22f), jacket);
            Part(human, PrimitiveType.Sphere, "Human head", new Vector3(0f, .66f, 0f), new Vector3(.16f, .2f, .15f), skin);
            Part(human, PrimitiveType.Sphere, "Hair", new Vector3(0f, .78f, -.015f), new Vector3(.165f, .09f, .155f), hair);
            Part(human, PrimitiveType.Cube, "Left eye", new Vector3(-.055f, .68f, .138f), new Vector3(.035f, .018f, .012f), eye);
            Part(human, PrimitiveType.Cube, "Right eye", new Vector3(.055f, .68f, .138f), new Vector3(.035f, .018f, .012f), eye);
            Part(human, PrimitiveType.Capsule, "Left arm", new Vector3(-.39f, .08f, 0f), new Vector3(.1f, .25f, .1f), jacket, new Vector3(0f, 0f, -8f));
            Part(human, PrimitiveType.Capsule, "Right arm", new Vector3(.39f, .08f, 0f), new Vector3(.1f, .25f, .1f), jacket, new Vector3(0f, 0f, 8f));
            Part(human, PrimitiveType.Sphere, "Left hand", new Vector3(-.4f, -.22f, .01f), new Vector3(.09f, .09f, .09f), skin);
            Part(human, PrimitiveType.Sphere, "Right hand", new Vector3(.4f, -.22f, .01f), new Vector3(.09f, .09f, .09f), skin);
            Part(human, PrimitiveType.Capsule, "Left trouser", new Vector3(-.15f, -.39f, 0f), new Vector3(.13f, .25f, .14f), pants);
            Part(human, PrimitiveType.Capsule, "Right trouser", new Vector3(.15f, -.39f, 0f), new Vector3(.13f, .25f, .14f), pants);
            Part(human, PrimitiveType.Cube, "Left shoe", new Vector3(-.15f, -.9f, .04f), new Vector3(.15f, .13f, .23f), hair);
            Part(human, PrimitiveType.Cube, "Right shoe", new Vector3(.15f, -.9f, .04f), new Vector3(.15f, .13f, .23f), hair);
        }

        private void ShowEnding()
        {
            Canvas canvas = statusLabel != null ? statusLabel.canvas : null;
            if (canvas == null) return;

            endOverlay = new GameObject("Humanity Restored Ending", typeof(RectTransform), typeof(Image));
            endOverlay.transform.SetParent(canvas.transform, false);
            RectTransform full = endOverlay.GetComponent<RectTransform>();
            full.anchorMin = Vector2.zero;
            full.anchorMax = Vector2.one;
            full.offsetMin = Vector2.zero;
            full.offsetMax = Vector2.zero;
            Image dim = endOverlay.GetComponent<Image>();
            dim.color = new Color(.005f, .02f, .035f, .83f);
            dim.raycastTarget = false;

            GameObject card = new GameObject("Home Ending Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(endOverlay.transform, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(780f, 360f);
            Image background = card.GetComponent<Image>();
            background.color = new Color(.025f, .085f, .105f, .98f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Ending Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(card.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(30f, 28f);
            textRect.offsetMax = new Vector2(-30f, -28f);
            endingText = textObject.GetComponent<Text>();
            endingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            endingText.fontSize = 34;
            endingText.fontStyle = FontStyle.Bold;
            endingText.alignment = TextAnchor.MiddleCenter;
            endingText.horizontalOverflow = HorizontalWrapMode.Wrap;
            endingText.verticalOverflow = VerticalWrapMode.Overflow;
            endingText.color = new Color(.68f, 1f, .96f);
            endingText.text = "HOME FOUND\n\nThe machine remembers who it was.\n\nHumanity restored.\nTHE END\n\nPress R to play again";
        }

        private static void Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3 rotation = default)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(rotation);
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material MakeMaterial(Color color, Color? emission = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value * 2f);
            }
            return material;
        }
    }
}
