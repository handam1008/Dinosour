using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace KDH.Scripts.Effects
{
    public class KDH_IceEffect : AbstractFeedBack
    {
        [SerializeField] private GameObject iceEffect;
        
        public override void CreateFeedBack(Vector3 pos)
        {
            Instantiate(iceEffect, pos, Quaternion.identity);
            
            Destroy(iceEffect, 1.5f);
        }

        public override void StopFeedBack()
        {
            throw new NotImplementedException();
        }
    }
}