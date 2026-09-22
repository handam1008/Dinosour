using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_Godzilla : MonoBehaviour
    {
        [SerializeField] private Transform target1, target2;
        [SerializeField] private Color signalColor1, signalColor2;
        [SerializeField] private Color baseColor;
        
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private KDH_BreatheDamageCaster damageCaster;
        [SerializeField] private float relationTime;

        private SpriteRenderer _targetSp1, _targetSp2;

        private void Awake()
        {
            _targetSp1 = target1.GetComponent<SpriteRenderer>();
            _targetSp2 = target2.GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            damageCaster.canDamage = false;

            StartCoroutine(StartBreate());
        }

        private IEnumerator StartBreate()
        {
            while (true)
            {
                yield return new WaitForSeconds(4);
                ChangeColor();
                
                yield return new WaitForSeconds(relationTime - 4);

                particles.Play();
                damageCaster.canDamage = true;

                yield return new WaitForSeconds(particles.main.duration);
                _targetSp2.DOColor(signalColor2, 1);
                damageCaster.canDamage = false;                  
            }
        }

        private void ChangeColor()
        {
            _targetSp1.color = signalColor1;
            _targetSp1.DOColor(baseColor, 1);

            _targetSp2.color = signalColor2;
            _targetSp2.DOColor(signalColor1, 1);
        }
    }
}
