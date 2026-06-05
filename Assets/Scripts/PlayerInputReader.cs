using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    private PlayerInputActions inputActions;

    public Vector2 MoveInput { get; private set; }

    public bool JumpPressed { get; private set; }

    public bool AttackPressed { get; private set; }

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        MoveInput =
            inputActions.Player.Move.ReadValue<Vector2>();

        JumpPressed =
            inputActions.Player.Jump.WasPressedThisFrame();

        AttackPressed =
            inputActions.Player.Attack.WasPressedThisFrame();
    }
}