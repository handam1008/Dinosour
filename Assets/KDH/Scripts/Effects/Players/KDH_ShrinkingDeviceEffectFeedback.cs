using KDH.Scripts.Gun;
using KDH.Scripts.System.FeedbackSystem;
using UnityEngine;

namespace KDH.Scripts.Effects.Players
{
    public class KDH_ShrinkingDeviceEffectFeedback : KDH_AbstractFeedback
    {
        [SerializeField] private GameObject effectPrefab;
        private KDH_ShrinkingDevice _parent;
        private KDH_Gun _gun;
        private Transform _target;
        
        private void Awake()
        {
            _gun = transform.root.gameObject.GetComponent<KDH_Gun>();
            _parent = GetComponentInParent<KDH_ShrinkingDevice>();
            _target = _gun.transform;
        }

        public override void CreateFeedBack()
        {
            GameObject entity = Instantiate(effectPrefab, _target.position, Quaternion.identity);
            
            Destroy(entity, _parent.Duration + 0.5f);
        }

        public override void StopFeedBack()
        {
        
        }
    }
}
