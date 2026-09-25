using UnityEngine;

[DisallowMultipleComponent]
public class FreezeEffect : MonoBehaviour
{
    Rigidbody2D _body;
    float _endTime;
    float _originalGravity;

    public static void Apply(GameObject target, float duration)
    {
        Rigidbody2D body = target.GetComponentInParent<Rigidbody2D>();
        if (body == null) return;

        FreezeEffect effect = body.GetComponent<FreezeEffect>();
        if (effect == null) effect = body.gameObject.AddComponent<FreezeEffect>();

        effect.Begin(duration);
    }

    void Begin(float duration)
    {
        if (_body == null)
        {
            _body = GetComponent<Rigidbody2D>();
            _originalGravity = _body.gravityScale;
        }

        _endTime = Mathf.Max(_endTime, Time.time + duration);
        _body.gravityScale = 0f;
        _body.linearVelocity = Vector2.zero;
        _body.angularVelocity = 0f;
    }

    void FixedUpdate()
    {
        if (_body == null)
        {
            Destroy(this);
            return;
        }

        if (Time.time >= _endTime)
        {
            _body.gravityScale = _originalGravity;
            Destroy(this);
            return;
        }
        
        _body.linearVelocity = Vector2.zero;
        _body.angularVelocity = 0f;
    }

    void OnDestroy()
    {
        if (_body != null) _body.gravityScale = _originalGravity;
    }
}