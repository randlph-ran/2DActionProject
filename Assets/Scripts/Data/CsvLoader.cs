using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// StreamingAssets配下のCSVを読み込む共通ユーティリティ。
/// Enemy / Player / Item / ストーリーテキストなど、全データで使い回す。
///
/// 対応している書式：
/// ・1行目をヘッダー（列名）として扱う
/// ・引用符で囲んだフィールド（"..."）内のカンマ・改行をそのまま扱える
/// ・引用符自体は "" と2つ重ねて表現する
/// ・行頭が # の行はコメントとして無視する
/// ・空行は無視する
/// ・文字列内の \n は改行として読み込まれる（CsvRow.GetString系で変換）
///
/// 【文字コードの注意】CSVは必ず「UTF-8 with BOM」で保存すること。
/// ・BOMが無いとExcelがShift-JISと誤認して日本語が文字化けする（BOMがあれば正しく開ける）
/// ・Excelの「CSV(コンマ区切り)」はShift-JISで保存されるため使わないこと。
///   保存時は必ず「CSV UTF-8(コンマ区切り)」を選ぶこと
/// ・BOM自体はこのローダー側で除去するため、読み込みには影響しない
/// </summary>
public static class CsvLoader
{
    /// <summary>
    /// StreamingAssets配下のCSVファイルを読み込む。
    /// </summary>
    /// <param name="fileName">StreamingAssetsからの相対パス（例: "item.csv"）</param>
    /// <returns>行データのリスト。読み込めなかった場合は空リスト</returns>
    public static List<CsvRow> Load(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"[CSV] ファイルが見つかりません: {path}");
            return new List<CsvRow>();
        }

        string text;
        try
        {
            // UTF-8で読み込む（BOMがあれば自動で処理される）
            text = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (IOException e)
        {
            Debug.LogError($"[CSV] ファイルの読み込みに失敗しました: {path}\n{e.Message}");
            return new List<CsvRow>();
        }

        return Parse(text, fileName);
    }

    /// <summary>
    /// CSVテキストを直接パースする（ファイルを介さないテスト用にも使える）。
    /// </summary>
    /// <param name="text">CSVの中身</param>
    /// <param name="sourceName">エラー表示用の名前</param>
    public static List<CsvRow> Parse(string text, string sourceName = "(text)")
    {
        var rows = new List<CsvRow>();

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[CSV] {sourceName}: 中身が空です");
            return rows;
        }

        // 先頭のBOM(U+FEFF)を除去する（Excel保存時に付くことがある）
        if (text.Length > 0 && text[0] == (char)0xFEFF)
        {
            text = text.Substring(1);
        }

        // 引用符を考慮してレコード（行）とフィールド（列）に分解する
        List<List<string>> records = ParseRecords(text);

        // 有効な行だけ残す（空行・コメント行を除く）。行番号も一緒に持つ
        var validRecords = new List<(List<string> fields, int lineNumber)>();

        for (int i = 0; i < records.Count; i++)
        {
            List<string> fields = records[i];

            // 完全な空行は無視
            if (IsEmptyRecord(fields))
            {
                continue;
            }

            // 行頭が # のコメント行は無視
            if (fields[0].TrimStart().StartsWith("#"))
            {
                continue;
            }

            // 表示用の行番号は1始まり
            validRecords.Add((fields, i + 1));
        }

        if (validRecords.Count == 0)
        {
            Debug.LogWarning($"[CSV] {sourceName}: 有効な行がありません");
            return rows;
        }

        // 1行目をヘッダー（列名）として使う
        List<string> header = validRecords[0].fields;
        for (int i = 0; i < header.Count; i++)
        {
            header[i] = header[i].Trim();
        }

        // 2行目以降をデータ行として読む
        for (int i = 1; i < validRecords.Count; i++)
        {
            List<string> fields = validRecords[i].fields;
            int lineNumber = validRecords[i].lineNumber;

            var values = new Dictionary<string, string>();

            for (int c = 0; c < header.Count; c++)
            {
                // 列名が空のものは無視する
                if (string.IsNullOrEmpty(header[c]))
                {
                    continue;
                }

                // 列数がヘッダーより少ない場合は空欄として扱う
                string value = c < fields.Count ? fields[c] : string.Empty;

                values[header[c]] = value;
            }

            rows.Add(new CsvRow(values, sourceName, lineNumber));
        }

        return rows;
    }

    //==============================
    // 内部処理
    //==============================

    /// <summary>
    /// CSVテキストを1文字ずつ走査して、レコード（行）とフィールド（列）に分解する。
    /// 引用符の中ではカンマ・改行を区切りとして扱わないため、
    /// 説明文やストーリーテキストにカンマや改行が含まれていても壊れない。
    /// </summary>
    private static List<List<string>> ParseRecords(string text)
    {
        var records = new List<List<string>>();
        var currentRecord = new List<string>();
        var currentField = new StringBuilder();

        // 引用符の中にいるか
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // "" は引用符1文字としてのエスケープ
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        // 引用符の終わり
                        inQuotes = false;
                    }
                }
                else
                {
                    // 引用符の中なのでカンマ・改行もそのまま文字として扱う
                    currentField.Append(c);
                }

                continue;
            }

            switch (c)
            {
                // 引用符の始まり
                case '"':
                    inQuotes = true;
                    break;

                // フィールドの区切り
                case ',':
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();
                    break;

                // 改行コードのCRは無視（CRLF対策）
                case '\r':
                    break;

                // レコードの区切り
                case '\n':
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();
                    records.Add(currentRecord);
                    currentRecord = new List<string>();
                    break;

                default:
                    currentField.Append(c);
                    break;
            }
        }

        // 最終行が改行で終わっていない場合の取りこぼしを回収する
        if (currentField.Length > 0 || currentRecord.Count > 0)
        {
            currentRecord.Add(currentField.ToString());
            records.Add(currentRecord);
        }

        return records;
    }

    // 中身が実質空のレコードか
    private static bool IsEmptyRecord(List<string> fields)
    {
        if (fields.Count == 0)
        {
            return true;
        }

        foreach (string f in fields)
        {
            if (!string.IsNullOrWhiteSpace(f))
            {
                return false;
            }
        }

        return true;
    }
}
