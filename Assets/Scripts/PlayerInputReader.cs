using UnityEngine;


/// <summary>
/// プレイヤーの入力を読み取るクラス
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    // プレイヤー入力アクション
    private PlayerInputActions inputActions;

    // 入力状態
    public Vector2 MoveInput { get; private set; }
    // ジャンプ、攻撃、射撃の入力状態
    public bool JumpPressed { get; private set; }
    // 攻撃と射撃の入力状態
    public bool AttackPressed { get; private set; }
    // 射撃の入力状態
    public bool ShootPressed { get; private set; }

    // 射撃ボタンを押している間
    public bool ShootHeld { get; private set; }

    private void Awake()
    {
        // プレイヤー入力アクションの初期化
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        // プレイヤー入力アクションの有効化
        inputActions.Enable();
    }

    // プレイヤー入力アクションの無効化
    private void OnDisable()
    {
        // プレイヤー入力アクションの無効化
        inputActions.Disable();
    }

    private void Update()
    {
        // 入力状態の更新
        MoveInput =
            inputActions.Player.Move.ReadValue<Vector2>();
        // ジャンプ、攻撃、射撃の入力状態の更新
        JumpPressed = inputActions.Player.Jump.WasPressedThisFrame();
        // 攻撃と射撃の入力状態の更新
        AttackPressed = inputActions.Player.Attack.WasPressedThisFrame();
        // 射撃の入力状態の更新
        ShootPressed = inputActions.Player.Shoot.WasPressedThisFrame();
        // 射撃ボタンを押している間の状態の更新
        ShootHeld = inputActions.Player.Shoot.IsPressed();
    }
}