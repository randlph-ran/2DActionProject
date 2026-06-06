
# 2Dアクションゲーム α版仕様書

## 目次
- [概要](#info)
- [操作方法](#playinfo)
- [プレイヤー仕様](#player)
- [戦闘仕様](#battle)
- [Enemy仕様](#enemy)
- [Boss仕様](#boss)
- [UI仕様](#ui)
- [シーン構成](#scene)
- [カメラ仕様](#camera)
- [ゲームフロー](#gameflow)
- [α版実装項目](#alpha)
- [β版実装対応候補](#beta)

## 1. 概要<a id="info"></a>

本作はUnity 6000.3.12f1で開発中の2D横スクロールアクションゲームである。

α版ではゲーム全体を最後まで遊べる状態を目標とし、プレイ感やゲームバランスの調整を実施する。

※本仕様書はAIによる出力されたもの。一部修正を加えている。

------------------------------------------------------------------------

## 2. 操作方法<a id="playinfo"></a>

 - α版

 | 操作 | キー|
 | ------------ | ------- |
 | 左移動 | ← / A |
 | 右移動 | → / D |
 | ジャンプ | Space |
 | 攻撃 | Z |
 | ゲーム終了| Esc |

  - β版(予定)

 | 操作 | キー|
 | ------------ | ------- |
 | 左移動 | ← / A / 左スティック左 |
 | 右移動 | → / D / 左スティック右 |
 | ジャンプ | Space / Aボタン / マウス左クリック |
 | 攻撃 | Z / Bボタン / マウス右クリック |
 | ゲーム終了| Esc |

### ジャンプ

-   最大2段ジャンプ

### 攻撃

-   3段コンボ攻撃…タイミングよく攻撃ボタンを押すと次の攻撃(コンボ)が発生する
-   Attack1 → Attack2 → Attack3
-   コンボごとに攻撃範囲とノックバック量が異なる
  

------------------------------------------------------------------------

## 3. プレイヤー仕様<a id="player"></a>

### 移動

-   左右移動
-   向き反転可能
-   空中移動可能

### ジャンプ

-   2段ジャンプ対応
-   着地時にジャンプ回数リセット

### 落下

-   最大落下速度制限あり

### ダメージ

-   被弾時ノックバック
-   HPが0になると死亡
-   最大HP100(仮)⇒マップ移動で回復(β版ではHPを引き継がせる)

------------------------------------------------------------------------

## 4. 戦闘仕様<a id="battle"></a>

### コンボ攻撃

#### Attack1

-   近距離攻撃
-   小ノックバック

  <img width="100" height="100" alt="Attack1_2" src="https://github.com/user-attachments/assets/d343abab-8a4f-4d31-8d55-9c4e49ebf812" />

#### Attack2

-   中距離攻撃
-   中ノックバック

 <img width="130" height="100" alt="Attack2_1" src="https://github.com/user-attachments/assets/061dd310-5928-442a-8c16-79a32cfb121f" />

 
#### Attack3

-   広範囲攻撃
-   大ノックバック

<img width="124" height="100" alt="Attack3_2" src="https://github.com/user-attachments/assets/197edbf7-9a8b-47c6-aa39-2b8d16639da1" />

### 攻撃判定

-   OverlapCircleによる判定
-   コンボごとに攻撃位置・攻撃半径を変更

------------------------------------------------------------------------

## 5. Enemy仕様<a id="enemy"></a>

### 共通機能

-   巡回行動
-   プレイヤー発見
-   追跡行動
-   攻撃行動
-   ダメージ処理
-   ノックバック
-   死亡処理

### Enemyバリエーション
**※実機ではカラーパレットをいじっているため、ここに載せているものとは色味が違う**
-   通常Enemy
<img width="100" height="100" alt="Enemy0_Idle1" src="https://github.com/user-attachments/assets/e6d61791-da36-4036-8c07-93a7f012836b" />

-   高速移動Enemy
<img width="177" height="100" alt="EnemyFast0_a01" src="https://github.com/user-attachments/assets/443c0241-abdb-437c-9055-b49fc4bc1d16" />

-   BossEnemy
<img width="355" height="200" alt="BossIdle" src="https://github.com/user-attachments/assets/4f8048e6-ab5e-4503-995b-cf0cee78e972" />


### 重量システム
-   PlayerとEnemyにWeightの概念を持たせる
-   α版では下記の簡単な仕様実装だが、β版ではEnemyによって押せたり押されたりするものを用意する
  
#### Weight 1

-   プレイヤーと押し合いで均衡

#### Weight 2

-   Boss級
-   プレイヤーが押し負ける

------------------------------------------------------------------------

## 6. Boss仕様<a id="boss"></a>

### HP

-   専用HPを持つ

### UI

-   Boss専用HPゲージ表示

### 戦闘

-   通常Enemyより高耐久
-   複数攻撃パターン実装予定

------------------------------------------------------------------------

## 7. UI仕様<a id="ui"></a>

### Player UI

-   HP表示
<img width="462" height="55" alt="PlayerHPUI" src="https://github.com/user-attachments/assets/eb57ca8c-d7b5-4b6b-bcde-8adac65203c3" />


### Boss UI

-   Boss HPゲージ
<img width="308" height="42" alt="BossHPUI" src="https://github.com/user-attachments/assets/904565cf-e441-458d-b2e0-3f724b8c7a56" />


### ステージ開始演出

-   シーン開始時フェードイン＆ステージタイトル表示演出
<img width="790" height="442" alt="StartEffect" src="https://github.com/user-attachments/assets/1dad23f5-928c-41ec-bba0-9fba3a4223a6" />


------------------------------------------------------------------------

## 8. シーン構成<a id="scene"></a>

### TitleScene

-   タイトル表示
-   Enterでゲーム開始
<img width="1122" height="632" alt="Title" src="https://github.com/user-attachments/assets/a33e06f0-2a38-479c-a61f-2f5b18696d98" />


### Stage1-1

-   通常ステージ(横長)
<img width="656" height="224" alt="1-1" src="https://github.com/user-attachments/assets/32faae11-6d47-411e-b60b-090d2c135d27" />

### Stage1-2
-   通常ステージ(縦長)
<img width="234" height="394" alt="タイトルなし" src="https://github.com/user-attachments/assets/e7dd1568-1d37-4b64-9f26-48d28cb96103" />


### BossScene

-   ボス専用ステージ
<img width="559" height="233" alt="1-Boss" src="https://github.com/user-attachments/assets/610cf0f7-f0c5-4b71-becc-820f249deaa9" />


### ClearScene

-   クリア演出
  ToBeContinued　のテキスト表示のみ
-   5秒後にTitleSceneへ戻る

### GameOverScene

-   ゲームオーバー演出
 <img width="789" height="439" alt="GameOver" src="https://github.com/user-attachments/assets/d8a94060-32f8-48f4-80de-e5b6da260706" />

-   5秒後にTitleSceneへ戻る


------------------------------------------------------------------------

## 9. カメラ仕様<a id="camera"></a>

### プレイヤー追従

-   Cinemachine使用

### 追従設定

-   Dead Zone対応
-   Screen Positionで表示位置調整

------------------------------------------------------------------------

## 10. ゲームフロー<a id="gameflow"></a>

- TitleScene

  ↓
  
- Stage1-1

  ↓
  
- Stage1-2

  ↓
  
- BossScene

  ↓
  
- ClearScene

  ↓
  
- TitleScene

死亡時

- Stage1-1 / Stage1-2 / BossScene

  ↓
  
-  GameOverScene

  ↓
  
- TitleScene

------------------------------------------------------------------------

## 11. α版実装済み機能<a id="alpha"></a>

-   タイトル画面
-   シーン遷移
-   フェード演出
-   Player HP
-   Enemy HP
-   Boss HP
-   2段ジャンプ
-   3段コンボ
-   ノックバック
-   Enemy AI
-   Boss戦
-   GameOver
-   Clear
-   Esc終了
-   自動タイトル復帰

------------------------------------------------------------------------

## 12. β版実装対応候補<a id="beta"></a>

-   新Input System移行(ゲームパッド、マウス対応)
-   ステージ追加
-   Player挙動周りの修正と調整
-   Enemy種類追加
-   EnemyAI周り修正
-   BGM追加、SE追加
-   アイテム追加
-   UI改善
-   演出強化
