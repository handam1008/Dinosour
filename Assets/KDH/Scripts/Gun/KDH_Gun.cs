using UnityEngine;
using UnityEngine.InputSystem;

public class KDH_Gun : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Transform _visual;
    public Transform gunPos;
    Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        FindMousePosition();
        FollowMouse();
    }

    private void FollowMouse()
    {
        Vector3 mouseWorldPos = FindMousePosition();
        Vector3 dir = (mouseWorldPos - transform.position).normalized;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        if (dir.x < 0)
        {
            _visual.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            _visual.localScale = new Vector3(1, 1, 1);
        }
    }

    public Vector3 FindMousePosition()
    {
        Vector3 world = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        Vector3 scale = _visual.localScale;
        scale.x = world.x < transform.position.x ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        _visual.localScale = scale;

        return world;
    }

}
