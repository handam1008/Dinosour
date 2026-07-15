using System;
using UnityEngine;

public class KDH_Bullet : MonoBehaviour
{
    [SerializeField] private float _speed;
    private Vector2 _moveDir;
    private Rigidbody2D _rigid;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _rigid.linearVelocity = _moveDir * _speed;
    }

    private void FixedUpdate()
    {
        float angle = Mathf.Atan2(_rigid.linearVelocity.y, _rigid.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void Init(Vector2 moveDir)
    {
        _moveDir = moveDir;
    }
}
