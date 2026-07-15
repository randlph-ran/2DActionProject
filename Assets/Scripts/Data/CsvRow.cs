using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// CSVの1行分のデータ。
/// ヘッダー行の列名をキーにして値を取り出す。
///
/// 取得メソッドは2系統ある：
/// ・GetXxxOrNull() … 未指定(列が無い/空欄/変換失敗)なら null を返す。
///                    「CSVで指定されていなければInspector値を使う」判定に使う。
/// ・GetXxx(既定値)  … 未指定なら既定値を返す。単純に読みたいときに使う。
///
/// いずれもデータ側のミスでクラッシュせず、警告を出して既定値/nullに倒れる。
/// </summary>
public class CsvRow
{
    // 列名 → 値
    private readonly Dictionary<string, string> values;

    // エラー表示用：どのファイルの何行目か
    private readonly string sourceName;
    private readonly int lineNumber;

    public CsvRow(Dictionary<string, string> values, string sourceName, int lineNumber)
    {
        this.values = values;
        this.sourceName = sourceName;
        this.lineNumber = lineNumber;
    }

    /// <summary>この行が指定した列を持っているか</summary>
    public bool Has(string column)
    {
        return values.ContainsKey(column);
    }

    //==============================
    // 未指定ならnullを返す系
    //==============================

    /// <summary>
    /// 文字列として取得する。未指定なら null。
    /// CSV上の \n（バックスラッシュ+n の2文字）は改行に変換される。
    /// セル内で実際に改行するとExcelでの編集時に壊れやすいため、\n で書く運用にしている。
    /// </summary>
    public string GetStringOrNull(string column)
    {
        if (!TryGetRaw(column, out string raw))
        {
            return null;
        }

        // CSVには \n と書いておき、ここで実際の改行へ変換する
        return raw.Replace("\\n", "\n");
    }

    /// <summary>intとして取得する。未指定・変換失敗なら null</summary>
    public int? GetIntOrNull(string column)
    {
        if (!TryGetRaw(column, out string raw))
        {
            return null;
        }
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }
        WarnParseFailed(column, raw, "int");
        return null;
    }

    /// <summary>floatとして取得する。未指定・変換失敗なら null</summary>
    public float? GetFloatOrNull(string column)
    {
        if (!TryGetRaw(column, out string raw))
        {
            return null;
        }
        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        WarnParseFailed(column, raw, "float");
        return null;
    }

    /// <summary>
    /// boolとして取得する。未指定・変換失敗なら null。
    /// true / false / 1 / 0 / yes / no / on / off を受け付ける（大文字小文字は無視）。
    /// </summary>
    public bool? GetBoolOrNull(string column)
    {
        if (!TryGetRaw(column, out string raw))
        {
            return null;
        }

        switch (raw.ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
            case "on":
                return true;
            case "false":
            case "0":
            case "no":
            case "off":
                return false;
        }

        WarnParseFailed(column, raw, "bool");
        return null;
    }

    //==============================
    // 未指定なら既定値を返す系
    //==============================

    /// <summary>文字列として取得する。未指定なら既定値</summary>
    public string GetString(string column, string defaultValue = "")
    {
        return GetStringOrNull(column) ?? defaultValue;
    }

    /// <summary>intとして取得する。未指定なら既定値</summary>
    public int GetInt(string column, int defaultValue = 0)
    {
        return GetIntOrNull(column) ?? defaultValue;
    }

    /// <summary>floatとして取得する。未指定なら既定値</summary>
    public float GetFloat(string column, float defaultValue = 0f)
    {
        return GetFloatOrNull(column) ?? defaultValue;
    }

    /// <summary>boolとして取得する。未指定なら既定値</summary>
    public bool GetBool(string column, bool defaultValue = false)
    {
        return GetBoolOrNull(column) ?? defaultValue;
    }

    //==============================
    // 内部処理
    //==============================

    // 列の生値を取得する。列が無い or 空欄なら false（＝未指定として扱う）
    private bool TryGetRaw(string column, out string raw)
    {
        if (!values.TryGetValue(column, out raw))
        {
            // 列そのものが無い＝ヘッダーの綴りミスの可能性が高いので警告する
            Debug.LogWarning($"[CSV] {sourceName}({lineNumber}行目): 列 '{column}' がありません");
            raw = null;
            return false;
        }

        // 空欄は「未指定」として扱う（警告は出さない。意図的に空けている運用のため）
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = null;
            return false;
        }

        raw = raw.Trim();
        return true;
    }

    // 変換失敗の警告
    private void WarnParseFailed(string column, string raw, string typeName)
    {
        Debug.LogWarning($"[CSV] {sourceName}({lineNumber}行目): 列 '{column}' の値 '{raw}' を {typeName} に変換できません");
    }
}
