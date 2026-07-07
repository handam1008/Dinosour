using UnityEngine;

public class KDH_Bullet : MonoBehaviour
{
    [SerializeField] private float _speed;
    private Vector2 _moveDir;
    private Rigidbody2D _rigid;
    
    private void FixedUpdate()
    {
        _rigid.linearVelocity = _moveDir * _speed;
    }
}
