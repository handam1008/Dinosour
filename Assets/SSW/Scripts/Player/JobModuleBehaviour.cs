using UnityEngine;

namespace SSW
{
    /// <summary>
    /// Keeps job activation in one place. Derived modules only implement their own behaviour.
    /// </summary>
    public abstract class JobModuleBehaviour : MonoBehaviour, IJobModule
    {
        PlayerIdentity _identity;
        bool _hasJobState;

        public abstract PlayerJob Job { get; }
        public bool IsJobActive { get; private set; }
        protected PlayerIdentity Identity => _identity;

        protected virtual void Awake()
        {
            _identity = GetComponentInParent<PlayerIdentity>();
        }

        protected virtual void OnEnable()
        {
            if (_identity == null)
                _identity = GetComponentInParent<PlayerIdentity>();

            if (_identity != null)
                _identity.JobChanged += HandleJobChanged;

            RefreshJobState();
        }

        protected virtual void OnDisable()
        {
            if (_identity != null)
                _identity.JobChanged -= HandleJobChanged;

            if (_hasJobState && IsJobActive)
            {
                IsJobActive = false;
                OnJobDeactivated();
            }

            _hasJobState = false;
        }

        public void SetJobActive(bool active)
        {
            if (_hasJobState && IsJobActive == active)
                return;

            _hasJobState = true;
            IsJobActive = active;
            if (active)
                OnJobActivated();
            else
                OnJobDeactivated();
        }

        protected virtual void OnJobActivated() { }
        protected virtual void OnJobDeactivated() { }

        void HandleJobChanged(PlayerJob _)
        {
            RefreshJobState();
        }

        void RefreshJobState()
        {
            SetJobActive(_identity != null && _identity.Job == Job);
        }
    }
}
