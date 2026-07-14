using System;
using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    public class PlayerIdentity : MonoBehaviour
    {
        [SerializeField] PlayerJob _job;

        public PlayerJob Job => _job;

        public event Action<PlayerJob> JobChanged;

        public void SetJob(PlayerJob job)
        {
            if (_job == job)
                return;

            _job = job;
            JobChanged?.Invoke(_job);
        }
    }
}
