using UnityEngine;

namespace QuestBilliards.UI
{
    public abstract class MenuBase : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        protected MenuManager Manager { get; private set; }

        public void Open(MenuManager manager)
        {
            if (manager == null) return;
            Manager = manager;
            manager.Open(this);
        }

        public void Close()
        {
            if (Manager != null) Manager.Close(this);
            else SetOpen(false);
        }

        internal void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            OnMenuVisibilityChanged(open);
        }

        protected virtual void OnMenuVisibilityChanged(bool visible) { }

        protected virtual void OnDestroy()
        {
            if (Manager != null) Manager.Close(this);
        }
    }
}
