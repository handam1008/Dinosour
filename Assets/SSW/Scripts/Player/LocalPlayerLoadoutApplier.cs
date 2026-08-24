using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerIdentity))]
    public sealed class LocalPlayerLoadoutApplier : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<PlayerIdentity>().SetJob(PlayerJobStorage.Load());
        }
    }
}
