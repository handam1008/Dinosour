using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public class AugmentDrafter : MonoBehaviour
    {
        [SerializeField] AugmentPool _commonPool;
        [SerializeField] AugmentPool _jobPool;
        [SerializeField] int _choiceCount = 3;
        [SerializeField] string _draftUIResourceName = "AugmentDraftUIDino";

        public event System.Action<Augment> OnAugmentSelected;

        readonly List<Augment> _owned = new List<Augment>();
        AugmentDraftUIBase _openDraft;
        PlayerIdentity _identity;

        public IReadOnlyList<Augment> Owned => _owned;
        bool IsMagician
        {
            get
            {
                if (_identity == null) _identity = GetComponent<PlayerIdentity>();
                return _identity != null && _identity.Job == PlayerJob.Magician;
            }
        }

        void Awake()
        {
            _identity = GetComponent<PlayerIdentity>();
        }

        void Update()
        {
            if (!IsMagician) return;
            if (_openDraft != null) return;
            if (Keyboard.current == null) return;
            if (Keyboard.current.pKey.wasPressedThisFrame) OpenCommonDraft();
            else if (Keyboard.current.oKey.wasPressedThisFrame) OpenCommonAndJobDraft();
        }

        public void OpenCommonDraft()
        {
            if (!IsMagician) return;
            OpenDraft(false);
        }

        public void OpenCommonAndJobDraft()
        {
            if (!IsMagician) return;
            OpenDraft(true);
        }

        void OpenDraft(bool includeJobReward)
        {
            if (!IsMagician) return;
            if (_openDraft != null) return;

            GameObject prefab = Resources.Load<GameObject>(_draftUIResourceName);
            if (prefab == null) return;

            Augment jobAugment = null;
            if (includeJobReward)
            {
                jobAugment = RollOne(_jobPool);
                Grant(jobAugment);
            }

            _openDraft = Instantiate(prefab).GetComponent<AugmentDraftUIBase>();
            _openDraft.Show(RollChoices(_choiceCount), HandleSelected);
            if (includeJobReward) _openDraft.ShowJobReward(jobAugment);
        }

        Augment[] RollChoices(int count)
        {
            List<Augment> candidates = Candidates(_commonPool);

            Augment[] choices = new Augment[count];
            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int index = Random.Range(0, candidates.Count);
                choices[i] = candidates[index];
                candidates.RemoveAt(index);
            }
            return choices;
        }

        Augment RollOne(AugmentPool pool)
        {
            List<Augment> candidates = Candidates(pool);
            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }

        List<Augment> Candidates(AugmentPool pool)
        {
            List<Augment> list = new List<Augment>();
            if (pool == null || pool.augments == null) return list;

            foreach (Augment augment in pool.augments)
            {
                if (augment != null && !_owned.Contains(augment)) list.Add(augment);
            }
            return list;
        }

        void Grant(Augment augment)
        {
            if (!IsMagician) return;
            if (augment == null) return;
            _owned.Add(augment);
            OnAugmentSelected?.Invoke(augment);
        }

        void HandleSelected(Augment augment)
        {
            _openDraft = null;
            Grant(augment);
        }
    }
}
