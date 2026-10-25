using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

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
        private bool introPlaying;
        private bool introPrepared;
        private float introStartTime;
        private VideoPlayer introPlayer;
        private AudioSource introVideoAudio;
        private string levelTitle = "LEVEL 1";
        private string levelSubtitle = "GET YOUR SENSES BACK";
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
            SceneManager.sceneLoaded += OnSceneLoaded;
            SetLevelCopy("SampleScene");
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

            if (!introPlaying) return;

            float introElapsed = Time.unscaledTime - introStartTime;
            if (!introPrepared && introElapsed > 12f)
            {
                Debug.LogWarning("Game intro took too long to prepare; continuing to Level 1.", this);
                FinishIntro();
                return;
            }
            if (introPrepared && introElapsed > 75f)
            {
                Debug.LogWarning("Game intro playback timed out; continuing to Level 1.", this);
                FinishIntro();
                return;
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                FinishIntro();
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

            if (introPlaying)
            {
                DrawIntroScreen();
                GUI.matrix = previousMatrix;
                return;
            }

            DrawRect(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(.012f, .022f, .04f, 1f));
            DrawRect(new Rect(0f, 0f, DesignWidth, 5f), new Color(.96f, .055f, .15f));
            DrawRect(new Rect(0f, 715f, DesignWidth, 5f), new Color(.96f, .055f, .15f));
            DrawRect(new Rect(0f, 0f, 150f, DesignHeight), new Color(.12f, .012f, .025f, 1f));
            DrawRect(new Rect(1128f, 0f, 152f, DesignHeight), new Color(.12f, .012f, .025f, 1f));

            DrawRect(new Rect(260f, 108f, 760f, 504f), new Color(.055f, .012f, .025f, .98f));
            DrawRect(new Rect(260f, 108f, 760f, 2f), new Color(.96f, .055f, .15f, .95f));
            DrawRect(new Rect(260f, 610f, 760f, 2f), new Color(.96f, .055f, .15f, .95f));
            GUI.Label(new Rect(300f, 164f, 680f, 86f), levelTitle, titleStyle);
            GUI.Label(new Rect(320f, 247f, 640f, 50f), levelSubtitle, subtitleStyle);
            DrawRect(new Rect(500f, 320f, 280f, 2f), new Color(.96f, .055f, .15f, .75f));

            if (GUI.Button(new Rect(450f, 370f, 380f, 72f), "START GAME", buttonStyle))
                StartGame();
            if (GUI.Button(new Rect(450f, 466f, 380f, 72f), "QUIT GAME", buttonStyle))
                QuitGame();

            GUI.Label(new Rect(330f, 557f, 620f, 32f), "W A S D TO MOVE   •   MOUSE TO LOOK", footerStyle);
            GUI.matrix = previousMatrix;
        }

        private void DrawIntroScreen()
        {
            DrawRect(new Rect(0f, 0f, DesignWidth, DesignHeight), Color.black);
            if (introPlayer != null && introPlayer.isPrepared && introPlayer.texture != null)
                GUI.DrawTexture(new Rect(0f, 0f, DesignWidth, DesignHeight), introPlayer.texture, ScaleMode.ScaleToFit);

            DrawRect(new Rect(0f, 650f, DesignWidth, 70f), new Color(.01f, .015f, .025f, .82f));
            GUI.Label(new Rect(36f, 668f, 520f, 32f), "THE LAST MEMORY  •  OPENING TRANSMISSION", footerStyle);
            if (GUI.Button(new Rect(1020f, 662f, 220f, 46f), "SKIP INTRO  ›", buttonStyle))
                FinishIntro();
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
                normal = { textColor = new Color(1f, .86f, .88f) }
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 19,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, .25f, .34f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = MakeTexture(new Color(.25f, .025f, .055f, 1f)), textColor = new Color(1f, .84f, .86f) },
                hover = { background = MakeTexture(new Color(.48f, .035f, .08f, 1f)), textColor = Color.white },
                active = { background = MakeTexture(new Color(.92f, .08f, .17f, 1f)), textColor = Color.white },
                border = new RectOffset(2, 2, 2, 2)
            };
            footerStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(.66f, .42f, .46f) }
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (started) return;

            // Keep the intro and menu for a normal game launch, but allow an editor-opened
            // level to run directly so level designers can test without replaying earlier levels.
            if (scene.name == "SampleScene")
            {
                SetLevelCopy(scene.name);
                return;
            }

            started = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Debug.Log("Direct level launch: " + scene.name + " (startup menu skipped for level editing).", this);
        }

        private void SetLevelCopy(string sceneName)
        {
            switch (sceneName)
            {
                case "Level2_DotCircuit":
                    levelTitle = "LEVEL 2";
                    levelSubtitle = "REMEMBER THE DIFFERENCE BETWEEN HUMAN AND MACHINE";
                    break;
                case "Level3_NeonCourier":
                    levelTitle = "LEVEL 3";
                    levelSubtitle = "COLLECT YOUR MEMORIES BEFORE THE LASERS CATCH YOU";
                    break;
                case "Level4_CyberMaze":
                    levelTitle = "LEVEL 4";
                    levelSubtitle = "LET'S GO BACK HOME";
                    break;
                default:
                    levelTitle = "LEVEL 1";
                    levelSubtitle = "GET YOUR SENSES BACK";
                    break;
            }
        }

        private void StartGame()
        {
            if (started || introPlaying) return;
            string introPath = Path.Combine(Application.streamingAssetsPath, "game-intro.mp4");
            if (!File.Exists(introPath))
            {
                Debug.LogWarning("Game intro video is missing from StreamingAssets; starting Level 1 directly.");
                BeginGameplay();
                return;
            }

            introPlaying = true;
            introPrepared = false;
            introStartTime = Time.unscaledTime;
            introPlayer = gameObject.AddComponent<VideoPlayer>();
            introPlayer.playOnAwake = false;
            introPlayer.isLooping = false;
            introPlayer.source = VideoSource.Url;
            introPlayer.url = introPath;
            introPlayer.renderMode = VideoRenderMode.APIOnly;
            introPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            introPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            introPlayer.waitForFirstFrame = true;
            introPlayer.skipOnDrop = true;
            introVideoAudio = gameObject.AddComponent<AudioSource>();
            introVideoAudio.playOnAwake = false;
            introVideoAudio.loop = false;
            introVideoAudio.spatialBlend = 0f;
            introVideoAudio.ignoreListenerPause = true;
            introPlayer.EnableAudioTrack(0, true);
            introPlayer.SetTargetAudioSource(0, introVideoAudio);
            introPlayer.prepareCompleted += OnIntroPrepared;
            introPlayer.loopPointReached += OnIntroEnded;
            introPlayer.errorReceived += OnIntroError;

            introPlayer.Prepare();
        }

        private void OnIntroPrepared(VideoPlayer source)
        {
            if (!introPlaying) return;
            introPrepared = true;
            source.Play();
        }

        private void OnIntroEnded(VideoPlayer source)
        {
            FinishIntro();
        }

        private void OnIntroError(VideoPlayer source, string message)
        {
            Debug.LogWarning("Game intro could not play: " + message, this);
            FinishIntro();
        }

        private void FinishIntro()
        {
            if (!introPlaying) return;
            introPlaying = false;

            if (introPlayer != null)
            {
                introPlayer.prepareCompleted -= OnIntroPrepared;
                introPlayer.loopPointReached -= OnIntroEnded;
                introPlayer.errorReceived -= OnIntroError;
                introPlayer.Stop();
                Destroy(introPlayer);
                introPlayer = null;
            }
            if (introVideoAudio != null)
            {
                introVideoAudio.Stop();
                Destroy(introVideoAudio);
                introVideoAudio = null;
            }
            BeginGameplay();
        }

        private void BeginGameplay()
        {
            started = true;
            foreach (PlayerController player in pausedPlayers)
                if (player != null) player.enabled = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            // Start Game from the title menu is the normal campaign entry and always begins at Level 1.
            if (SceneManager.GetActiveScene().name != "SampleScene")
                SceneManager.LoadScene("SampleScene");
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
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (introPlayer != null)
            {
                introPlayer.prepareCompleted -= OnIntroPrepared;
                introPlayer.loopPointReached -= OnIntroEnded;
                introPlayer.errorReceived -= OnIntroError;
            }
            if (pixel != null) Destroy(pixel);
            if (instance == this) instance = null;
        }
    }
}
