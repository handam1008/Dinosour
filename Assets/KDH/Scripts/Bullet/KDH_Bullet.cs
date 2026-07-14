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

    private void OnEnable()
    {
        Vector3 mousePos = gun.FindMousePosition();
        mousePos.z = 0f;

        Vector3 gunPos3D = gun.gunPos.position;
        gunPos3D.z = 0f;

        _moveDir = ((Vector2)(mousePos - gunPos3D)).normalized;

        _rigid.linearVelocity = _moveDir * _speed;
    }

    private void FixedUpdate()
    {
        float angle = Mathf.Atan2(_rigid.linearVelocity.y, _rigid.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
