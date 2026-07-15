using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SSW
{
    [RequireComponent(typeof(Button))]
    public sealed class JobSlotControl : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
    {
        [SerializeField] Button _button;
        [SerializeField] Image _hatIcon;
        [SerializeField] Text _jobName;
        [SerializeField] Graphic _selectionFrame;
        [SerializeField] GameObject _equippedMarker;
        [SerializeField] Color _unselectedColor = new Color(1f, 1f, 1f, 0.22f);

        JobDefinition _definition;
        System.Action<JobSlotControl> _previewRequested;
        System.Action<JobSlotControl> _tooltipRequested;
        bool _isBound;

        public JobDefinition Definition => _definition;
        public bool IsBound => _isBound;

        void Reset()
        {
            _button = GetComponent<Button>();
        }

        public void Bind(
            JobDefinition definition,
            System.Action<JobSlotControl> previewRequested,
            System.Action<JobSlotControl> tooltipRequested)
        {
            if (_isBound)
            {
                Debug.LogError($"{name} was bound more than once.", this);
                return;
            }

            _definition = definition;
            _previewRequested = previewRequested;
            _tooltipRequested = tooltipRequested;
            _isBound = definition != null;

            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) _button.interactable = _isBound;
            if (_hatIcon != null) _hatIcon.sprite = definition != null ? definition.HatSprite : null;
            if (_jobName != null) _jobName.text = definition != null ? definition.KoreanName : string.Empty;
            SetSelected(false);
            SetEquipped(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selectionFrame == null) return;
            _selectionFrame.color = selected && _definition != null ? _definition.Accent : _unselectedColor;
        }

        public void SetEquipped(bool equipped)
        {
            if (_equippedMarker != null) _equippedMarker.SetActive(equipped);
        }

        public void SelectForNavigation()
        {
            if (_button != null && _button.interactable) _button.Select();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            RequestTooltip();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) RequestPreview();
        }

        public void OnSelect(BaseEventData eventData)
        {
            RequestPreview();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            RequestPreview();
        }

        void RequestPreview()
        {
            if (_isBound && (_button == null || _button.interactable)) _previewRequested?.Invoke(this);
        }

        void RequestTooltip()
        {
            if (_isBound && (_button == null || _button.interactable)) _tooltipRequested?.Invoke(this);
        }
    }
}
