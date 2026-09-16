using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SSW
{
    public abstract class AugmentDraftUIBase : MonoBehaviour
    {
        [SerializeField] JobRewardPanelUI _jobRewardPanel;

        public bool PausesGame { get; set; } = true;

        protected JobRewardPanelUI JobRewardPanel => _jobRewardPanel;

        public abstract void Show(Augment[] choices, System.Action<Augment> onSelected);

        public void ShowJobReward(Augment augment)
        {
            if (_jobRewardPanel != null) _jobRewardPanel.Show(augment);
        }

        protected static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
