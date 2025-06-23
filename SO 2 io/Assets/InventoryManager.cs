using UnityEngine;
using System.Collections.Generic;

// ���� ��� ������������� ����� ���������
// [System.Serializable] �������� ������ ��� ���� � ��������� Unity
[System.Serializable]
public class InventorySlot
{
    public Item item;       // ������� � ����� ���� (��������� �� Item ScriptableObject)
    public int quantity;    // ʳ������ �������� � ����� ����
    public bool isLocked;   // �� ������������ ����

    // ����������� ��� �����, �������� �������, �� �� ������������ ��� ��������
    public InventorySlot(bool locked = false)
    {
        item = null;
        quantity = 0;
        isLocked = locked;
    }

    // ����� ��� ��������� �������� �� �����
    // ������� �������, ��� �� ���������� � ��� ���� (�������)
    public int AddItem(Item newItem, int amount)
    {
        // ���� ���� ������� ��� ��� ������ ��� ����� �������
        if (item == null || item == newItem)
        {
            item = newItem; // ���������� ����� ������� (���� ���� ��� �������)
            int spaceLeft = item.maxStack - quantity; // ������ ���� ���������� � ����
            int amountToAdd = Mathf.Min(amount, spaceLeft); // ������ ����� ������
            quantity += amountToAdd; // ������ �������
            return amount - amountToAdd; // ��������� �������, ���� �� ������� ������
        }
        return amount; // ���� ������� �� �������, ������� ��� ������ �������
    }

    // ����� ��� �������� ����� (������� � ������� �����������)
    public void ClearSlot()
    {
        item = null;
        quantity = 0;
    }

    // ��������, �� ���� �������
    public bool IsEmpty()
    {
        return item == null || quantity == 0;
    }

    // ��������, �� ����� ������ � ���� ������ ������� (� ����������� ���� ��������, ������������� ����� �� ����������)
    public bool CanAddItem(Item newItem)
    {
        // ���� ���� �������� �������, ����:
        // 1. ³� ������� ���
        // 2. ³� ������ ����� ����� ������� � �� ���������� �� ������������� �����
        // � ��� ����� ���� �� ������������
        return (IsEmpty() || (item == newItem && quantity < item.maxStack)) && !isLocked;
    }
}


// �������� ���� InventoryManager, �� � ����������� MonoBehaviour
public class InventoryManager : MonoBehaviour
{
    [Tooltip("�������� ������� ����� ���������.")]
    public int inventorySize = 20;
    [Tooltip("ʳ������ ��������� ������������ ����� (�� ���� ���������).")]
    public int lockedSlotsCount = 10;
    [Tooltip("������� (������) ������������� ������ ����� (���������, �������).")]
    public int unlockSlotCost = 10;

    public List<InventorySlot> slots;

    // ����, ��� ����������� ������� ����, ���� �������� ���������
    // UI �� ���� ������� ������ ���������� �� �� ����, ��� ���������
    public delegate void OnInventoryChanged();
    public event OnInventoryChanged onInventoryChangedCallback;

    // ����� Awake ����������� ��� ����������� ��'���� (����� ���� ������ ��������)
    void Awake()
    {
        slots = new List<InventorySlot>();
        for (int i = 0; i < inventorySize; i++)
        {
            // ��������� ��� �����; ������ lockedSlotsCount ����� ������ ����������
            slots.Add(new InventorySlot(i >= (inventorySize - lockedSlotsCount)));
        }
    }

    // ����� ��� ��������� �������� � ��������
    // ������� true, ���� ������� (��� ���� �������) ��� ������ �������
    public bool AddItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0) // �������� ������� �����
        {
            Debug.LogWarning("AddItem: ������� ��� ������� ���������.");
            return false;
        }

        // 1. ���������� ������ �� ��������� ����� (���������� ��������)
        foreach (InventorySlot slot in slots)
        {
            if (slot.item == item && slot.quantity < item.maxStack && !slot.isLocked)
            {
                int remainingAmount = slot.AddItem(item, quantity); // ���������� ������ � ��� ����
                if (remainingAmount == 0) // ���� ��� ����������
                {
                    onInventoryChangedCallback?.Invoke(); // �������� ��� ����
                    return true;
                }
                quantity = remainingAmount; // ��������� �������, ��� ���������� ��� ���������
            }
        }

        // 2. ���������� ������ � ������� ����
        foreach (InventorySlot slot in slots)
        {
            if (slot.IsEmpty() && !slot.isLocked) // ���� ���� ������� � �� ������������
            {
                int remainingAmount = slot.AddItem(item, quantity); // ������
                if (remainingAmount == 0) // ���� ��� ����������
                {
                    onInventoryChangedCallback?.Invoke(); // �������� ��� ����
                    return true;
                }
                quantity = remainingAmount; // ��������� �������, ��� ����������
            }
        }

        // ���� ����� ����, ������� �������� ���������� ��� ���� ����
        Debug.LogWarning($"�������� ���������� ��� ���� ���� ��� {item.itemName} x{quantity}.");
        onInventoryChangedCallback?.Invoke(); // ��� ���� ���������, ��� UI �������� (����� ���� �� �������)
        return false; // �� ������� ������ �� ��������
    }

    // ����� ��� ��������� �������� � ���������
    // ������� true, ���� ������� ��� ������ ��������� (��� ���� �������)
    public bool RemoveItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0) // �������� ������� �����
        {
            Debug.LogWarning("RemoveItem: ������� ��� ������� ���������.");
            return false;
        }

        int totalRemoved = 0; // ˳������� ��� ����������, ������ �������� ���� ��������
        // ��������� ����� � ���������� ������� (���� ���� ��������, ���������, ���� �������� � ���� ���������)
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            InventorySlot slot = slots[i];
            if (slot.item == item && !slot.IsEmpty()) // ���� ���� ������ �������� ������� � �� �������
            {
                int amountToRemove = Mathf.Min(quantity, slot.quantity); // ������ ����� �������� � ����� �����
                slot.quantity -= amountToRemove; // �������� �������
                totalRemoved += amountToRemove; // ������ �� �������� �������� �������
                quantity -= amountToRemove; // �������� �������, ��� �� ������� ��������

                if (slot.quantity <= 0) // ���� ���� ���� �������, ������� ����
                {
                    slot.ClearSlot();
                }

                if (quantity <= 0) break; // ���� ��� ������� ������� ��������, �������� � �����
            }
        }

        if (totalRemoved > 0) // ���� ���� ���� ��������
        {
            onInventoryChangedCallback?.Invoke(); // �������� ��� ����
            return true;
        }
        Debug.LogWarning($"RemoveItem: �� ������� ������ {item.itemName} ��� ��������� ������� ��� ���������.");
        return false; // ͳ���� �� ��������
    }

    // ����� ��� ������������� ����� �� ���� ��������
    public bool UnlockSlot(int slotIndex)
    {
        // ����������, �� ������ ����������� � ����� �������� �����
        if (slotIndex >= 0 && slotIndex < slots.Count)
        {
            if (slots[slotIndex].isLocked) // ���� ���� ����� ������������
            {
                // ��� � ����������� ����� ������ ����� �������� ������� ������ ��� �������������
                // ���������:
                // if (Player.instance.HasEnoughResource("Gold", unlockSlotCost))
                // {
                //     Player.instance.RemoveResource("Gold", unlockSlotCost);
                slots[slotIndex].isLocked = false; // ������������ ����
                onInventoryChangedCallback?.Invoke(); // �������� ��� ����
                Debug.Log($"���� {slotIndex} ������������.");
                return true;
                // }
                // else
                // {
                //     Debug.Log("����������� ������� ��� ������������� �����.");
                //     return false;
                // }
            }
            else
            {
                Debug.Log($"���� {slotIndex} ��� ������������.");
            }
        }
        Debug.LogWarning($"UnlockSlot: ����������� ������ ����� ({slotIndex}).");
        return false; // ���� �� ������������
    }

    // ����� ��� ����� ������ ����� ���� �����
    // ����� �������� ����� ����������, ���� �������� �������
    public void SwapSlots(int indexA, int indexB)
    {
        // ����������, �� ������� ����������� � ����� �������� �����
        if (indexA < 0 || indexA >= slots.Count || indexB < 0 || indexB >= slots.Count)
        {
            Debug.LogWarning($"SwapSlots: ������ ������� ����� �� ������ ��������. IndexA: {indexA}, IndexB: {indexB}, InventorySize: {slots.Count}.");
            return;
        }

        InventorySlot slotA = slots[indexA];
        InventorySlot slotB = slots[indexB];

        // �� ���������� ������������ � ������������ ����, ���� �� �� ������� � �� � ��� �����, �� �������
        // �� ������, �� ����� �������� ������� �� ������������� �����, ��� �� ����� �������� ���� � ������������ ����, ���� �� ��� �� ������.
        if (slotB.isLocked && !slotB.IsEmpty() && indexA != indexB)
        {
            Debug.Log("�� ����� ����������� ������� � ������������ ����, ���� �� �� �������.");
            onInventoryChangedCallback?.Invoke(); // ��������� UI, ���� ������������� �� ��������
            return;
        }

        // ����� ����������: ���� �������� ������� � �������� ���� (slotB) �� ������
        if (slotA.item != null && slotA.item == slotB.item && slotB.quantity < slotB.item.maxStack && !slotB.isLocked)
        {
            int amountToStack = Mathf.Min(slotA.quantity, slotB.item.maxStack - slotB.quantity); // ������ ����� ������ �� �����
            slotB.quantity += amountToStack; // ������ �� ��������� �����
            slotA.quantity -= amountToStack; // ������� � ��������� ����

            if (slotA.quantity <= 0) // ���� �������� ���� ���� �������, ������� ����
            {
                slotA.ClearSlot();
            }
        }
        else // ������ ���� ������ (���� �������� ��� ��� ���������� ���������)
        {
            // ��������� �������� ���� ����� B
            Item tempItem = slotB.item;
            int tempQuantity = slotB.quantity;

            // ��������� ���� ����� A � ���� B
            slotB.item = slotA.item;
            slotB.quantity = slotA.quantity;

            // ��������� ��������� ���������� ���� � ���� A
            slotA.item = tempItem;
            slotA.quantity = tempQuantity;
        }

        onInventoryChangedCallback?.Invoke(); // ����������� UI ��� ����
        Debug.Log($"�������/��������� ����� {indexA} �� {indexB}.");
    }

    // ����� ��� ��������� ����� ����� �� ��������
    public InventorySlot GetSlot(int index)
    {
        if (index >= 0 && index < slots.Count)
        {
            return slots[index];
        }
        Debug.LogWarning($"GetSlot: ����������� ������ ����� ({index}).");
        return null;
    }
}
