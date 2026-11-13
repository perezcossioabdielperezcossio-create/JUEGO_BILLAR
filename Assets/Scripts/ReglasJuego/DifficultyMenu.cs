using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QuestBilliards.Inputs;
using QuestBilliards.UI;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace QuestBilliards.GameRules
{
    [DefaultExecutionOrder(-300)]
    public sealed class DifficultyMenu : MenuBase
    {
        public BilliardsPlayerMode playerMode;
        public BilliardsInputReader inputs;
        public Transform head;
        public PocketDifficultyController table;
        public Material panelMaterial;
        public UITheme theme;
        public bool IsChoosing { get; private set; } = true;
        public int SelectedIndex { get; private set; }
        private GameObject panel;
        private readonly UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[3];
        private bool previousTrigger;
        private bool navigationHeld;
        private readonly string[] labels = { "FACIL  -  troneras de 17 cm", "NORMAL  -  troneras de 15 cm", "DIFICIL  -  troneras de 13 cm" };

        private void Awake()
        {
            Application.runInBackground = true;
            if (theme == null) theme = Resources.Load<UITheme>("UI/UITheme");
            if (theme == null) theme = ScriptableObject.CreateInstance<UITheme>();
            Open(MenuManager.GetOrCreate(playerMode));
        }

        private void Start()
        {
            BuildMenu();
            Highlight();
        }

        private void BuildMenu()
        {
            var canvas = WorldSpaceUIHelper.CreateMenuCanvas("DifficultySelection", head, theme);
            panel = canvas.gameObject;
            Transform contentParent = panel.transform;
            if (theme.menuBackingPrefab == null)
            {
                var frame = WorldSpaceUIHelper.CreateImage(panel.transform, "BrassFrame", theme.frame);
                WorldSpaceUIHelper.Stretch(frame.rectTransform);
                var background = WorldSpaceUIHelper.CreateImage(frame.transform, "WalnutPanel", theme.panel);
                WorldSpaceUIHelper.Stretch(background.rectTransform, 10f);
                contentParent = background.transform;
            }

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(contentParent, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(48f, 38f);
            contentRect.offsetMax = new Vector2(-48f, -38f);
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            AddText(content.transform, "Title", "BILLAR VR", theme.titleSize, theme.title, 90f, true);
            AddText(content.transform, "Subtitle", "Elige la dificultad", theme.subtitleSize, theme.text, 60f, false);
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                buttons[i] = WorldSpaceUIHelper.CreateButton(content.transform, "Difficulty_" + i, labels[i], theme,
                    () => Choose((BilliardsDifficulty)index));
                buttons[i].gameObject.AddComponent<LayoutElement>().preferredHeight = 104f;
            }
            AddText(content.transform, "Help", "Joystick arriba/abajo: elegir | Gatillo: jugar\nB derecho: sentado / movimiento fisico\nSIM: Y/H y T  |  UNITY: 1, 2 o 3", theme.captionSize, theme.text, 110f, false);
        }

        private void AddText(Transform parent, string name, string value, float size, Color color, float height, bool title)
        {
            var text = WorldSpaceUIHelper.CreateText(parent, name, value, size, color, title ? theme.titleFont : theme.bodyFont);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private void Highlight()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var colors = buttons[i].colors;
                colors.normalColor = i == SelectedIndex ? theme.buttonSelected : theme.button;
                buttons[i].colors = colors;
            }
            if (EventSystem.current != null && buttons[SelectedIndex] != null)
                EventSystem.current.SetSelectedGameObject(buttons[SelectedIndex].gameObject);
        }

        public void Choose(BilliardsDifficulty level)
        {
            if (!IsChoosing) return;
            table.Apply(level);
            SelectedIndex = (int)level;
            IsChoosing = false;
            Close();
        }

        protected override void OnMenuVisibilityChanged(bool visible)
        {
            if (visible)
            {
                previousTrigger = true;
                navigationHeld = true;
            }
            if (panel != null) panel.SetActive(visible);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (panel != null) Destroy(panel);
        }

        private void Update()
        {
            if (!IsChoosing || !IsOpen) return;
            bool trigger = inputs != null && (inputs.Right.Trigger || inputs.Left.Trigger);
            float axis = inputs != null ? inputs.Right.Stick.y + inputs.Left.Stick.y : 0f;
            if (Mathf.Abs(axis) > 0.5f && !navigationHeld)
            {
                SelectedIndex = (SelectedIndex + (axis > 0f ? -1 : 1) + buttons.Length) % buttons.Length;
                Highlight();
            }
            navigationHeld = Mathf.Abs(axis) > 0.25f;
#if UNITY_EDITOR
            var keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.digit1Key.wasPressedThisFrame) { Choose(BilliardsDifficulty.Easy); return; }
                if (keys.digit2Key.wasPressedThisFrame) { Choose(BilliardsDifficulty.Normal); return; }
                if (keys.digit3Key.wasPressedThisFrame) { Choose(BilliardsDifficulty.Hard); return; }
                if (keys.enterKey.wasPressedThisFrame) { Choose((BilliardsDifficulty)SelectedIndex); return; }
            }
#endif
            if (trigger && !previousTrigger) Choose((BilliardsDifficulty)SelectedIndex);
            previousTrigger = trigger;
        }
    }
}
