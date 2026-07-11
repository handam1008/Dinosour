using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public class AugmentDrafter : MonoBehaviour
    {
        [SerializeField] Augment[] _commonAugments;
        [SerializeField] Augment[] _jobAugments;
        [SerializeField] int _choiceCount = 3;
        [SerializeField] string _draftUIResourceName = "AugmentDraftUI";

        public event System.Action<Augment> OnAugmentSelected;

        readonly List<Augment> _owned = new List<Augment>();
        AugmentDraftUIBase _openDraft;

        public IReadOnlyList<Augment> Owned => _owned;

        void Update()
        {
            if (_openDraft != null) return;
            if (Keyboard.current == null || !Keyboard.current.pKey.wasPressedThisFrame) return;
            OpenDraft();
        }

        public void OpenDraft()
        {
            if (_openDraft != null) return;

            GameObject prefab = Resources.Load<GameObject>(_draftUIResourceName);
            if (prefab == null) return;

            _openDraft = Instantiate(prefab).GetComponent<AugmentDraftUIBase>();
            _openDraft.Show(RollChoices(), HandleSelected);
        }

        Augment[] RollChoices()
        {
            List<Augment> pool = new List<Augment>();
            if (_commonAugments != null) pool.AddRange(_commonAugments);
            if (_jobAugments != null) pool.AddRange(_jobAugments);
            pool.RemoveAll(a => a == null || _owned.Contains(a));

            Augment[] choices = new Augment[_choiceCount];
            for (int i = 0; i < _choiceCount && pool.Count > 0; i++)
            {
                int index = Random.Range(0, pool.Count);
                choices[i] = pool[index];
                pool.RemoveAt(index);
            }
            return choices;
        }

        void HandleSelected(Augment augment)
        {
            _openDraft = null;
            if (augment != null) _owned.Add(augment);
            OnAugmentSelected?.Invoke(augment);
        }
    }
}
