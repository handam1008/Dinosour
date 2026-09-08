using DG.Tweening;
using UnityEngine;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_LightningFlag : MonoBehaviour
    {
        [SerializeField] private GameObject[] flags;
        [SerializeField] private Vector2 animationSize = new Vector2(0, 0.25f);
        [SerializeField] private float duration = 0.6f;
        [SerializeField] private float staggerDelay = 0.08f;

        private Transform[] _startTrm;
        private GameObject[] _entities;
        private Tweener[] _tweeners;
        private bool _initialized;

        public void Init(GameObject entity, int hitCount)
        {
            _entities = new GameObject[flags.Length];
            _startTrm = new Transform[flags.Length];
            _tweeners = new Tweener[flags.Length];

            for (int i = 0; i < flags.Length; i++)
            {
                _entities[i] = entity.transform.GetChild(0).transform.GetChild(i).gameObject;
                _entities[i].SetActive(false);
                _startTrm[i] = _entities[i].transform;
            }

            for (int i = 0; i < hitCount; i++)
            {
                _entities[i].SetActive(true);
            }

            _initialized = true;
            Animation();
        }

        private void Animation()
        {
            for (int i = 0; i < flags.Length; i++)
            {
                Transform t = _entities[i].transform;

                _tweeners[i] = t.DOLocalMoveY(animationSize.y, duration)
                    .SetRelative(true)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(i * staggerDelay);
            }
        }

        private void OnDisable()
        {
            if (!_initialized) return;

            for (int i = 0; i < _entities.Length; i++)
            {
                if (_entities[i] == null) continue;

                _tweeners[i]?.Kill();
                _entities[i].transform.position = _startTrm[i].position;
            }
        }
    }
}