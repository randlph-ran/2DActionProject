using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// アイテムメニュー全体の管理クラス。
/// グリッド表示・2Dカーソルナビゲーション・装備確認ポップアップを制御する。
/// </summary>
public class InventoryMenuUI : MonoBehaviour
{
    //==============================
    // 定数
    //==============================

    private const int Columns = 4;
    private const int Rows    = 2;

    //==============================
    // UI参照
    //==============================

    [Header("メニュールート")]

    [Tooltip("メニュー全体のルートオブジェクト")]
    [SerializeField]
    private GameObject inventoryMenu;

    [Header("アイテムグリッド")]

    [Tooltip("スロットを並べる親オブジェクト（Grid Layout Group）")]
    [SerializeField]
    private Transform itemGridContainer;

    [Tooltip("アイテムスロットのPrefab")]
    [SerializeField]
    private GameObject itemSlotPrefab;

    [Header("装備欄（右上）")]

    [Tooltip("装備中アイテムのアイコン画像")]
    [SerializeField]
    private Image equippedIcon;

    [Tooltip("装備中アイテム名テキスト（未装備時は「装備無し」）")]
    [SerializeField]
    private TextMeshProUGUI equippedLabel;

    [Header("説明テキスト（下部）")]

    [Tooltip("選択中アイテムの説明テキスト")]
    [SerializeField]
    private TextMeshProUGUI descriptionText;

    [Header("確認ポップアップ")]

    [Tooltip("確認ポップアップUI")]
    [SerializeField]
    private ConfirmationPopupUI confirmPopup;

    //==============================
    // 依存コンポーネント
    //==============================

    [Header("依存コンポーネント")]

    [SerializeField]
    private PlayerInputReader inputReader;

    [SerializeField]
    private InventoryManager inventoryManager;

    //==============================
    // ナビゲーション設定
    //==============================

    [Header("ナビゲーション設定")]

    [Tooltip("長押し時の最初のリピートまでの待機時間（秒）")]
    [SerializeField]
    private float firstRepeatDelay = 0.4f;

    [Tooltip("リピート間隔（秒）")]
    [SerializeField]
    private float repeatDelay = 0.1f;

    //==============================
    // 内部状態
    //==============================

    /// <summary>メニューの状態</summary>
    private enum MenuState
    {
        ItemGrid,     // グリッドでアイテム選択中
        ConfirmPopup  // 装備確認ポップアップ表示中
    }

    private MenuState currentState;
    private bool      isOpen;

    // グリッドカーソルの平坦インデックス（0 〜 Columns×Rows-1）
    private int selectedIndex;

    // 生成済みスロット一覧
    private readonly List<InventoryItemSlot> itemSlots = new();

    // グリッドナビゲーション用
    private float navigateTimer;
    private bool  isHolding;

    // ポップアップ水平入力の前フレーム値（押した瞬間だけ反応させるため）
    private float prevPopupHorizontal;

    //==============================
    // Unity イベント
    //==============================

    private void Update()
    {
        // メニュー開閉は状態に関わらず常に受け付ける
        if (inputReader.MenuPressed)
        {
            ToggleMenu();
            return;
        }

        if (!isOpen) return;

        switch (currentState)
        {
            case MenuState.ItemGrid:
                HandleGridNavigation();
                if (inputReader.MenuConfirmPressed) OnGridConfirm();
                if (inputReader.MenuCancelPressed)  CloseMenu();
                break;

            case MenuState.ConfirmPopup:
                HandlePopupNavigation();
                if (inputReader.MenuConfirmPressed) confirmPopup.Confirm();
                if (inputReader.MenuCancelPressed)  CloseConfirmPopup();
                break;
        }
    }

    //==============================
    // メニュー開閉
    //==============================

    private void ToggleMenu()
    {
        if (isOpen) CloseMenu();
        else        OpenMenu();
    }

    private void OpenMenu()
    {
        isOpen                 = true;
        inputReader.IsMenuOpen = true;
        Time.timeScale         = 0f;

        inventoryMenu.SetActive(true);

        // カーソルとタイマーを初期化
        selectedIndex = 0;
        navigateTimer = 0f;
        isHolding     = false;
        currentState  = MenuState.ItemGrid;

        RefreshGrid();
    }

    private void CloseMenu()
    {
        isOpen                 = false;
        inputReader.IsMenuOpen = false;
        Time.timeScale         = 1f;

        inventoryMenu.SetActive(false);
        confirmPopup.Hide();
    }

    //==============================
    // グリッドナビゲーション
    //==============================

    private void HandleGridNavigation()
    {
        Vector2 nav = inputReader.MenuNavigateInput;

        if (nav.magnitude < 0.5f)
        {
            navigateTimer = 0f;
            isHolding     = false;
            return;
        }

        navigateTimer -= Time.unscaledDeltaTime;
        if (navigateTimer > 0f) return;

        // 斜め入力は上下優先で4方向に限定
        if (Mathf.Abs(nav.y) >= Mathf.Abs(nav.x))
            NavigateGrid(0, nav.y > 0 ? -1 : 1);  // 上 / 下
        else
            NavigateGrid(nav.x > 0 ? 1 : -1, 0);  // 右 / 左

        navigateTimer = isHolding ? repeatDelay : firstRepeatDelay;
        isHolding     = true;
    }

    /// <summary>
    /// グリッドカーソルを移動する。
    /// 端・空スロットは移動しない（止まる）。
    /// </summary>
    private void NavigateGrid(int colDelta, int rowDelta)
    {
        int currentRow = selectedIndex / Columns;
        int currentCol = selectedIndex % Columns;

        int newRow   = Mathf.Clamp(currentRow + rowDelta, 0, Rows    - 1);
        int newCol   = Mathf.Clamp(currentCol + colDelta, 0, Columns - 1);
        int newIndex = newRow * Columns + newCol;

        // 移動先にスロットがある場合のみ移動
        if (newIndex < itemSlots.Count)
        {
            selectedIndex = newIndex;
            UpdateSlotSelection();
            UpdateDescription();
        }
    }

    private void UpdateSlotSelection()
    {
        for (int i = 0; i < itemSlots.Count; i++)
            itemSlots[i].SetSelected(i == selectedIndex);
    }

    //==============================
    // グリッド決定・ポップアップ表示
    //==============================

    private void OnGridConfirm()
    {
        if (itemSlots.Count == 0) return;

        InventoryItem item      = inventoryManager.Inventory[selectedIndex];
        bool          isEquipped = inventoryManager.EquippedItem == item.itemData;

        string message = isEquipped
            ? $"{item.itemData.ItemName} を外しますか？"
            : $"{item.itemData.ItemName} を装備しますか？";

        prevPopupHorizontal = 0f;
        currentState        = MenuState.ConfirmPopup;

        confirmPopup.Show(
            message,
            onYesCallback: () => ExecuteEquip(item),
            onNoCallback:  ()  => CloseConfirmPopup()
        );
    }

    private void ExecuteEquip(InventoryItem item)
    {
        if (inventoryManager.EquippedItem == item.itemData)
            inventoryManager.UnequipItem();
        else
            inventoryManager.EquipItem(item.itemData);

        CloseConfirmPopup();
        RefreshGrid();
    }

    private void CloseConfirmPopup()
    {
        confirmPopup.Hide();
        currentState = MenuState.ItemGrid;
    }

    //==============================
    // ポップアップナビゲーション
    //==============================

    private void HandlePopupNavigation()
    {
        float h = inputReader.MenuNavigateInput.x;

        // 閾値を越えた瞬間だけ切り替え（リピートなし）
        if      (h >  0.5f && prevPopupHorizontal <=  0.5f) confirmPopup.MoveSelection( 1f);
        else if (h < -0.5f && prevPopupHorizontal >= -0.5f) confirmPopup.MoveSelection(-1f);

        prevPopupHorizontal = h;
    }

    //==============================
    // グリッド生成・更新
    //==============================

    private void RefreshGrid()
    {
        // 既存スロットを破棄
        foreach (Transform child in itemGridContainer)
            Destroy(child.gameObject);
        itemSlots.Clear();

        // 所持アイテム数ぶん（最大 Columns×Rows）スロットを生成
        int count = Mathf.Min(inventoryManager.Inventory.Count, Columns * Rows);
        for (int i = 0; i < count; i++)
        {
            InventoryItem     item    = inventoryManager.Inventory[i];
            GameObject        slotObj = Instantiate(itemSlotPrefab, itemGridContainer);
            InventoryItemSlot slot    = slotObj.GetComponent<InventoryItemSlot>();

            bool isEquipped = inventoryManager.EquippedItem == item.itemData;
            slot.Setup(item, isEquipped, OnSlotClicked);
            itemSlots.Add(slot);
        }

        // アイテム減少時にカーソルが範囲外にならないよう補正
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, itemSlots.Count - 1));

        UpdateSlotSelection();
        UpdateDescription();
        UpdateEquipmentDisplay();
    }

    /// <summary>マウスクリック時：クリックされたスロットにカーソルを合わせてポップアップを開く</summary>
    private void OnSlotClicked(InventoryItem item)
    {
        int idx = inventoryManager.Inventory.IndexOf(item);
        if (idx < 0) return;

        selectedIndex = idx;
        UpdateSlotSelection();
        UpdateDescription();
        OnGridConfirm();
    }

    //==============================
    // 各表示更新
    //==============================

    private void UpdateDescription()
    {
        if (descriptionText == null) return;

        descriptionText.text = (itemSlots.Count > 0 && selectedIndex < inventoryManager.Inventory.Count)
            ? inventoryManager.Inventory[selectedIndex].itemData.Description
            : string.Empty;
    }

    private void UpdateEquipmentDisplay()
    {
        ItemData equipped = inventoryManager.EquippedItem;

        if (equippedLabel != null)
            equippedLabel.text = equipped != null ? equipped.ItemName : "装備無し";

        if (equippedIcon != null)
        {
            equippedIcon.sprite  = equipped?.ItemIcon;
            equippedIcon.enabled = equipped?.ItemIcon != null;
        }
    }
}
