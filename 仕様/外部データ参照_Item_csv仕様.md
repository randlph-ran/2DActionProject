# 外部データ参照：item.csv 仕様

アイテムのパラメータをCSVから読み込む仕組みの仕様書。
CSVの書式、読み込みの流れ、文字コード（UTF-8 with BOM）の注意点をまとめる。

---

## 1. 概要

### 目的
- アイテムのパラメータをCSVで管理し、**ビルド後もCSVを差し替えるだけで調整できる**ようにする
- CSVが壊れていてもゲームが落ちないよう、**Inspector値へフォールバック**する

### 基本方針

| 項目 | 方針 |
| --- | --- |
| 読み込みタイミング | **実行時**にCSVを読む（ビルド後の差し替えが可能） |
| 置き場所 | `Assets/StreamingAssets/item.csv` |
| CSVに入れるもの | 数値・文字列・フラグのみ |
| CSVに入れないもの | Sprite / AudioClip / Prefab などのアセット参照、itemType（Inspectorで設定） |
| 壊れたとき | Inspector値を使って通常どおり動作する |

---

## 2. 文字コード：UTF-8 with BOM（重要）

### 必ず「UTF-8 with BOM」で保存すること

理由を理解しておくと事故を防げる。

#### 前提：テキストファイルに文字コードの情報は入っていない
ファイルの実体はただのバイトの羅列で、「これはUTF-8です」というラベルはどこにも無い。
そのため、開く側が**文字コードを推測するしかない**。

#### 文字化けの正体
同じ文字でも文字コードによってバイトの並びが違う。

| 文字 | UTF-8 | Shift-JIS |
| --- | --- | --- |
| `あ` | `E3 81 82`（3バイト） | `82 A0`（2バイト） |

日本語版WindowsのExcelは**デフォルトでShift-JISと推測する**ため、
UTF-8のCSVをそのまま開くとバイトの区切りがズレて `アイテムデータ` が `繧｢繧､繝・Β繝・・繧ｿ` のように化ける。

#### BOMとは
**ファイル先頭に置く3バイトの目印** `EF BB BF`。「これはUTF-8です」と明示的に宣言するもの。

```text
BOMあり： EF BB BF | 23 20 E3 82 A2 ...
          └─BOM─┘   └──── 本文 ────┘
BOMなし： 23 20 E3 82 A2 ...
```

Excelは先頭を見て判定する。

- 先頭が `EF BB BF` → 「UTF-8だ」と認識 → 正しく開ける
- 先頭が `EF BB BF` でない → 「Shift-JISだろう」と推測 → 文字化け

#### 名前の由来（Byte Order Mark）
本来はUTF-16用の仕組み。UTF-16は1文字2バイトのため「どちらのバイトを先に置くか」の流儀が2つあり
（`FE FF`=ビッグエンディアン / `FF FE`=リトルエンディアン）、その**バイト順（Byte Order）**を示すのが元の役目。
UTF-8にはバイト順の問題が無いため、**UTF-8のBOMは単なる「文字コードの署名」として流用されている**。

正体は文字 **U+FEFF（ゼロ幅ノーブレークスペース）＝見えない文字**。UTF-8で符号化すると `EF BB BF` になる。

#### BOMは見えないのが正常
幅ゼロの見えない文字であり、BOMを理解しているエディタ／Excelは
「本文」ではなく「文字コードの目印」として消費するため画面に表示されない。

#### プログラム側ではBOMを除去する
BOMは本文ではないので、そのまま読むと**1列目の列名が `id` ではなく `﻿id` になり、列の参照に失敗する**。
そのため `CsvLoader` で読み込み時に除去している。

```csharp
/* 先頭のBOM(U+FEFF)を除去する（Excel保存時に付くことがある） */
if (text.Length > 0 && text[0] == (char)0xFEFF)
{
    text = text.Substring(1);
}
```

**「Excelには目印が要る／プログラムには目印が邪魔」** という役割分担になっている。

### BOMの確認・付与方法

| 目的 | 方法 |
| --- | --- |
| 確認（エディタ） | VSCode等のステータスバーに `UTF-8 with BOM` と出るか（`UTF-8` だけならBOM無し） |
| 確認（コマンド） | `file item.csv` → `(with BOM)` と出るか |
| 確認（バイト） | `head -c 16 item.csv | xxd` → 先頭が `efbb bf` か |
| 付与（Excel） | 「CSV UTF-8(コンマ区切り)」で保存する（自動でBOMが付く） |
| 付与（VSCode） | ステータスバーの文字コード → 「エンコード付きで保存」 → `UTF-8 with BOM` |
| 付与（メモ帳） | 名前を付けて保存 → 文字コードで `UTF-8 (BOM付き)` |

---

## 3. CSVの書式ルール

| ルール | 内容 |
| --- | --- |
| ヘッダー | 最初の有効行を列名として扱う |
| **列の指定方法** | **列名（ヘッダー）をキーに読む。並び順は自由で、変えてもコード修正は不要** |
| コメント行 | 行頭が `#` の行は無視される |
| 空行 | 無視される |
| 空欄 | 「未指定」として扱い、**その項目だけInspector値が使われる** |
| 改行の表現 | セル内で改行せず、`\n` と書く（読み込み時に改行へ変換される） |
| カンマを含む値 | `"..."` で囲む（引用符内のカンマ・改行はそのまま扱える） |
| 引用符自体 | `""` と2つ重ねる |
| bool値 | `true/false` `1/0` `yes/no` `on/off`（大文字small問わず。Excelが書く `TRUE/FALSE` もそのまま読める） |
| **列順の方針** | **文字数の多い項目（descriptionなど）は表が見づらくなるため一番後ろに置く** |

---

## 4. item.csv 列定義

```text
id,itemName,maxUseCount,cooldown,canAutoFire,canUseInAir,useTimeoutDuration,value,knockbackPower,launchPower,chargeTime,description
```

| 列名 | 型 | 内容 |
| --- | --- | --- |
| `id` | string | ItemDataと紐づけるキー。**ItemDataアセット名**（またはItemDataのidに設定した文字列）と一致させる |
| `itemName` | string | アイテム名 |
| `maxUseCount` | int | 最大使用回数（-1で無限） |
| `cooldown` | float | 再使用までの待機時間 |
| `canAutoFire` | bool | ボタン長押しで連射するか |
| `canUseInAir` | bool | 空中で使用可能か |
| `useTimeoutDuration` | float | 使用アニメのタイムアウト秒数（AnimationEvent不発時の保険） |
| `value` | int | 効果の大きさ（Projectile:ダメージ / Recovery:回復量） |
| `knockbackPower` | float | ノックバック強さ |
| `launchPower` | float | 打ち上げ強さ |
| `chargeTime` | float | 発動前のチャージ時間（Crash用） |
| `description` | string | アイテム説明（`\n` で改行。**長文のため末尾に配置**） |

### CSVで扱わない項目（Inspectorで設定）

| 項目 | 理由 |
| --- | --- |
| `itemIcon` (Sprite) | アセット参照はCSVで表現できない |
| `pickupSE` (AudioClip) | 同上 |
| `projectilePrefab` (GameObject) | 同上 |
| `crashEffectPrefab` (GameObject) | 同上 |
| `itemType` (enum) | アセット参照と密結合な構造的項目のため |

---

## 5. 読み込みの仕組み

### データの流れ

```text
Assets/StreamingAssets/item.csv
        │  初回アクセス時に自動で読み込み
        ▼
CsvLoader          … ファイル読み込み＋パース（BOM除去・引用符・コメント対応）
        ▼
ItemDatabase       … id → ItemStats の辞書を構築（static。シーン配置不要）
        ▲
        │  ItemDataが自分のIDで引きに行く（pull型）
ItemData(SO)       … プロパティが「CSV値 or Inspector値」を返す
        ▼
PlayerItemAction 等 … CurrentItem.Cooldown など。呼び出し側は変更不要
```

### 設計上のポイント

**① pull型にしてアセット列挙を不要にした**
Databaseが全ItemDataを探して値を配る（push型）のではなく、**ItemDataが自分のIDでDatabaseを引く**形にした。
これにより **Resourcesフォルダへのアセット移動も、登録リストの管理も不要**になっている。

**② シーン設定が不要**
`ItemDatabase` はstaticクラスで、**初回アクセス時に自動でCSVを読む**。ロード用オブジェクトの配置は要らない。

**③ IDは未設定でも動く**
```csharp
public string Id => string.IsNullOrEmpty(id) ? name : id;
```
Inspectorの `id` が空なら**アセット名**（`Knife` / `Potion`）がIDとして使われる。
CSVのid列と一致するため、Inspectorでの設定作業はゼロ。

**④ 呼び出し側のコードは変更不要**
`CurrentItem.Cooldown` などの呼び出しはそのままで、中身がCSV値を返すようになっている。

---

## 6. フォールバックの仕組み

### なぜInspector値を上書きしないのか

ScriptableObjectの `[SerializeField]` を実行時に書き換えると、
**エディタではその変更が `.asset` に永続化され**、Playのたびにアセットが書き換わってgit差分が出続ける。

さらに致命的なのは、**上書きするとInspectorの元の値が消えるため、フォールバック先が無くなる**こと。
「CSVが無いときはInspector値を使う」を実現するには、**Inspector値を絶対に上書きしない**ことが必須になる。

### 箱を2つに分ける

```csharp
public class ItemData : ScriptableObject
{
    /* 【箱1】Inspector値 = フォールバック。絶対に書き換えない */
    [SerializeField] private float cooldown = 0.5f;

    /* 【箱2】CSV値 = ItemDatabaseから取得。アセットには保存されない */
    private ItemStats Stats => ItemDatabase.GetStats(Id);

    /* CSVがあればCSV値、無ければInspector値 */
    public float Cooldown => Stats?.cooldown ?? cooldown;
}
```

`ItemStats` の各項目は**null許容**になっており、`null` = 「CSVで未指定」を意味する。
これにより列単位でフォールバックできる。

### 動作パターン

| ケース | 結果 |
| --- | --- |
| CSVが正常に読めた | **CSV値**を使う |
| CSVファイルが無い / 読み込み失敗 | **Inspector値**を使う（エラーログは出るがゲームは正常動作） |
| CSVはあるが、その `id` の行が無い | **Inspector値**を使う |
| 行はあるが、その列が空欄 | **その項目だけInspector値**を使う |
| 値が型変換できない（例: cooldownに文字列） | 警告を出して**その項目だけInspector値**を使う |

---

## 7. Excel運用ルール

| 操作 | 手順 |
| --- | --- |
| 開く | ダブルクリックでOK（BOMがあるので化けない） |
| **保存** | **必ず「CSV UTF-8(コンマ区切り)」を選ぶ** |
| 説明文の改行 | セル内で改行せず `\n` と打つ |

> ⚠️ 「CSV(コンマ区切り)」（UTF-8が付かない方）で保存すると**Shift-JISになりファイルが壊れる**。
> 一度「CSV UTF-8」で保存すれば、以降はCtrl+Sで形式が維持される。

Excelで保存するとコメント行に末尾カンマが付いたり、boolが `TRUE/FALSE` の大文字になるが、
どちらもパーサ側で吸収するため問題ない。

---

## 8. 新しいCSVを追加するときの手順

1. `Assets/StreamingAssets/` に **UTF-8 with BOM** でCSVを作成する
   - テキストエディタで新規作成すると**BOM無しになりExcelで化ける**ので注意
2. 1行目に列名を書く（長文の列は末尾に置く）
3. `XxxStats` クラスを作る（各項目は**null許容**にする）
4. `XxxDatabase` を作る（`CsvLoader.Load()` → `id`をキーに辞書化）
5. 対象クラスのプロパティを `Stats?.xxx ?? インスペクタ値` の形にする

---

## 9. 関連ファイル

| ファイル | 役割 |
| --- | --- |
| `Assets/StreamingAssets/item.csv` | アイテムデータ本体 |
| `Assets/Scripts/Data/CsvLoader.cs` | CSV読み込み・パースの共通処理 |
| `Assets/Scripts/Data/CsvRow.cs` | 1行分のデータ。型付きアクセサ（`GetXxxOrNull` / `GetXxx`） |
| `Assets/Scripts/Data/ItemStats.cs` | CSV1件分のデータ（null許容） |
| `Assets/Scripts/Data/ItemDatabase.cs` | item.csvの読み込みと辞書管理 |
| `Assets/Scripts/Item/ItemData.cs` | ScriptableObject。CSV値とInspector値のフォールバックを担当 |
