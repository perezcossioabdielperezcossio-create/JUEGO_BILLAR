using TMPro;
using UnityEngine;

namespace QuestBilliards.UI
{
    [CreateAssetMenu(fileName = "UITheme", menuName = "Quest Billiards/UI Theme")]
    public sealed class UITheme : ScriptableObject
    {
        [Header("Vintage Pool Hall")]
        public Color panel = new Color(0.102f, 0.078f, 0.063f, 0.97f);
        public Color frame = new Color(0.722f, 0.525f, 0.043f, 1f);
        public Color button = new Color(0.071f, 0.035f, 0.020f, 1f);
        public Color buttonHover = new Color(0.278f, 0.145f, 0.059f, 1f);
        public Color buttonSelected = new Color(0.024f, 0.220f, 0.086f, 1f);
        public Color title = new Color(0.831f, 0.686f, 0.216f, 1f);
        public Color text = new Color(0.961f, 0.961f, 0.863f, 1f);
        public Color danger = new Color(0.545f, 0.149f, 0.208f, 1f);

        [Header("Typography")]
        public TMP_FontAsset titleFont;
        public TMP_FontAsset bodyFont;
        public float titleSize = 68f;
        public float subtitleSize = 38f;
        public float buttonSize = 36f;
        public float captionSize = 26f;

        [Header("World Space")]
        public GameObject menuBackingPrefab;
        public float menuDistance = 1.1f;
        public float canvasScale = 0.001f;
        public Vector2 menuSize = new Vector2(900f, 820f);
    }
}
