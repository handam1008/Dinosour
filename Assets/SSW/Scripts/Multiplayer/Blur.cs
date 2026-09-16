using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SSW
{
    [DefaultExecutionOrder(1000)]
    public sealed class Blur : MonoBehaviour
    {
        static readonly int Step = Shader.PropertyToID("_Step");
        [SerializeField] RawImage _image;
        [SerializeField] Material _filter;
        [SerializeField, Range(0.5f, 3f)] float _radius = 1.5f;
        readonly UniversalRenderPipeline.SingleCameraRequest _request = new UniversalRenderPipeline.SingleCameraRequest();
        Camera _view;
        Material _material;
        RenderTexture _frame;
        RenderTexture _temp;
        RenderTexture _blur;
        bool _active;

        public void Show(Camera view)
        {
            _view = view;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            int width = Mathf.Max(1, view.pixelWidth / 4);
            int height = Mathf.Max(1, view.pixelHeight / 4);
            _material = new Material(_filter);
            _frame = Create(width, height, 24);
            _temp = Create(width, height, 0);
            _blur = Create(width, height, 0);
            _request.destination = _frame;
            _image.texture = _blur;
            _active = true;
        }

        static RenderTexture Create(int width, int height, int depth)
        {
            var texture = new RenderTexture(width, height, depth, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.Create();
            return texture;
        }

        void LateUpdate()
        {
            if (!_active) return;
            RenderTexture previous = RenderTexture.active;
            RenderPipeline.SubmitRenderRequest(_view, _request);
            _material.SetVector(Step, new Vector4(_radius / _frame.width, 0f, 0f, 0f));
            Graphics.Blit(_frame, _temp, _material);
            _material.SetVector(Step, new Vector4(0f, _radius / _frame.height, 0f, 0f));
            Graphics.Blit(_temp, _blur, _material);
            RenderTexture.active = previous;
        }

        void OnDestroy()
        {
            if (!_active) return;
            _image.texture = null;
            _frame.Release();
            _temp.Release();
            _blur.Release();
            Destroy(_frame);
            Destroy(_temp);
            Destroy(_blur);
            Destroy(_material);
        }
    }
}