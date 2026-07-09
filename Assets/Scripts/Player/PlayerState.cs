/// <summary>
/// Playerの行動状態。
/// Attacking / Shooting / JumpAttacking は互いに排他で、
/// 攻撃・射撃・ジャンプ攻撃が同時に発動しないようにするための単一の状態管理として使う。
/// PlayerController がこの状態の唯一の持ち主となり、各コンポーネントはそこを参照する。
/// </summary>
public enum PlayerState
{
    Idle,
    Attacking,
    Shooting,
    JumpAttacking
}
