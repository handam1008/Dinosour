using UnityEngine;
using UnityEngine.InputSystem;

public class KDH_Gun : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    public Transform gunPos;
    private InputAction lookAction;

    private void Awake()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();

        if (playerInput != null) lookAction = playerInput.actions["Look"];
    }

    private void Update()
    {
        FindMousePosition();
    }

    public Vector3 FindMousePosition()
    {
        Vector3 world = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3 scale = _visual.localScale;
        scale.x = world.x < transform.position.x ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        _visual.localScale = scale;

        return direction;
    }
}
