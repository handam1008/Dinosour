using UnityEngine;

public class KDH_Bullet : MonoBehaviour
{
    private KDH_Gun gun;
    [SerializeField] private float _speed;
    private Vector2 _moveDir;
    private Rigidbody2D _rigid;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
        gun = KDH_GameManager.instanec.player.GetComponentInChildren<KDH_Gun>();
    }

    private void FixedUpdate()
    {
        _moveDir = (Vector2)gun.FindMousePosition();
        _rigid.linearVelocity = _moveDir * _speed;
    }
}
