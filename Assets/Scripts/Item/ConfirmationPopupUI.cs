using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 装備確認ポップアップの UI 制御
/// 表示内容と選択肢（はい / いいえ）の管理を担当する。
/// 入力処理は InventoryMenuUI 側で行い、このクラスへメソッド呼び出しで通知する。
/// </summary>
public class ConfirmationPopupUI : MonoBehaviour
{
    [Header("UI参照")]

    [Tooltip("確認メッセージテキスト")]
    [SerializeField]
    private TextMeshProUGUI messageText;

    [Tooltip("「はい」選択中マーカー")]
    [SerializeField]
    private GameObject yesSelectMarker;

    [Tooltip("「いいえ」選択中マーカー")]
    [SerializeField]
    private GameObject noSelectMarker;

    // 実行コールバック
    private Action onYes;
    private Action onNo;

    // 現在「はい」が選ばれているか
    private bool isYesSelected;

    //==============================
    // 表示制御
    //==============================

    /// <summary>
    /// ポップアップを表示する
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="onYesCallback">「はい」選択時の処理</param>
    /// <param name="onNoCallback">「いいえ」選択時の処理</param>
    public void Show(string message, Action onYesCallback, Action onNoCallback)
    {
        onYes = onYesCallback;
        onNo  = onNoCallback;

        messageText.text = message;

        // 「はい」をデフォルト選択にする
        isYesSelected = true;
        UpdateChoiceDisplay();

        gameObject.SetActive(true);
    }

    /// <summary>ポップアップを非表示にする</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    //==============================
    // 選択肢操作（InventoryMenuUI から呼ばれる）
    //==============================

    /// <summary>
    /// 水平入力で「はい / いいえ」を切り替える。
    /// 閾値越え時のみ呼ぶこと（毎フレーム渡さない）。
    /// </summary>
    /// <param name="horizontal">正値 = 右（いいえ）、負値 = 左（はい）</param>
    public void MoveSelection(float horizontal)
    {
        isYesSelected = horizontal < 0f; // 左 = はい、右 = いいえ
        UpdateChoiceDisplay();
    }

    /// <summary>現在の選択肢を実行する</summary>
    public void Confirm()
    {
        if (isYesSelected) onYes?.Invoke();
        else               onNo?.Invoke();
    }

    //==============================
    // 内部処理
    //==============================

    private void UpdateChoiceDisplay()
    {
        if (yesSelectMarker != null) yesSelectMarker.SetActive( isYesSelected);
        if (noSelectMarker  != null) noSelectMarker.SetActive (!isYesSelected);
    }
}
