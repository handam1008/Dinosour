using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KDH.Scripts.Objects
{
    public class KDH_TimeLineEffect : MonoBehaviour
    {
        private Volume _volume;
        private ColorAdjustments _cameraColorEffect;
        private Camera _camera;
        private float _originalSize;
        private float _targetSize;
        private bool _initialized;

        [SerializeField] private float cameraFilterValue;
        
        private void Awake()
        {
            _camera = Camera.main;
            _volume = gameObject.transform.root.GetComponentInChildren<Volume>();
            
            if (_volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
                _cameraColorEffect = Instantiate(colorAdjustments);
            }
        }

        public void StartEffect(float t)
        {
            StartCameraSizeChange(t);
            StartVolumeChange();

        }

        public void StopEffect()
        {
            StopCameraSizeChange();
            StopVolumeChange();
        }

        private void StartCameraSizeChange(float t)
        {
            if (!_initialized)
            {
                _originalSize = _camera.orthographicSize;
                _targetSize = _originalSize * 0.9f;
                _initialized = true;
            }

            _camera.orthographicSize = Mathf.Lerp(_originalSize, _targetSize, t);
        }
        
        private void StopCameraSizeChange()
        {
            _camera.orthographicSize = _originalSize;
        }

        private void StartVolumeChange()
        {
            _cameraColorEffect.saturation.value = cameraFilterValue;
        }
        
        private void StopVolumeChange()
        {
            _cameraColorEffect.saturation.value = 0;
        }
    }
}