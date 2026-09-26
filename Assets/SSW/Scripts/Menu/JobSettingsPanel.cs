using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SSW
{
    public sealed class JobSettingsPanel : MonoBehaviour, ICancelHandler
    {
        const int RequiredSlotCount = 6;

        [SerializeField] JobCatalog _catalog;
        [SerializeField] JobSlotControl[] _slots = new JobSlotControl[RequiredSlotCount];
        [SerializeField] Text _header;
        [SerializeField] Text _tooltip;
        [SerializeField] MenuDinosaurPreview _preview;
        [SerializeField] Button _equipButton;
        [SerializeField] Button _backButton;

        JobSlotControl _selectedSlot;
        PlayerJob _equippedJob;
        bool _initialized;

        public event System.Action BackRequested;
        public event System.Action<PlayerJob> Equipped;

        public PlayerJob SelectedJob => _selectedSlot != null && _selectedSlot.Definition != null
            ? _selectedSlot.Definition.Job
            : PlayerJob.None;
        public PlayerJob EquippedJob => _equippedJob;

        void Awake()
        {
            Initialize();
            if (_equipButton != null) _equipButton.onClick.AddListener(Equip);
            if (_backButton != null) _backButton.onClick.AddListener(Back);
        }

        void OnEnable()
        {
            Open();
        }

        void OnDestroy()
        {
            if (_equipButton != null) _equipButton.onClick.RemoveListener(Equip);
            if (_backButton != null) _backButton.onClick.RemoveListener(Back);
        }

        public void Open()
        {
            if (!Initialize()) return;

            _equippedJob = PlayerJobStorage.Load();
            UpdateEquippedMarkers();

            JobSlotControl equippedSlot = FindSlot(_equippedJob);
            SelectSlot(equippedSlot != null ? equippedSlot : FirstBoundSlot());
            _selectedSlot?.SelectForNavigation();
        }

        public void Equip()
        {
            if (_selectedSlot == null || _selectedSlot.Definition == null) return;

            _equippedJob = _selectedSlot.Definition.Job;
            PlayerJobStorage.Save(_equippedJob);
            UpdateEquippedMarkers();
            UpdateTooltip(_selectedSlot.Definition);
            Equipped?.Invoke(_equippedJob);
        }

        public void Back()
        {
            RestoreStoredPreview();
            BackRequested?.Invoke();
        }

        public void OnCancel(BaseEventData eventData)
        {
            Back();
        }

        public void RestoreStoredPreview()
        {
            if (!Initialize()) return;

            _equippedJob = PlayerJobStorage.Load();
            UpdateEquippedMarkers();
            JobSlotControl storedSlot = FindSlot(_equippedJob);
            SelectSlot(storedSlot != null ? storedSlot : FirstBoundSlot());
        }

        bool Initialize()
        {
            if (_initialized) return true;
            if (_catalog == null)
            {
                Debug.LogError("Job Settings requires a JobCatalog.", this);
                return false;
            }

            if (_slots == null || _slots.Length != RequiredSlotCount
                || _catalog.Definitions == null || _catalog.Definitions.Count != RequiredSlotCount)
            {
                Debug.LogError($"Job Settings requires exactly {RequiredSlotCount} slots and catalog entries.", this);
                return false;
            }

            HashSet<JobSlotControl> uniqueSlots = new HashSet<JobSlotControl>();
            HashSet<PlayerJob> uniqueJobs = new HashSet<PlayerJob>();
            for (int i = 0; i < RequiredSlotCount; i++)
            {
                JobSlotControl slot = _slots[i];
                JobDefinition definition = _catalog.Definitions[i];
                if (slot == null || definition == null || slot.IsBound
                    || !uniqueSlots.Add(slot)
                    || !PlayerJobStorage.IsSelectable(definition.Job)
                    || !uniqueJobs.Add(definition.Job))
                {
                    Debug.LogError($"Job Settings slot {i} is missing, duplicated, invalid, or already bound.", this);
                    return false;
                }
            }

            for (int i = 0; i < RequiredSlotCount; i++)
            {
                JobSlotControl slot = _slots[i];
                JobDefinition definition = _catalog.Definitions[i];
                slot.Bind(definition, HandlePreviewRequested, HandleTooltipRequested);
            }

            _initialized = true;
            return true;
        }

        void HandlePreviewRequested(JobSlotControl slot)
        {
            SelectSlot(slot);
        }

        void HandleTooltipRequested(JobSlotControl slot)
        {
            if (slot == null || slot.Definition == null) return;
            if (_tooltip != null) _tooltip.text = slot.Definition.KoreanName;
        }

        void SelectSlot(JobSlotControl slot)
        {
            if (slot == null || slot.Definition == null) return;

            if (_selectedSlot != null) _selectedSlot.SetSelected(false);
            _selectedSlot = slot;
            _selectedSlot.SetSelected(true);
            _preview?.Show(_selectedSlot.Definition);
            UpdateSelectedCopy(_selectedSlot.Definition);
        }

        void UpdateSelectedCopy(JobDefinition definition)
        {
            if (_header != null) _header.text = definition.KoreanName;
            UpdateTooltip(definition);
        }

        void UpdateTooltip(JobDefinition definition)
        {
            if (_tooltip != null)
            {
                _tooltip.text = definition.Job == _equippedJob
                    ? $"{definition.KoreanName} · 장착 중"
                    : $"{definition.KoreanName} · 미리보기";
            }
        }

        void UpdateEquippedMarkers()
        {
            foreach (JobSlotControl slot in _slots)
            {
                if (slot != null && slot.Definition != null) slot.SetEquipped(slot.Definition.Job == _equippedJob);
            }
        }

        JobSlotControl FindSlot(PlayerJob job)
        {
            foreach (JobSlotControl slot in _slots)
            {
                if (slot != null && slot.Definition != null && slot.Definition.Job == job) return slot;
            }

            return null;
        }

        JobSlotControl FirstBoundSlot()
        {
            foreach (JobSlotControl slot in _slots)
            {
                if (slot != null && slot.IsBound) return slot;
            }

            return null;
        }
    }
}
