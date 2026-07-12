using UnityEngine;

namespace SSW
{
    public class PlayerIdentity : MonoBehaviour
    {
        [SerializeField] PlayerJob _job;

        public PlayerJob Job => _job;
    }
}
