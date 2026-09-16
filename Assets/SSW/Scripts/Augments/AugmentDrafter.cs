using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerIdentity))]
    public class AugmentDrafter : MonoBehaviour, IAugmentSource
    {
        [SerializeField] AugmentPool _commonPool;
        [SerializeField] AugmentPool _jobPool;
        [SerializeField] int _choiceCount = 3;
        [SerializeField] string _draftUIResourceName = "AugmentDraftUIDino";

        public event System.Action<Augment> OnAugmentSelected;
        public event System.Action<Augment> AugmentGranted;

        readonly List<Augment> _owned = new List<Augment>();
        AugmentDraftUIBase _openDraft;
        PlayerIdentity _identity;
        bool _networked;

        public void BindNetwork() => _networked = true;

        public IReadOnlyList<Augment> Owned => _owned;

        void Awake()
        {
            _identity = GetComponent<PlayerIdentity>();
        }

        void Update()
        {
            if (_networked) return;
            if (_openDraft != null) return;
            if (Keyboard.current == null) return;
            if (Keyboard.current.pKey.wasPressedThisFrame) OpenCommonDraft();
            else if (Keyboard.current.oKey.wasPressedThisFrame) OpenCommonAndJobDraft();
        }

        public void OpenCommonDraft()
        {
            OpenDraft(false);
        }

        public void OpenCommonAndJobDraft()
        {
            OpenDraft(true);
        }

        public void SetJobPool(AugmentPool pool)
        {
            _jobPool = pool;
        }

        void OpenDraft(bool includeJobReward)
        {
            if (_networked) return;
            if (_openDraft != null) return;

            GameObject prefab = Resources.Load<GameObject>(_draftUIResourceName);
            if (prefab == null) return;

            Augment jobAugment = null;
            if (includeJobReward)
            {
                jobAugment = RollOne(_jobPool);
                TryGrant(jobAugment);
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
                if (augment != null && !_owned.Contains(augment) && IsEligible(augment))
                    list.Add(augment);
            }
            return list;
        }

        public bool TryGrant(Augment augment)
        {
            return !_networked && ApplyGrant(augment);
        }

        internal bool ApplyGrant(Augment augment)
        {
            if (augment == null || _owned.Contains(augment) || !IsEligible(augment))
                return false;

            _owned.Add(augment);
            AugmentGranted?.Invoke(augment);
            OnAugmentSelected?.Invoke(augment);
            return true;
        }

        void HandleSelected(Augment augment)
        {
            _openDraft = null;
            TryGrant(augment);
        }

        bool IsEligible(Augment augment)
        {
            if (augment is not IJobRestrictedAugment restricted)
                return true;
            
            return _identity != null && _identity.Job == restricted.RequiredJob;
        }
    }
}
