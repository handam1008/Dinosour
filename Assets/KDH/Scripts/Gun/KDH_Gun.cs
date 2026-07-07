using UnityEngine;
using UnityEngine.InputSystem;

public class KDH_Gun : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    private InputAction lookAction;

    private void Awake()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();

        if (playerInput != null) lookAction = playerInput.actions["Look"];
    }

    private void Update()
    {
        FindMousePosition();
        Flip(FindMousePosition());
    }

    private void Flip(Vector3 direction)
    {
        if (direction.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(1, -1, 1);
    }

    private Vector3 FindMousePosition()
    {
        Vector2 mouseScreenPos = Vector2.zero;
        if (lookAction != null)
        {
            mouseScreenPos = lookAction.ReadValue<Vector2>();
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;

        Vector3 direction = mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);

        return direction;
    }
}
