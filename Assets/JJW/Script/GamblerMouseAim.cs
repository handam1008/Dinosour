using UnityEngine;
using UnityEngine.InputSystem;
public class GamblerMouseAim : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private SpriteRenderer weaponSprite;

    public Vector2 AimDirection { get; private set; } = Vector2.right;
    public Vector3 MouseWorldPosition { get; private set; }

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (weaponPivot == null)
        {
            weaponPivot = transform;
        }
    }

    private void Update()
    {
        if (!TryReadMouseWorldPosition(out Vector3 mouseWorldPosition))
        {
            return;
        }

        MouseWorldPosition = mouseWorldPosition;

        Vector2 pivotToMouse = mouseWorldPosition - weaponPivot.position;
        if (pivotToMouse.sqrMagnitude < 0.0001f)
        {
            return;
        }

        AimDirection = pivotToMouse.normalized;

        float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        // The weapon sprite is expected to face right at rotation Z = 0.
        if (weaponSprite != null)
        {
            weaponSprite.flipY = AimDirection.x < 0f;
        }
    }

    public bool TryGetDirectionFrom(Vector3 origin, out Vector2 direction)
    {
        direction = AimDirection;

        // Read the mouse again at the exact moment of firing.
        // This prevents a one-frame-old aim direction from being used.
        if (!TryReadMouseWorldPosition(out Vector3 mouseWorldPosition))
        {
            return false;
        }

        MouseWorldPosition = mouseWorldPosition;
        Vector2 originToMouse = mouseWorldPosition - origin;

        if (originToMouse.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        direction = originToMouse.normalized;
        return true;
    }

    private bool TryReadMouseWorldPosition(out Vector3 mouseWorldPosition)
    {
        mouseWorldPosition = default;

        if (worldCamera == null)
        {
            Debug.LogError("GamblerMouseAim needs a Camera reference.", this);
            return false;
        }

        if (Mouse.current == null)
        {
            return false;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();

        // ScreenToWorldPoint uses Z as distance from the camera.
        // In this 2D setup, the target plane is the weapon pivot's Z plane.
        mouseScreenPosition.z = Mathf.Abs(
            worldCamera.transform.position.z - weaponPivot.position.z);

        mouseWorldPosition = worldCamera.ScreenToWorldPoint(mouseScreenPosition);
        mouseWorldPosition.z = weaponPivot.position.z;
        return true;
    }
}
