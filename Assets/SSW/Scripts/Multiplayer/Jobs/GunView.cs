using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    public sealed class GunView : MonoBehaviour
    {
        [System.Serializable] struct Slot
        {
            public GameObject Root;
            public Transform Fill;
        }

        [SerializeField] NetPlayer _player;
        [SerializeField] GunCast _gun;
        [SerializeField] GunFx _effects;
        [SerializeField] Transform _muzzle;
        [SerializeField] Slot[] _slots;
        [SerializeField] Canvas _quest;
        [SerializeField] Text _questText;
        float _completeUntil;
        int _progress = -1;
        public int VisibleAmmo { get; private set; }
        public int ChargedAmmo { get; private set; }

        public Vector2 Muzzle(Vector2 center, Vector2 direction, float scale)
        {
            Vector2 point = _muzzle.localPosition;
            if (direction.x < 0f) point.y = -point.y;
            return center + (direction * point.x + new Vector2(-direction.y, direction.x) * point.y) * scale;
        }

        public void Render(bool active)
        {
            if (active)
            {
                Vector2 aim = _effects.Aim;
                transform.SetPositionAndRotation(_player.View.position,
                    Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg));
                transform.localScale = new Vector3(1f, aim.x < 0f ? -1f : 1f, 1f);
            }
            Draw(active, _gun.Status, _gun.ViewTick);
            DrawQuest(active);
        }

        public void Draw(bool active, WeaponState state, uint tick)
        {
            if (active) Expand(_gun.Capacity);
            VisibleAmmo = 0;
            ChargedAmmo = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                bool show = active && i < _gun.Capacity && i < state.Ammo && i < state.Loaded.Length;
                _slots[i].Root.SetActive(show);
                if (!show) continue;
                Vector3 scale = _slots[i].Fill.localScale;
                scale.x = _gun.Charge(state.Loaded[i], tick);
                _slots[i].Fill.localScale = scale;
                VisibleAmmo++;
                if (scale.x >= 1f) ChargedAmmo++;
            }
        }

        void Expand(int capacity)
        {
            int count = _slots.Length;
            if (capacity <= count) return;
            Vector3 step = _slots[3].Root.transform.localPosition - _slots[0].Root.transform.localPosition;
            System.Array.Resize(ref _slots, capacity);
            for (int i = count; i < capacity; i++)
            {
                Slot source = _slots[i % 3];
                GameObject root = Instantiate(source.Root, source.Root.transform.parent);
                root.name = $"Ammo {i + 1}";
                root.transform.localPosition = source.Root.transform.localPosition + step * (i / 3);
                root.SetActive(false);
                _slots[i] = new Slot { Root = root, Fill = Copy(source.Root.transform, source.Fill, root.transform) };
            }
        }

        static Transform Copy(Transform source, Transform target, Transform clone)
        {
            if (target == source) return clone;
            return Copy(source, target.parent, clone).GetChild(target.GetSiblingIndex());
        }

        void DrawQuest(bool active)
        {
            int progress = _gun.Progress;
            if (progress != _progress)
            {
                _progress = progress;
                if (progress >= _gun.QuestHits) _completeUntil = Time.time + 5f;
                _questText.text = progress >= _gun.QuestHits ? "퀘스트: 진화 완료!" : $"퀘스트: 진화\n진행률: {progress} / {_gun.QuestHits}";
            }
            _quest.gameObject.SetActive(active && _player.IsOwner && _gun.Has(KDH.Scripts.Arguments.GunnerAugmentType.Quest_EvolutionAbility)
                && (progress < _gun.QuestHits || Time.time < _completeUntil));
        }
    }
}
