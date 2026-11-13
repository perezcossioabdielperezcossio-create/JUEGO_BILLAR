using System.Collections.Generic;
using UnityEngine;
using QuestBilliards.Inputs;

namespace QuestBilliards.UI
{
    public sealed class MenuManager : MonoBehaviour
    {
        private static MenuManager instance;
        private readonly List<MenuBase> stack = new List<MenuBase>();
        private BilliardsPlayerMode playerMode;
        private GameObject cueStatus;
        private bool previousControlsLocked;
        private bool previousCueStatusActive;

        public static MenuManager GetOrCreate(BilliardsPlayerMode player)
        {
            if (instance == null)
            {
                var go = new GameObject("MenuManager");
                instance = go.AddComponent<MenuManager>();
            }
            instance.playerMode = player;
            return instance;
        }

        public void Open(MenuBase menu)
        {
            if (menu == null) return;
            if (stack.Count == 0) CaptureGameplayState();
            if (stack.Count > 0) stack[stack.Count - 1].SetOpen(false);
            stack.Remove(menu);
            stack.Add(menu);
            menu.SetOpen(true);
            LockGameplay();
        }

        public void Close(MenuBase menu)
        {
            if (menu == null) return;
            int index = stack.IndexOf(menu);
            if (index < 0) { menu.SetOpen(false); return; }
            bool wasTop = index == stack.Count - 1;
            stack.RemoveAt(index);
            menu.SetOpen(false);
            if (stack.Count == 0) RestoreGameplayState();
            else if (wasTop) stack[stack.Count - 1].SetOpen(true);
        }

        private void CaptureGameplayState()
        {
            previousControlsLocked = playerMode != null && playerMode.ControlsLocked;
            var grab = FindAnyObjectByType<QuestBilliards.CueInteraction.CueGrabController>();
            cueStatus = grab != null && grab.statusLabel != null ? grab.statusLabel.gameObject : null;
            previousCueStatusActive = cueStatus != null && cueStatus.activeSelf;
        }

        private void LockGameplay()
        {
            if (playerMode != null) playerMode.ControlsLocked = true;
            if (cueStatus != null) cueStatus.SetActive(false);
        }

        private void RestoreGameplayState()
        {
            if (playerMode != null) playerMode.ControlsLocked = previousControlsLocked;
            if (cueStatus != null) cueStatus.SetActive(previousCueStatusActive);
        }

        private void LateUpdate()
        {
            if (stack.Count > 0) LockGameplay();
        }

        private void OnDestroy()
        {
            RestoreGameplayState();
            for (int i = stack.Count - 1; i >= 0; i--) stack[i].SetOpen(false);
            stack.Clear();
            if (instance == this) instance = null;
        }
    }
}
