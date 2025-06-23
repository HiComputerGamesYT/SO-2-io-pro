using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public Item item;
    public int quantity;
    public bool isLocked;

    public InventorySlot(bool locked = false)
    {
        item = null;
        quantity = 0;
        isLocked = locked;
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}