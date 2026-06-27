using System;

[Serializable]
public class InventoryItem
{
    /// <summary>
    /// アイテムデータ
    /// </summary>
    public ItemData itemData;

    /// <summary>
    /// 所持数
    /// </summary>
    public int count;
}