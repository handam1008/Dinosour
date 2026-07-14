using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    public sealed class MenuDinosaurPreview : MonoBehaviour
    {
        [SerializeField] RectTransform _dinosaurTransform;
        [SerializeField] Image _dinosaurImage;
        [SerializeField] Image _hatImage;
        [SerializeField] Sprite[] _idleFrames = new Sprite[4];
        [SerializeField, Min(0.01f)] float _frameInterval = 0.12f;
        [SerializeField, Min(0f)] float _bobHeight = 3f;
        [SerializeField, Min(0.01f)] float _bobPeriod = 1.4f;

        float _frameTimer;
        float _bobTimer;
        int _frameIndex;
        Vector2 _restPosition;
        bool _hasRestPosition;

        public int FrameIndex => _frameIndex;
        public RectTransform DinosaurTransform => _dinosaurTransform;
        public Image HatImage => _hatImage;

        void Awake()
        {
            EnsureHierarchy();
            ApplyCurrentFrame();
        }

        void OnEnable()
        {
            Canvas.ForceUpdateCanvases();
            CaptureRestPosition();
            _frameTimer = 0f;
            _bobTimer = 0f;
            _frameIndex = 0;
            ApplyCurrentFrame();
        }

        void OnDisable()
        {
            if (_dinosaurTransform != null && _hasRestPosition) _dinosaurTransform.anchoredPosition = _restPosition;
            _hasRestPosition = false;
        }

        void Update()
        {
            AnimateFrames(Time.unscaledDeltaTime);
            AnimateBob(Time.unscaledDeltaTime);
        }

        public void Show(JobDefinition definition)
        {
            EnsureHierarchy();
            if (_hatImage == null) return;

            bool visible = definition != null && definition.HatSprite != null;
            _hatImage.enabled = visible;
            _hatImage.sprite = visible ? definition.HatSprite : null;
            if (!visible) return;

            RectTransform hatTransform = _hatImage.rectTransform;
            hatTransform.anchoredPosition = definition.PreviewOffset;
            hatTransform.localScale = Vector3.one * definition.PreviewScale;
        }

        public void SetFacing(bool facesRight)
        {
            if (_dinosaurTransform == null) return;
            Vector3 scale = _dinosaurTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * (facesRight ? 1f : -1f);
            _dinosaurTransform.localScale = scale;
        }

        void AnimateFrames(float unscaledDeltaTime)
        {
            if (_dinosaurImage == null || _idleFrames == null || _idleFrames.Length == 0) return;

            _frameTimer += unscaledDeltaTime;
            while (_frameTimer >= _frameInterval)
            {
                _frameTimer -= _frameInterval;
                _frameIndex = (_frameIndex + 1) % _idleFrames.Length;
                ApplyCurrentFrame();
            }
        }

        void AnimateBob(float unscaledDeltaTime)
        {
            if (_dinosaurTransform == null || !_hasRestPosition) return;
            _bobTimer = (_bobTimer + unscaledDeltaTime) % _bobPeriod;
            float offset = Mathf.Sin(_bobTimer / _bobPeriod * Mathf.PI * 2f) * _bobHeight;
            _dinosaurTransform.anchoredPosition = _restPosition + Vector2.up * offset;
        }

        void ApplyCurrentFrame()
        {
            if (_dinosaurImage == null || _idleFrames == null || _idleFrames.Length == 0) return;
            _frameIndex %= _idleFrames.Length;
            _dinosaurImage.sprite = _idleFrames[_frameIndex];
        }

        void EnsureHierarchy()
        {
            if (_dinosaurTransform == null || _hatImage == null) return;
            if (_hatImage.transform.parent != _dinosaurTransform) _hatImage.transform.SetParent(_dinosaurTransform, false);
        }

        void CaptureRestPosition()
        {
            if (_dinosaurTransform == null) return;
            _restPosition = _dinosaurTransform.anchoredPosition;
            _hasRestPosition = true;
        }
    }
}
