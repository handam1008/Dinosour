using NKY.Scripts.Job;
using UnityEngine;

namespace NKY.Scripts
{
    public class PlayerJobModule : MonoBehaviour
    {
        [field: SerializeField] public PlayerJobDataSo Data {get; private set;}

        [SerializeField] private AbstractWeapon weapon;
        private void Update()
        {
            
        }
    }
}