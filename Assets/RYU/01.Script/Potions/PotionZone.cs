using System.Collections.Generic;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    // 잔류형 포션: 터진 자리에 남아서 일정 시간 동안 같은 효과를 반복 적용한다.
    public class PotionZone : MonoBehaviour
    {
        [SerializeField] private float duration = 3f;      // 장판 유지 시간
        [SerializeField] private float tickInterval = 0.5f; // 몇 초마다 적용할지
        [SerializeField] private float powerPerTick = 0.35f; // 틱당 위력 (밸런싱용)

        private AbstractPotion _data;
        private PotionModifiers _mods;
        private Component _owner;
        private float _radius;

        private float _life;
        private float _tick;

        public void Init(AbstractPotion data, PotionModifiers mods, Component owner, float radius)
        {
            _data = data;
            _owner = owner;
            _radius = radius;

            // 위력을 낮추고, 장판이 또 장판을 만들지 않게 한다
            _mods = mods.ForZone(powerPerTick);

            ApplyVisual(data.potionColor);
        }

        // 파티클 색을 포션 색으로 맞추고, 뿌려지는 범위를 실제 장판 크기에 맞춘다
        private void ApplyVisual(Color color)
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in systems)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = color;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.radius = _radius;
            }
        }

        private void Update()
        {
            _life += Time.deltaTime;
            if (_life >= duration)
            {
                Destroy(gameObject);
                return;
            }

            _tick += Time.deltaTime;
            if (_tick < tickInterval) return;
            _tick = 0f;
            ApplyOnce();
        }

        private void ApplyOnce()
        {
            if (_data == null) return;

            // 한 번의 틱에서 같은 대상에 중복 적용되지 않게 루트 기준으로 거른다
            HashSet<Transform> applied = new HashSet<Transform>();
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);

            foreach (Collider2D hit in hits)
            {
                if (!applied.Add(hit.transform.root)) continue;
                _data.Use(hit.gameObject, _owner, _mods);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
