using UnityEngine;

public class HealDial : MonoBehaviour
{
    const float DialRadius = 0.9f;
    static readonly int ProgressId = Shader.PropertyToID("_Progress");

    [SerializeField] Renderer _renderer;

    MaterialPropertyBlock _block;
    float _duration = 1f;
    float _time;

    void Awake()
    {
        _block = new MaterialPropertyBlock();
    }

    public void Play(float duration, float radius)
    {
        _duration = duration;
        _time = 0f;
        transform.localScale = Vector3.one * (radius * 2f / DialRadius /transform.parent.lossyScale.x);
        Apply();
    }

    void Update()
    {
        _time += Time.deltaTime;
        Apply();
    }

    void Apply()
    {
        _block.SetFloat(ProgressId, Mathf.Clamp01(_time / _duration));
        _renderer.SetPropertyBlock(_block);
    }
}
