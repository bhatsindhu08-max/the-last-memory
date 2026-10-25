using UnityEngine;

namespace TheLastMemory
{
    /// <summary>Decorative digital rain for the cyber maze; it has no gameplay or collision effects.</summary>
    public sealed class FallingCodeRain : MonoBehaviour
    {
        private sealed class Stream
        {
            public float x;
            public float y;
            public float speed;
            public int length;
            public string glyphs;
            public GUIStyle style;
        }

        private const string GlyphSet = "01 01 1010 0101 ABCDEF <> {} [] / + -";
        private Stream[] streams;
        private int configuredWidth;
        private Font font;

        private void OnGUI()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || GUI.skin == null) return;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (streams == null || configuredWidth != Screen.width) ConfigureStreams();

            float fontSize = Mathf.Clamp(Screen.width / 85f, 12f, 19f);
            for (int i = 0; i < streams.Length; i++)
            {
                Stream stream = streams[i];
                if (stream == null || stream.style == null) continue;
                // Keep the top-left instruction area readable.
                if (stream.x < Screen.width * .34f && stream.y < Screen.height * .28f) continue;
                GUI.Label(new Rect(stream.x, stream.y, 30f, (stream.length + 1) * fontSize), stream.glyphs, stream.style);
            }
        }

        private void Update()
        {
            // OnGUI owns initialization because GUI skin/styles are only valid there.
            if (streams == null) return;
            float delta = Mathf.Min(Time.unscaledDeltaTime, .06f);
            float fontSize = Mathf.Clamp(Screen.width / 85f, 12f, 19f);
            for (int i = 0; i < streams.Length; i++)
            {
                Stream stream = streams[i];
                if (stream == null) continue;
                stream.y += stream.speed * delta;
                if (stream.y > Screen.height + stream.length * fontSize)
                {
                    stream.y = -UnityEngine.Random.Range(stream.length * fontSize, Screen.height * .35f + stream.length * fontSize);
                    stream.speed = UnityEngine.Random.Range(32f, 92f);
                    stream.glyphs = MakeGlyphs(stream.length);
                }
            }
        }

        private void ConfigureStreams()
        {
            configuredWidth = Screen.width;
            int count = Mathf.Clamp(Mathf.CeilToInt(Screen.width / 56f), 16, 42);
            streams = new Stream[count];
            float fontSize = Mathf.Clamp(Screen.width / 85f, 12f, 19f);
            GUIStyle baseStyle = GUI.skin.label;
            for (int i = 0; i < count; i++)
            {
                int length = UnityEngine.Random.Range(7, 17);
                float x = (i + .5f) * Screen.width / count + UnityEngine.Random.Range(-10f, 10f);
                float opacity = UnityEngine.Random.Range(.12f, .28f);
                GUIStyle style = new GUIStyle(baseStyle)
                {
                    font = font,
                    fontSize = Mathf.RoundToInt(fontSize),
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = false,
                    richText = false,
                    clipping = TextClipping.Clip,
                    normal = { textColor = new Color(.32f, 1f, .58f, opacity) }
                };
                streams[i] = new Stream
                {
                    x = x,
                    y = UnityEngine.Random.Range(-Screen.height, Screen.height),
                    speed = UnityEngine.Random.Range(32f, 92f),
                    length = length,
                    glyphs = MakeGlyphs(length),
                    style = style
                };
            }
        }

        private static string MakeGlyphs(int length)
        {
            char[] chars = new char[length * 2];
            for (int i = 0; i < length; i++)
            {
                chars[i * 2] = GlyphSet[UnityEngine.Random.Range(0, GlyphSet.Length)];
                chars[i * 2 + 1] = '\n';
            }
            return new string(chars);
        }
    }
}
