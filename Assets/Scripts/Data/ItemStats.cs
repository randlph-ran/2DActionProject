/// <summary>
/// CSVから読み込んだItem1件分のデータ（実行時のみメモリ上に存在する）。
///
/// 各項目はnull許容になっており、
/// null = 「CSVで指定されていない」＝ ItemData のInspector値をそのまま使う、という意味になる。
/// これによりCSVの列を空欄にするだけで、その項目だけInspector値へフォールバックできる。
/// </summary>
public class ItemStats
{
    // 基本情報
    public string itemName;
    public string description;

    // 使用制御
    public int? maxUseCount;
    public float? cooldown;
    public bool? canAutoFire;
    public bool? canUseInAir;
    public float? useTimeoutDuration;

    // 効果量
    public int? value;
    public float? knockbackPower;
    public float? launchPower;

    // Crash用
    public float? chargeTime;
}
