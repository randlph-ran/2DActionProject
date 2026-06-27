using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インベントリグリッドの1スロット UI 制御
/// </summary>
[RequireComponent(typeof(Button))]
public class InventoryItemSlot : MonoBehaviour
{
    [Header("UI参照")]

    [Tooltip("アイテムアイコン画像")]
    [SerializeField]
    private Image iconImage;

    [Tooltip("所持数テキスト（×n）")]
    [SerializeField]
    private TextMeshProUGUI countText;

    [Tooltip("装備中マーカー（★など）")]
    [SerializeField]
    private GameObject equippedMarker;

    [Tooltip("カーソル選択中のハイライト")]
    [SerializeField]
    private GameObject selectMarker;

    // 対応するインベントリアイテム
    private InventoryItem inventoryItem;

    // マウスクリック時のコールバック
    private Action<InventoryItem> onClicked;

    /// <summary>
    /// スロットの内容をセットアップする
    /// </summary>
    public void Setup(InventoryItem item, bool isEquipped, Action<InventoryItem> onClickCallback)
    {
        inventoryItem = item;
        onClicked     = onClickCallback;

        // アイコン（未設定なら非表示）
        if (iconImage != null)
        {
            // アイコンを設定
            iconImage.sprite  = item.itemData.ItemIcon;
            // アイコンが設定されていない場合は非表示にする
            iconImage.enabled = item.itemData.ItemIcon != null;
            // アイコンのアスペクト比を維持する
            iconImage.preserveAspect = true;
        }

        // 所持数
        if (countText != null)
            countText.text = $"×{item.count}";

        // 装備中マーカー
        if (equippedMarker != null)
            equippedMarker.SetActive(isEquipped);

        // 選択マーカーは初期非表示
        if (selectMarker != null)
            selectMarker.SetActive(false);

        // マウスクリック時に通知
        GetComponent<Button>().onClick.AddListener(() => onClicked?.Invoke(inventoryItem));
    }

    /// <summary>カーソル選択状態のハイライトを切り替える</summary>
    public void SetSelected(bool isSelected)
    {
        if (selectMarker != null) selectMarker.SetActive(isSelected);
    }

    /// <summary>装備中マーカーを更新する</summary>
    public void UpdateEquippedMarker(bool isEquipped)
    {
        if (equippedMarker != null) equippedMarker.SetActive(isEquipped);
    }
}
