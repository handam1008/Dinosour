using DG.Tweening;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_Flood : MonoBehaviour
    {
        [SerializeField] private float duration;
        [SerializeField] private GameObject water;
        [SerializeField] private Transform targetTrm;

        void Start()
        {
            Flood();
        }

        void Flood()
        {
            Sequence seq = DOTween.Sequence();
         
            DOTween.Kill(seq);
            
            seq.Append(water.transform.DOMoveY(targetTrm.position.y, duration));
        }
    }
}
