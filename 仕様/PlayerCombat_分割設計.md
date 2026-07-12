# PlayerCombat 分割設計

## 概要

肥大化した `PlayerController` から攻撃処理を `PlayerCombat` として分離した。
状態と参照は `PlayerController`（コーディネーター）が一元管理し、各コンポーネントはそこを経由して動作する「司令塔＋委譲」構成をとる。

---

## 1. 分割基準

**「攻撃という行動に関わるものは Combat へ、常時必要な土台は Controller に残す」**

判断基準：**「この処理は攻撃をやめたら不要になるか？」**

| PlayerCombat へ移動 | PlayerController に残留 |
| --- | --- |
| コンボ制御 | 移動 |
| 攻撃範囲・判定 | ジャンプ |
| ジャンプ攻撃 | 接地判定 |
| 攻撃中の前進移動 | 向き（Flip / FaceEnemy） |
| コンボ追尾 | ガード |
| 攻撃SE | 状態・参照の保持 |
| 攻撃Gizmo | 地面センシング（HasGroundAhead） |

- 例：コンボ段階 → 攻撃しなければ不要 → **Combat**
- 例：接地判定 → 攻撃してなくても常に必要 → **Controller**

---

## 2. アクションの呼び出し方（3パターン）

分割後、Combat のメソッドは 3 つの経路で呼ばれる。

### パターンA：Update() からの入力ディスパッチ

攻撃ボタンの入力判定を、司令塔 `Update()` から毎フレーム Combat へ委譲する。

```csharp
// PlayerController.Update()
if (combat != null && combat.HandleAttackInput())
{
    return;  // 攻撃側が「以降の入力を打ち切るべき」と返したらUpdateを抜ける
}
```

ポイントは `HandleAttackInput()` が **bool を返す** こと。

分割前は Update 内で直接 `return;` していたが、別クラスに移すと中の `return` では Update は止まらない。
そこで「Update を抜けるべきか」を bool で返し、判断を Controller に戻す。

```csharp
// PlayerCombat.HandleAttackInput()
if (!player.IsGrounded)
{
    HandleJumpAttack();
    return true;   // 「Updateはここで打ち切って」の合図
}
// ...
return false;      // 「続けていい」の合図
```

これにより「攻撃入力があったフレームはジャンプ・アイテム入力をスキップする」という
元の早期 return 挙動が完全に保たれる。

### パターンB：Animation Event からの直接呼び出し

`Attack()` `EndAttack()` `Attack1MoveStart()` などはアニメーションクリップに埋め込まれた
Animation Event から呼ばれる。

> **Unity の Animation Event は、メソッド名で「同じ GameObject 上のどのコンポーネントか」を自動で探して呼ぶ。**

そのため `Attack()` を Controller から Combat へ移しても、両者が同じ Player オブジェクトに付いていれば
Animation 側の再設定なしで自動的に `PlayerCombat.Attack()` が呼ばれる。

```csharp
// PlayerCombat.cs（publicにしておくことが必須）
public void Attack() { /* ... */ }        // アニメの「Attack」イベントが自動で見つける
public void EndAttack() { /* ... */ }
```

対象メソッド：`Attack` / `EndAttack` / `EnableNextCombo` / `Attack1〜3MoveStart` /
`FaceComboTarget` / `JumpAttack` / `EndJumpAttack` / `PlayAttack1〜3SE` / `PlayJumpAttackSE`

### パターンC：コーディネーター経由の委譲

Combat の内部処理を他クラスから呼ぶ場合は、Controller を「窓口」にして委譲する。

例：射撃開始時にコンボ状態をリセットしたい。だがコンボ状態は Combat が持つ。
PlayerItemAction は Combat を直接知らない → Controller 経由にする。

```csharp
// PlayerItemAction.cs
player.ResetComboState();   // Controllerに頼む

// PlayerController.cs（窓口として受けて実体へ委譲）
public void ResetComboState() => combat?.ResetComboState();

// PlayerCombat.cs（実体）
public void ResetComboState() { canNextCombo = false; comboStep = 0; /* ... */ }
```

各コンポーネントは「Combat の存在」を知らずに済み、Controller だけを見ればよい構造になる。
相互依存が減る。

同じパターンが着地時にも使われる。

```csharp
// PlayerController.ResetJumpCount()（着地時）
combat?.ResetJumpAttackState();  // ジャンプ攻撃の使用済みフラグを戻す
```

---

## 3. Combat が状態を読み書きする方法

Combat は自分で状態を持たず、Controller（唯一の持ち主）から取得する。

```csharp
// PlayerCombat.cs
private PlayerController player;

private void Awake()
{
    player = GetComponent<PlayerController>();  // 同じGameObjectのControllerを掴む
}

// 使うときは全部 player. 経由
if (player.CurrentState != PlayerState.Attacking) { /* ... */ }  // 状態を読む
player.CurrentState = PlayerState.Attacking;                     // 状態を書く
player.PlayerAnimator.SetTrigger("Attack");                      // Animator参照を借りる
player.FaceEnemy(comboTarget.position);                          // 向き制御を頼む
if (!player.HasGroundAhead(direction)) yield break;              // 地面センシングを頼む
```

これは「currentState はコーディネーターに集約」という方針の実装形。
Combat が状態のコピーを持たないため「どちらが正しい状態か分からない」バグが起きない。

---

## 4. 全体の呼び出しフロー

```text
[毎フレーム]
PlayerController.Update()  ← 司令塔
    ├─ CheckGround / UpdateGuardState / Flip      … Controller自身が担当（土台）
    ├─ combat.HandleAttackInput()  ─┐             … 攻撃入力をCombatへ委譲（パターンA）
    │      └ 戻り値trueならreturn ─┘
    ├─ Jump()                                      … Controller自身が担当
    └─ itemAction.HandleShootInput()               … 射撃入力をItemActionへ委譲

[アニメ再生中の要所]
Animation Event ──自動でメソッド名検索──▶ PlayerCombat.Attack() など（パターンB）

[他クラスからの依頼]
PlayerItemAction ──▶ player.ResetComboState() ──委譲──▶ combat.ResetComboState()（パターンC）
```

---

## 5. コーディネーター公開インターフェース

Combat が利用する Controller の公開メンバ。

| メンバ | 種別 | 用途 |
| --- | --- | --- |
| `CurrentState` | get/set | 行動状態の読み書き |
| `IsGrounded` | get | 接地判定 |
| `IsFacingRight` | get | 向き |
| `IsGuarding` | get | ガード状態 |
| `Rb` | get | Rigidbody2D参照 |
| `PlayerAnimator` | get | Animator参照 |
| `Health` | get | PlayerHealth参照（IsKnockback等） |
| `FaceEnemy(pos)` | method | 指定方向へ向き直る |
| `HasGroundAhead(dir)` | method | 前方の地面有無（攻撃移動用） |
| `ResetComboState()` | method | コンボ状態リセット（Combatへ委譲） |

---

## 6. まとめ

| 観点 | 変更内容 |
| --- | --- |
| 状態の持ち主 | Controller が唯一の持ち主。Combat は `player.` 経由で読み書き |
| 入力の流れ | Update() が司令塔として各系へ委譲。攻撃は bool 戻り値で早期 return を再現 |
| Animation Event | 同じ GameObject に乗せることで、コード移動してもアニメ側は無変更で動く |
| クラス間の依存 | 各コンポーネントは Controller だけを見る。相互依存を作らない（委譲パターン） |

この「司令塔＋委譲」構成により、今後スペシャル技やパリィを追加する際も
新コンポーネントを1つ作り Update から1行呼ぶだけで組み込める。
