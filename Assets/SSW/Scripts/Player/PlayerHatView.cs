using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class PlayerHatView : MonoBehaviour
    {
        [SerializeField] PlayerIdentity _identity;
        [SerializeField] JobCatalog _catalog;
        [SerializeField] SpriteRenderer _renderer;

        void OnEnable()
        {
            Apply(_identity != null ? _identity.Job : PlayerJob.None);

            if (_identity != null)
                _identity.JobChanged += Apply;
        }

        void OnDisable()
        {
            if (_identity != null)
                _identity.JobChanged -= Apply;
        }

        void Apply(PlayerJob job)
        {
            if (_renderer == null)
                return;

            if (job == PlayerJob.None
                || _catalog == null
                || !_catalog.TryGet(job, out JobDefinition definition)
                || definition.HatSprite == null)
            {
                Hide();
                return;
            }

            _renderer.sprite = definition.HatSprite;
            _renderer.enabled = true;

            Transform rendererTransform = _renderer.transform;
            Vector3 localPosition = rendererTransform.localPosition;
            localPosition.x = definition.WorldOffset.x;
            localPosition.y = definition.WorldOffset.y;
            rendererTransform.localPosition = localPosition;
            rendererTransform.localScale = new Vector3(
                definition.WorldScale,
                definition.WorldScale,
                1f);
        }

        void Hide()
        {
            _renderer.sprite = null;
            _renderer.enabled = false;
        }
    }
}
