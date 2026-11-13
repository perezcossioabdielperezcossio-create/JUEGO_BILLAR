using System.Collections;
using NUnit.Framework;
using QuestBilliards.CueInteraction;
using QuestBilliards.GameRules;
using QuestBilliards.Inputs;
using QuestBilliards.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace QuestBilliards.Tests.PlayMode
{
    public sealed class DifficultyMenuTests
    {
        private GameObject menuRoot;
        private GameObject head;
        private GameObject tableRoot;
        private GameObject playerRoot;
        private GameObject cueRoot;
        private GameObject cueStatus;
        private GameObject backingPrefab;
        private GameObject eventSystem;
        private DifficultyMenu menu;
        private PocketDifficultyController table;
        private BilliardsPlayerMode playerMode;
        private MenuManager manager;
        private UITheme theme;

        [UnityTest]
        public IEnumerator BuildMenu_WithBacking_RemovesOpaquePanelsAndUsesCanvasContent()
        {
            yield return CreateMenu();

            var canvas = head.GetComponentInChildren<Canvas>(true);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.transform.Find("BrassFrame"), Is.Null);
            Assert.That(canvas.transform.Find("WalnutPanel"), Is.Null);
            Assert.That(canvas.transform.Find("Content"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ChooseEasy_AppliesSelectionAndCloses()
        {
            yield return CreateMenu();
            ChooseAndAssert(BilliardsDifficulty.Easy, 0.085f, 0);
        }

        [UnityTest]
        public IEnumerator ChooseNormal_AppliesSelectionAndCloses()
        {
            yield return CreateMenu();
            ChooseAndAssert(BilliardsDifficulty.Normal, 0.075f, 1);
        }

        [UnityTest]
        public IEnumerator ChooseHard_AppliesSelectionAndCloses()
        {
            yield return CreateMenu();
            ChooseAndAssert(BilliardsDifficulty.Hard, 0.065f, 2);
        }

        private void ChooseAndAssert(BilliardsDifficulty level, float radius, int index)
        {
            var button = FindButton(index);
            Assert.That(button.interactable, Is.True);
            button.onClick.Invoke();

            Assert.That(table.CurrentDifficulty, Is.EqualTo(level));
            Assert.That(table.OpeningRadius, Is.EqualTo(radius).Within(0.000001f));
            Assert.That(menu.SelectedIndex, Is.EqualTo(index));
            Assert.That(menu.IsChoosing, Is.False);
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(head.GetComponentInChildren<Canvas>(true).gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator OpenAndClose_LocksGameplayAndRestoresPreviousState()
        {
            yield return CreateMenu(controlsLocked: false, cueStatusActive: true);

            var button = FindButton(0);
            Assert.That(playerMode.ControlsLocked, Is.True);
            Assert.That(cueStatus.activeSelf, Is.False);
            Assert.That(button.interactable, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(button.gameObject));

            menu.Close();

            Assert.That(playerMode.ControlsLocked, Is.False);
            Assert.That(cueStatus.activeSelf, Is.True);
            Assert.That(menu.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator OpenAndClose_RestoresLockedGameplayAndHiddenCueStatus()
        {
            yield return CreateMenu(controlsLocked: true, cueStatusActive: false);

            Assert.That(playerMode.ControlsLocked, Is.True);
            Assert.That(cueStatus.activeSelf, Is.False);
            Assert.That(FindButton(0).interactable, Is.True);

            menu.Close();

            Assert.That(playerMode.ControlsLocked, Is.True);
            Assert.That(cueStatus.activeSelf, Is.False);
            Assert.That(menu.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator OpenMenu_HidesCuePlacementInstructions()
        {
            yield return CreateMenu();
            var ballRoot = new GameObject("WhiteBall", typeof(Rigidbody), typeof(QuestBilliards.BallPhysics.BilliardBallBody));
            ballRoot.transform.SetParent(menuRoot.transform, false);
            var ball = ballRoot.GetComponent<QuestBilliards.BallPhysics.BilliardBallBody>();
            ball.isCueBall = true;
            ball.TryPocket();
            var placementRoot = new GameObject("CueBallPlacement", typeof(CueBallPlacement));
            placementRoot.transform.SetParent(menuRoot.transform, false);
            var placement = placementRoot.GetComponent<CueBallPlacement>();
            placement.whiteBall = ball;
            placement.mode = playerMode;
            placement.head = head.transform;
            yield return null;

            var instructions = head.transform.Find("ScratchInstructions");
            Assert.That(playerMode.ControlsLocked, Is.True);
            Assert.That(instructions, Is.Not.Null);
            Assert.That(instructions.gameObject.activeSelf, Is.False);
        }

        private static UnityEngine.UI.Button FindButton(int index)
        {
            var buttons = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return System.Array.Find(buttons, button => button.name == "Difficulty_" + index);
        }

        private IEnumerator CreateMenu(bool controlsLocked = false, bool cueStatusActive = true)
        {
            eventSystem = null;
            var previousEventSystem = Object.FindAnyObjectByType<EventSystem>();
            head = new GameObject("Head");
            playerRoot = new GameObject("Player", typeof(CharacterController));
            playerMode = playerRoot.AddComponent<BilliardsPlayerMode>();
            playerMode.ControlsLocked = controlsLocked;
            cueStatus = new GameObject("CueStatus", typeof(TextMesh));
            cueStatus.SetActive(cueStatusActive);
            cueRoot = new GameObject("Cue");
            cueRoot.AddComponent<CueGrabController>().statusLabel = cueStatus.GetComponent<TextMesh>();
            tableRoot = new GameObject("Table", typeof(MeshFilter), typeof(MeshCollider));
            table = tableRoot.AddComponent<PocketDifficultyController>();
            table.playingSurface = tableRoot.GetComponent<MeshFilter>();
            table.surfaceCollider = tableRoot.GetComponent<MeshCollider>();
            backingPrefab = new GameObject("ArtDecoMenuBacking", typeof(MeshFilter), typeof(MeshRenderer));
            theme = ScriptableObject.CreateInstance<UITheme>();
            theme.menuBackingPrefab = backingPrefab;
            manager = MenuManager.GetOrCreate(playerMode);
            menuRoot = new GameObject("DifficultyMenu");
            menuRoot.SetActive(false);
            menu = menuRoot.AddComponent<DifficultyMenu>();
            menu.playerMode = playerMode;
            menu.head = head.transform;
            menu.table = table;
            menu.theme = theme;
            menuRoot.SetActive(true);
            yield return null;
            var currentEventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (previousEventSystem == null && currentEventSystem != null) eventSystem = currentEventSystem.gameObject;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (menuRoot != null) Object.Destroy(menuRoot);
            if (manager != null) Object.Destroy(manager.gameObject);
            if (head != null) Object.Destroy(head);
            if (tableRoot != null) Object.Destroy(tableRoot);
            if (cueRoot != null) Object.Destroy(cueRoot);
            if (cueStatus != null) Object.Destroy(cueStatus);
            if (playerRoot != null) Object.Destroy(playerRoot);
            if (backingPrefab != null) Object.Destroy(backingPrefab);
            if (eventSystem != null) Object.Destroy(eventSystem);
            if (theme != null) Object.Destroy(theme);
            yield return null;
        }
    }
}
