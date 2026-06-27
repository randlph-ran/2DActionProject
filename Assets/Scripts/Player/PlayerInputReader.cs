using UnityEngine;

/// <summary>
/// プレイヤーの入力を読み取るクラス
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    private PlayerInputActions inputActions;

    //==============================
    // プレイヤー操作入力
    //==============================

    public Vector2 MoveInput     { get; private set; }
    public bool    JumpPressed   { get; private set; }
    public bool    AttackPressed { get; private set; }
    public bool    ShootPressed  { get; private set; }
    public bool    ShootHeld     { get; private set; }
    public bool    GuardHeld     { get; private set; }

    //==============================
    // メニュー共通入力
    //==============================

    /// <summary>メニュー開閉（常に有効）</summary>
    public bool MenuPressed { get; private set; }

    //==============================
    // メニュー操作入力（IsMenuOpen 中のみ有効）
    //==============================

    /// <summary>カーソル移動（スティック・WASD・矢印）</summary>
    public Vector2 MenuNavigateInput  { get; private set; }

    /// <summary>決定（Zキー / Enter / パッドA）</summary>
    public bool    MenuConfirmPressed { get; private set; }

    /// <summary>キャンセル（Escape / パッドB）</summary>
    public bool    MenuCancelPressed  { get; private set; }

    //==============================
    // 状態フラグ
    //==============================

    /// <summary>
    /// メニューが開いている間はプレイヤー操作入力を無効化する。
    /// InventoryMenuUI から開閉に合わせてセットする。
    /// </summary>
    public bool IsMenuOpen { get; set; }

    //==============================
    // Unity イベント
    //==============================

    private void Awake()   => inputActions = new PlayerInputActions();
    private void OnEnable()  => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        // メニュー開閉は常に受け付ける
        MenuPressed = inputActions.Player.Menu.WasPressedThisFrame();

        if (IsMenuOpen)
        {
            // プレイヤー操作をすべて無効化
            MoveInput     = Vector2.zero;
            JumpPressed   = false;
            AttackPressed = false;
            ShootPressed  = false;
            ShootHeld     = false;
            GuardHeld     = false;

            // メニュー操作を読み取る
            MenuNavigateInput  = inputActions.Player.Move.ReadValue<Vector2>();
            MenuConfirmPressed = inputActions.Player.MenuConfirm.WasPressedThisFrame();
            MenuCancelPressed  = inputActions.Player.MenuCancel.WasPressedThisFrame();
            return;
        }

        // 通常プレイ時
        MoveInput     = inputActions.Player.Move.ReadValue<Vector2>();
        JumpPressed   = inputActions.Player.Jump.WasPressedThisFrame();
        AttackPressed = inputActions.Player.Attack.WasPressedThisFrame();
        ShootPressed  = inputActions.Player.Shoot.WasPressedThisFrame();
        ShootHeld     = inputActions.Player.Shoot.IsPressed();
        GuardHeld     = inputActions.Player.Guard.IsPressed();

        // メニュー操作を無効化
        MenuNavigateInput  = Vector2.zero;
        MenuConfirmPressed = false;
        MenuCancelPressed  = false;
    }
}
