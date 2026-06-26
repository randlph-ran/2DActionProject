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
    private const int Rows = 2;

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

    [Header("サウンド")]

    [Tooltip("メニューを開く時のSE")]
    [SerializeField]
    private AudioClip openMenuSE;

    [Tooltip("メニューを閉じる時のSE")]
    [SerializeField]
    private AudioClip closeMenuSE;

    [Tooltip("カーソル移動時のSE")]
    [SerializeField]
    private AudioClip cursorMoveSE;

    [Tooltip("装備確認ポップアップ表示時のSE")]
    [SerializeField]
    private AudioClip popupShowSE;

    [Tooltip("決定時のSE")]
    [SerializeField]
    private AudioClip decideSE;

    [Tooltip("キャンセル時のSE")]
    [SerializeField]
    private AudioClip cancelSE;

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
    private bool isOpen;

    // グリッドカーソルの平坦インデックス（0 〜 Columns×Rows-1）
    private int selectedIndex;

    // 生成済みスロット一覧
    private readonly List<InventoryItemSlot> itemSlots = new();

    // グリッドナビゲーション用
    private float navigateTimer;
    private bool isHolding;

    // ポップアップ水平入力の前フレーム値（押した瞬間だけ反応させるため）
    private float prevPopupHorizontal;

    //==============================
    // Unity イベント
    //==============================

    private void Update()
    {
        // ゲーム開始前（フェード/スタートテキスト演出中など）は入力を無視する
        // フラグを個別に持たず、GameManager.IsGameStarted を単一の判定基準にする
        if (!GameManager.IsGameStarted)
        {
            // ゲーム開始前にメニューが開いたままだと困るので強制的に閉じる
            if (isOpen) CloseMenu();
            return;
        }

        // inputReaderが取得できていない場合は再取得を試みる
        // （Scene遷移時の重複Player/Menu破棄レースにより、一時的に壊れた参照を掴んでしまうケースの保険）
        if (inputReader == null)
        {
            inputReader = FindFirstObjectByType<PlayerInputReader>();
            if (inputReader == null) return;
        }

        // InventoryManager の参照が取れていない場合も再取得を試みる
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        // メニュー開閉は状態に関わらず常に受け付ける
        if (inputReader.MenuPressed)
        {
            ToggleMenu();
            return;
        }
        // メニューが開いていない場合は以降の入力を無視する
        if (!isOpen) return;

        // メニューが開いている場合の入力処理
        switch (currentState)
        {
            // グリッド選択中
            case MenuState.ItemGrid:
                // グリッドナビゲーション処理
                HandleGridNavigation();
                // 決定・キャンセル入力
                if (inputReader.MenuConfirmPressed) OnGridConfirm();
                // キャンセル入力でメニューを閉じる
                if (inputReader.MenuCancelPressed) CloseMenu();
                break;
            // 装備確認ポップアップ表示中
            case MenuState.ConfirmPopup:
                // ポップアップナビゲーション処理
                HandlePopupNavigation();
                // 決定・キャンセル入力
                if (inputReader.MenuConfirmPressed)
                {
                    // 決定SE再生
                    SoundManager.Instance?.PlaySE(decideSE);
                    // ポップアップの「はい」を選択している場合のみ装備処理を実行
                    confirmPopup.Confirm();
                }
                // キャンセル入力でポップアップを閉じる
                if (inputReader.MenuCancelPressed) CloseConfirmPopup();
                break;
        }
    }

    //==============================
    // メニュー開閉の処理
    //==============================

    private void ToggleMenu()
    {
        //　開いてたら閉じて、閉じてたら開く
        if (isOpen) CloseMenu();
        else OpenMenu();
    }

    private void OpenMenu()
    {
        // メニューを開くSE再生
        SoundManager.Instance?.PlaySE(openMenuSE);
        // メニューを開く
        isOpen = true;
        // PlayerInputReader の状態を更新して、ゲーム内の操作を無効化する
        inputReader.IsMenuOpen = true;
        // ゲームを一時停止
        Time.timeScale = 0f;
        // メニューUIを表示
        inventoryMenu.SetActive(true);

        // カーソルとタイマーを初期化
        selectedIndex = 0;
        navigateTimer = 0f;
        isHolding = false;
        currentState = MenuState.ItemGrid;
        // グリッドを更新して表示
        RefreshGrid();
    }

    private void CloseMenu()
    {
        // メニューを閉じるSE再生
        SoundManager.Instance?.PlaySE(closeMenuSE);
        // メニューを閉じる
        isOpen = false;
        // PlayerInputReader の状態を更新して、ゲーム内の操作を有効化する
        inputReader.IsMenuOpen = false;
        // ゲームを再開
        Time.timeScale = 1f;
        // メニューUIを非表示
        inventoryMenu.SetActive(false);
        // ポップアップが開いていた場合は閉じる
        confirmPopup.Hide();
    }

    //==============================
    // グリッドナビゲーション
    //==============================

    private void HandleGridNavigation()
    {
        // 入力ベクトルを取得
        Vector2 nav = inputReader.MenuNavigateInput;
        // 入力が閾値未満ならリピートタイマーをリセットして終了
        if (nav.magnitude < 0.5f)
        {
            // 入力がない場合はリピートタイマーをリセット
            navigateTimer = 0f;
            // 長押し状態もリセット
            isHolding = false;
            // 以降の処理をスキップ
            return;
        }
        // リピートタイマーを減算
        navigateTimer -= Time.unscaledDeltaTime;
        // タイマーが残っている場合は移動処理をスキップ
        if (navigateTimer > 0f) return;

        // 斜め入力は上下優先で4方向に限定
        if (Mathf.Abs(nav.y) >= Mathf.Abs(nav.x))
            NavigateGrid(0, nav.y > 0 ? -1 : 1);  // 上 / 下
        else
            NavigateGrid(nav.x > 0 ? 1 : -1, 0);  // 右 / 左
        // リピートタイマーをリセット
        navigateTimer = isHolding ? repeatDelay : firstRepeatDelay;
        // 長押し状態を記録
        isHolding = true;
    }

    /// <summary>
    /// グリッドカーソルを移動する。
    /// 端・空スロットは移動しない（止まる）。
    /// </summary>
    private void NavigateGrid(int colDelta, int rowDelta)
    {
        // 現在の行・列を計算
        int currentRow = selectedIndex / Columns;
        // 現在の列を計算
        int currentCol = selectedIndex % Columns;
        // 移動先の行を計算（範囲外はClampで補正）
        int newRow = Mathf.Clamp(currentRow + rowDelta, 0, Rows - 1);
        // 移動先の列を計算（範囲外はClampで補正）
        int newCol = Mathf.Clamp(currentCol + colDelta, 0, Columns - 1);
        // 移動先のインデックスを計算
        int newIndex = newRow * Columns + newCol;

        // 移動先にスロットがある場合のみ移動
        if (newIndex < itemSlots.Count)
        {
            // カーソル移動SE再生
            SoundManager.Instance?.PlaySE(cursorMoveSE);
            // カーソル位置を更新
            selectedIndex = newIndex;
            // スロットの選択状態を更新
            UpdateSlotSelection();
            // 説明テキストを更新
            UpdateDescription();
        }
    }

    /// <summary>
    /// スロットの選択状態を更新する。
    /// </summary>
    private void UpdateSlotSelection()
    {
        // 選択中のスロットだけを選択状態にする
        for (int i = 0; i < itemSlots.Count; i++)
            itemSlots[i].SetSelected(i == selectedIndex);
    }

    //==============================
    // グリッド決定・ポップアップ表示
    //==============================

    private void OnGridConfirm()
    {
        // 所持アイテムがない場合は何もしない
        if (itemSlots.Count == 0) return;
        // 選択中のアイテムを取得
        InventoryItem item = inventoryManager.Inventory[selectedIndex];
        // 装備中かどうかを判定
        bool isEquipped = inventoryManager.EquippedItem == item.itemData;
        // ポップアップのメッセージを作成
        string message = isEquipped
            ? $"{item.itemData.ItemName} を外しますか？"
            : $"{item.itemData.ItemName} を装備しますか？";
        // ポップアップ表示中は水平入力の前フレーム値をリセットして、押した瞬間だけ反応するようにする
        prevPopupHorizontal = 0f;
        // メニュー状態をポップアップ表示中に切り替え
        currentState = MenuState.ConfirmPopup;

        // ポップアップ表示SE再生
        SoundManager.Instance?.PlaySE(popupShowSE);
        // ポップアップを表示して、Yes/Noのコールバックを設定
        confirmPopup.Show(
            message,
            onYesCallback: () => ExecuteEquip(item),
            onNoCallback: () => CloseConfirmPopup()
        );
    }

    /// <summary>
    /// 装備・解除処理を実行する
    /// </summary>
    /// <param name="item"></param>
    private void ExecuteEquip(InventoryItem item)
    {
        // 装備中のアイテムと同じ場合は解除、違う場合は装備する
        if (inventoryManager.EquippedItem == item.itemData)
            inventoryManager.UnequipItem();
        else
            inventoryManager.EquipItem(item.itemData);
        // ポップアップを閉じて、グリッドを更新
        CloseConfirmPopup();
        // グリッドを更新して、装備欄の表示も更新
        RefreshGrid();
    }

    /// <summary>
    /// 装備確認ポップアップを閉じる
    /// </summary>
    private void CloseConfirmPopup()
    {
        // キャンセルSE再生
        SoundManager.Instance?.PlaySE(cancelSE);

        confirmPopup.Hide();
        currentState = MenuState.ItemGrid;
    }

    //==============================
    // ポップアップナビゲーション
    //==============================

    private void HandlePopupNavigation()
    {
        // 水平入力を取得
        float h = inputReader.MenuNavigateInput.x;

        // 閾値を越えた瞬間だけ切り替え（リピートなし）
        if (h > 0.5f && prevPopupHorizontal <= 0.5f)
        {
            SoundManager.Instance?.PlaySE(cursorMoveSE);
            confirmPopup.MoveSelection(1f);
        }
        else if (h < -0.5f && prevPopupHorizontal >= -0.5f)
        {
            SoundManager.Instance?.PlaySE(cursorMoveSE);
            confirmPopup.MoveSelection(-1f);
        }
        // 前フレームの水平入力を更新
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
            // 所持アイテムを取得
            InventoryItem item = inventoryManager.Inventory[i];
            // スロットを生成して初期化
            GameObject slotObj = Instantiate(itemSlotPrefab, itemGridContainer);
            // InventoryItemSlot コンポーネントを取得
            InventoryItemSlot slot = slotObj.GetComponent<InventoryItemSlot>();
            // 装備中かどうかを判定
            bool isEquipped = inventoryManager.EquippedItem == item.itemData;
            // スロットをセットアップして、クリック時のコールバックを設定
            slot.Setup(item, isEquipped, OnSlotClicked);
            // 生成済みスロットリストに追加
            itemSlots.Add(slot);
        }

        // アイテム減少時にカーソルが範囲外にならないよう補正
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, itemSlots.Count - 1));
        // スロットの選択状態・説明テキスト・装備欄表示を更新
        UpdateSlotSelection();
        UpdateDescription();
        UpdateEquipmentDisplay();
    }

    /// <summary>マウスクリック時：クリックされたスロットにカーソルを合わせてポップアップを開く</summary>
    private void OnSlotClicked(InventoryItem item)
    {
        // クリックされたアイテムのインデックスを取得
        int idx = inventoryManager.Inventory.IndexOf(item);
        // インデックスが見つからない場合は何もしない
        if (idx < 0) return;
        // カーソル位置を更新して、スロットの選択状態・説明テキストを更新
        selectedIndex = idx;
        // スロットの選択状態・説明テキストを更新
        UpdateSlotSelection();
        // 説明テキストを更新
        UpdateDescription();
        // グリッド決定処理を呼び出してポップアップを開く
        OnGridConfirm();
    }

    //==============================
    // 各表示更新
    //==============================

    private void UpdateDescription()
    {
        // 説明テキストが設定されていない場合は何もしない
        if (descriptionText == null) return;
        // 選択中のアイテムが存在する場合は説明を表示、存在しない場合は空文字列にする
        descriptionText.text = (itemSlots.Count > 0 && selectedIndex < inventoryManager.Inventory.Count)
            ? inventoryManager.Inventory[selectedIndex].itemData.Description
            : string.Empty;
    }

    // 装備欄の表示を更新する
    private void UpdateEquipmentDisplay()
    {
        // 装備中アイテムを取得
        ItemData equipped = inventoryManager.EquippedItem;

        // アイコンとテキストを更新
        if (equippedLabel != null)
            equippedLabel.text = equipped != null ? equipped.ItemName : "装備無し";
        // アイコンは装備中アイテムがあれば表示、なければ非表示
        if (equippedIcon != null)
        {
            equippedIcon.sprite = equipped?.ItemIcon;
            equippedIcon.enabled = equipped?.ItemIcon != null;
            // アイコンが表示される場合はアスペクト比を維持する設定にする
            equippedIcon.preserveAspect = true;
        }
    }

}
