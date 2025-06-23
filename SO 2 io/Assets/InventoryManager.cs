using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;

        slots = new List<InventorySlot>();
        for (int i = 0; i < inventorySize; i++)
        {
            slots.Add(new InventorySlot(i >= (inventorySize - lockedSlotsCount)));
        }
    }

    [Header("Налаштування Інвентарю")]
    public int inventorySize = 20;
    public int lockedSlotsCount = 10;
    public GameObject worldItemPrefab;

    [HideInInspector]
    public int activeSlotIndex = 0;

    private List<InventorySlot> slots;

    public delegate void OnInventoryChanged();
    public event OnInventoryChanged onInventoryChangedCallback;

    public void SetActiveSlot(int index)
    {
        if (index >= 0 && index < inventorySize && !slots[index].isLocked)
        {
            activeSlotIndex = index;
            onInventoryChangedCallback?.Invoke();
        }
    }

    public Item GetActiveItem()
    {
        if (slots.Count > activeSlotIndex)
        {
            return slots[activeSlotIndex].item;
        }
        return null;
    }

    public bool HasItem(Item item, int quantity)
    {
        if (item == null) return false;
        return slots.Where(s => !s.isLocked && s.item == item).Sum(s => s.quantity) >= quantity;
    }

    public void RemoveItem(Item itemToRemove, int quantityToRemove)
    {
        if (itemToRemove == null || quantityToRemove <= 0) return;
        int amountLeft = quantityToRemove;
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (amountLeft <= 0) break;
            var slot = slots[i];
            if (!slot.isLocked && slot.item == itemToRemove)
            {
                int canRemove = Mathf.Min(amountLeft, slot.quantity);
                slot.quantity -= canRemove;
                amountLeft -= canRemove;
                if (slot.quantity <= 0) slot.Clear();
            }
        }
        onInventoryChangedCallback?.Invoke();
    }

    public bool AddItem(Item itemToAdd, int amount)
    {
        if (itemToAdd == null || amount <= 0) return false;
        int remainingAmount = amount;
        foreach (var slot in slots.Where(s => !s.isLocked && s.item == itemToAdd && s.quantity < itemToAdd.maxStack))
        {
            int canAdd = itemToAdd.maxStack - slot.quantity;
            int toAdd = Mathf.Min(remainingAmount, canAdd);
            slot.quantity += toAdd;
            remainingAmount -= toAdd;
            if (remainingAmount <= 0) break;
        }
        if (remainingAmount > 0)
        {
            foreach (var slot in slots.Where(s => !s.isLocked && s.item == null))
            {
                slot.item = itemToAdd;
                int toAdd = Mathf.Min(remainingAmount, itemToAdd.maxStack);
                slot.quantity += toAdd;
                remainingAmount -= toAdd;
                if (remainingAmount <= 0) break;
            }
        }
        onInventoryChangedCallback?.Invoke();
        return remainingAmount == 0;
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count || indexB < 0 || indexB >= slots.Count || slots[indexA].isLocked || slots[indexB].isLocked) return;

        Item tempItem = slots[indexA].item;
        int tempQuantity = slots[indexA].quantity;

        slots[indexA].item = slots[indexB].item;
        slots[indexA].quantity = slots[indexB].quantity;

        slots[indexB].item = tempItem;
        slots[indexB].quantity = tempQuantity;

        onInventoryChangedCallback?.Invoke();
    }

    public void DropItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex].item == null) return;
        if (worldItemPrefab == null)
        {
            Debug.LogError("WorldItemPrefab не призначено в InventoryManager!");
            return;
        }
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 playerPos = player.transform.position;
        Vector3 spawnPos = playerPos + (mousePos - playerPos).normalized * 1.5f;

        GameObject itemObject = Instantiate(worldItemPrefab, spawnPos, Quaternion.identity);

        WorldItem worldItem = itemObject.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.itemData = slots[slotIndex].item;
            worldItem.quantity = slots[slotIndex].quantity;
            SpriteRenderer sr = itemObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = slots[slotIndex].item.icon;
                sr.sortingLayerName = "Items";
            }
        }

        slots[slotIndex].Clear();
        onInventoryChangedCallback?.Invoke();
    }

    public InventorySlot GetSlot(int index)
    {
        return (index >= 0 && index < slots.Count) ? slots[index] : null;
    }
}