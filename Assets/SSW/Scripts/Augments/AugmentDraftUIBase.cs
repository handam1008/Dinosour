using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SSW
{
    public abstract class AugmentDraftUIBase : MonoBehaviour
    {
        public abstract void Show(Augment[] choices, System.Action<Augment> onSelected);

        protected static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
