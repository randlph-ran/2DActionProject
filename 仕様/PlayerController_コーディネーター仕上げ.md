# PlayerController コーディネーター仕上げ ＋ ノックバック改修

PlayerControllerのリファクタリング最終段（コーディネーター仕上げ）と、そこから派生したノックバックのState化までの修正内容をまとめる。

---

## 1. コーディネーターの仕上げ（整理）

移動系は分離せず、PlayerControllerが「体（移動・状態の土台）」を担当し、各コンポーネントが「能力（攻撃・アイテム・エフェクト）」を担当する構成のまま整理した。

### 変更点

| 項目 | 内容 |
| --- | --- |
| 未使用フィールド削除 | `slashEffectPrefab`（どこからも参照されていなかった） |
| 未使用using削除 | `using System.Collections;`（コルーチンは各コンポーネントへ移動済みだったため。※後述のノックバックで再追加） |
| フィールド整理 | 「参照 / 調整値(Inspector) / 実行時状態 / 公開インターフェース」の4ブロックに再編（シリアライズ名は不変のためInspector再設定は不要） |
| Update()分割 | まとまった処理を名前付きメソッドへ抽出し、Update()本体を司令塔として読みやすくした |
| FixedUpdate整理 | 横移動停止判定を `ShouldStopHorizontalMove()` に切り出し |

### Update()から抽出したメソッド

| メソッド | 役割 |
| --- | --- |
| `UpdateAnimatorParams(isRunning)` | isGuarding / isRunning / isGrounded / verticalSpeed をAnimatorへ反映 |
| `UpdateFootstep(isRunning)` | 足音の再生/停止判定 |
| `HandleJumpInput()` | ジャンプ入力の受付 |

### 挙動保全の注意点（FixedUpdate）

`ShouldStopHorizontalMove()` に **ノックバックを含めてはいけない**。
ノックバック中は「横速度をゼロにする」のではなく「横速度を**上書きしない**（勢いを保つ）」必要があるため、FixedUpdate側で個別に早期returnしている。

```csharp
// ノックバック中は横速度を上書きしない（勢いを保つため、ここでは何もしない）
if (playerHealth.IsKnockback)
{
    return;
}
// 攻撃/アイテム中・ガード中・Weight負け中だけ横速度をゼロにする
if (ShouldStopHorizontalMove())
{
    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    return;
}
```

---

## 2. ノックバックのState化

「ダメージを受けたら、Playerの行動による移動を全て無効化し、ノックバックを優先する」を実現するため、
ノックバックを **PlayerStateの正式な状態** として扱う方式に変更した。

### 設計のキモ

`IsKnockback` を **状態判定のプロパティ**に置き換えたことで、既存の全ゲート（移動・ジャンプ・攻撃前進・攻撃発生）が自動でState基準になり、追加フラグが不要になった。

```csharp
// PlayerHealth
// 状態の実体はPlayerControllerのCurrentState(Knockback)。ここはその判定を公開するだけ
public bool IsKnockback => playerController != null
    && playerController.CurrentState == PlayerState.Knockback;
```

### 変更点（コード）

**PlayerState.cs**
- `Knockback` を追加

**PlayerController.cs**
- `EnterKnockback()` … 状態をKnockbackに、コンボ中断、`Knockback`トリガー送信、セーフティ監視開始
- `EndKnockback()` … [Animation Event用] 状態をIdleへ戻す
- `KnockbackSafetyTimeout()` … Animation Eventが来ない場合の強制解除（`knockbackSafetyTime`秒）
- 走りアニメ判定に `&& currentState != Knockback` を追加（ノックバック中に走りアニメが混入するのを防止）

**PlayerHealth.cs**
- `IsKnockback` を状態判定プロパティ化（前述）
- `KnockbackCoroutine` → `KnockbackVelocityCoroutine`（**速度だけ**担当。状態管理はPlayerControllerへ分離）
- `TakeDamage` を整理：死亡チェックを先に（死亡時はKnockback状態にせず死亡演出へ）→ ガード時return → ノックバック発生
- 被弾時に `FaceEnemy` で敵の方を向く（後方へ吹っ飛ぶリアクションを自然に見せるため）

**PlayerCombat.cs**
- `AttackMoveCoroutine` の先頭で `IsKnockback` を見て、ノックバック中は前進を中断

### 状態の流れ

```text
被弾(TakeDamage)
  ├─ KnockbackVelocityCoroutine : 速度を与える → knockbackDuration後に速度0
  └─ EnterKnockback : CurrentState = Knockback / Knockbackトリガー / セーフティ開始
       │
       │  この間、IsKnockback=true により
       │  移動・ジャンプ・攻撃前進・攻撃発生が全て無効
       │
  ノックバックアニメ再生
       │
  アニメ末尾の Animation Event → EndKnockback : CurrentState = Idle
       └─ （保険）EndKnockbackが来なければ knockbackSafetyTime 後に強制Idle
```

---

## 3. Unity側の設定（Animator）

ノックバックState化に必要なAnimator設定。

1. Trigger パラメータ **`Knockback`** を追加
2. ノックバック用アニメーションクリップを用意（**Loop Time は OFF**）
3. 遷移 **`Any State → Knockback`**（Condition: `Knockback` トリガー、Can Transition To Self OFF）
4. 遷移 **`Knockback → Idle/Run`**（`Has Exit Time` **ON**）
5. クリップ末尾付近に Animation Event **`EndKnockback`** を配置

### ハマりどころ（実際に踏んだもの）

| 症状 | 原因 | 対処 |
| --- | --- | --- |
| ノックバックがループする | Animation Eventの関数を `EndKnockback` ではなく `EnterKnockback` に設定していた | イベントの関数を `EndKnockback` に修正 |
| EndKnockbackが発火せずセーフティ警告 | `Has Exit Time` の Exit Time は「秒」ではなく**正規化割合(0〜1)**。イベント位置(0.9)より小さい値だと、イベント到達前に遷移して発火しない | Exit Time をイベント位置より大きく（0.95） |
| Exit Time調整後もセーフティ警告 | `knockbackSafetyTime`(1.0秒)が、正規イベントの発火時刻(約0.95秒)と近すぎて競合し、保険が先に発火 | `knockbackSafetyTime` を 2.0 に（保険は通常より十分長く） |

> **Exit Time は割合（0=先頭, 1=末尾）。イベント位置より大きい値にする。**
> **セーフティ時間は「イベントが永遠に来ない時の最後の砦」。通常の再生時間より十分長く設定する。**

---

## 4. Enemy側のノックバック統一

### 症状
移動入力しながら被弾すると、ノックバックがほぼ発生しないように見えた。

### 原因
入力で敵に近づくと、`EnemyAttack`（force 20）より先に **`EnemyTouchDamage`（接触ダメージ、force 2.5）** が発生していた。
ノックバック移動距離は **力 × knockbackDuration** なので：

| ヒット源 | 計算 | 距離 |
| --- | --- | --- |
| 接触(2.5) | 2.5 × 0.15秒 | 約0.375ユニット（ほぼ見えない） |
| 攻撃(20) | 20 × 0.15秒 | 約3ユニット（見える） |

さらに接触ダメージは無敵を付与するため、後続の攻撃が入らず「移動時だけ弱い」状態になっていた。
（Player側のコードは正常で、FixedUpdateはノックバック速度を正しく保持していたことをログで確認済み）

### 対処
- `EnemyTouchDamage.TOUCH_KNOCKBACK` を **2.5 → 10** に変更し、攻撃と手応えを統一

---

## 5. まとめ

| 観点 | 結果 |
| --- | --- |
| コーディネーター | 移動系は保持しつつ、未使用物の削除・Update分割で可読性を整理 |
| ノックバック | PlayerStateの正式な状態(Knockback)として管理。IsKnockbackを状態判定に一本化し、全ゲートを自動でState基準に |
| 行動不能 | ノックバックアニメ再生中はPlayerの全行動が無効。解除はAnimation Event、保険にセーフティタイマー |
| Enemy統一 | 接触と攻撃のノックバック力を10で統一 |
