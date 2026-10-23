using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheLastMemory
{
    /// <summary>Shows a simple, clickable start screen before the first level.</summary>
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        private static MainMenuBootstrap instance;
        private readonly HashSet<PlayerController> pausedPlayers = new HashSet<PlayerController>();
        private Texture2D pixel;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle footerStyle;
        private bool started;
        private const float DesignWidth = 1280f;
        private const float DesignHeight = 720f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateMenu()
        {
            if (instance != null) return;
            GameObject menuObject = new GameObject("The Last Memory Main Menu");
            instance = menuObject.AddComponent<MainMenuBootstrap>();
            DontDestroyOnLoad(menuObject);
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }

        private void Update()
        {
            if (started) return;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (PlayerController player in players)
            {
                if (player == null || !player.enabled) continue;
                pausedPlayers.Add(player);
                player.enabled = false;
            }
        }

        private void OnGUI()
        {
            if (started) return;
            EnsureStyles();

            float scale = Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3((Screen.width - DesignWidth * scale) * .5f, (Screen.height - DesignHeight * scale) * .5f, 0f),
                Quaternion.identity,
                Vector3.one * scale);

            DrawRect(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(.012f, .022f, .04f, 1f));
            DrawRect(new Rect(0f, 0f, DesignWidth, 5f), new Color(.08f, .9f, 1f));
            DrawRect(new Rect(0f, 715f, DesignWidth, 5f), new Color(.08f, .9f, 1f));
            DrawRect(new Rect(0f, 0f, 150f, DesignHeight), new Color(.02f, .1f, .15f, 1f));
            DrawRect(new Rect(1128f, 0f, 152f, DesignHeight), new Color(.02f, .1f, .15f, 1f));

            DrawRect(new Rect(260f, 108f, 760f, 504f), new Color(.018f, .045f, .073f, .98f));
            DrawRect(new Rect(260f, 108f, 760f, 2f), new Color(.08f, .9f, 1f, .9f));
            DrawRect(new Rect(260f, 610f, 760f, 2f), new Color(.08f, .9f, 1f, .9f));
            GUI.Label(new Rect(300f, 164f, 680f, 86f), "THE LAST MEMORY", titleStyle);
            GUI.Label(new Rect(320f, 247f, 640f, 50f), "A MACHINE'S JOURNEY BACK TO BEING HUMAN", subtitleStyle);
            DrawRect(new Rect(500f, 320f, 280f, 2f), new Color(.08f, .9f, 1f, .7f));

            if (GUI.Button(new Rect(450f, 370f, 380f, 72f), "START GAME", buttonStyle))
                StartGame();
            if (GUI.Button(new Rect(450f, 466f, 380f, 72f), "QUIT GAME", buttonStyle))
                QuitGame();

            GUI.Label(new Rect(330f, 557f, 620f, 32f), "W A S D TO MOVE   •   MOUSE TO LOOK", footerStyle);
            GUI.matrix = previousMatrix;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 56,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(.78f, .98f, 1f) }
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 19,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(.54f, .72f, .81f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = MakeTexture(new Color(.025f, .18f, .25f, 1f)), textColor = new Color(.72f, .98f, 1f) },
                hover = { background = MakeTexture(new Color(.04f, .36f, .44f, 1f)), textColor = Color.white },
                active = { background = MakeTexture(new Color(.08f, .72f, .78f, 1f)), textColor = Color.white },
                border = new RectOffset(2, 2, 2, 2)
            };
            footerStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(.48f, .65f, .71f) }
            };
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        private void StartGame()
        {
            started = true;
            foreach (PlayerController player in pausedPlayers)
                if (player != null) player.enabled = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(0);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (pixel != null) Destroy(pixel);
            if (instance == this) instance = null;
        }
    }
}
