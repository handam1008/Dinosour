using UnityEngine;
using UnityEngine.InputSystem;
public class GamblerMouseAim : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform playerCenter;
    [SerializeField] private SpriteRenderer gunSprite;

    [Header("총 위치")]
    [Min(0f)]
    [SerializeField] private float distanceFromPlayer = 0.8f;

    public Vector2 AimDirection { get; private set; } = Vector2.right;
    public Vector3 MouseWorldPosition { get; private set; }

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (gunSprite == null)
        {
            gunSprite = GetComponent<SpriteRenderer>();
        }

        if (playerCenter == null && transform.parent != null)
        {
            playerCenter = transform.parent;
        }
    }

    private void Update()
    {
        TryRefreshAim();
    }

    public bool TryGetFireDirection(out Vector2 direction)
    {
        direction = AimDirection;

        if (!TryRefreshAim())
        {
            return false;
        }

        Vector2 gunToMouse = MouseWorldPosition - transform.position;

        if (gunToMouse.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        direction = gunToMouse.normalized;
        return true;
    }

    private bool TryRefreshAim()
    {
        if (playerCenter == null)
        {
            Debug.LogError("GamblerMouseAim needs a Player Center reference.", this);

            return false;
        }

        if (!TryReadMouseWorldPosition(out Vector3 mouseWorldPosition))
        {
            return false;
        }

        MouseWorldPosition = mouseWorldPosition;

        Vector2 playerToMouse = mouseWorldPosition - playerCenter.position;

        if (playerToMouse.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        AimDirection = playerToMouse.normalized;

        Vector3 gunPosition = playerCenter.position + (Vector3)(AimDirection * distanceFromPlayer);

        gunPosition.z = transform.position.z;
        transform.position = gunPosition;

        float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (gunSprite != null)
        {
            gunSprite.flipY = AimDirection.x < 0f;
        }

        return true;
    }

    private bool TryReadMouseWorldPosition(out Vector3 mouseWorldPosition)
    {
        mouseWorldPosition = default;

        if (worldCamera == null)
        {
            Debug.LogError(
                "GamblerMouseAim needs a Camera reference.",this);

            return false;
        }

        if (Mouse.current == null)
        {
            return false;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();

        mouseScreenPosition.z = Mathf.Abs(worldCamera.transform.position.z - transform.position.z);

        mouseWorldPosition = worldCamera.ScreenToWorldPoint(mouseScreenPosition);

        mouseWorldPosition.z = transform.position.z;

        return true;
    }
}
