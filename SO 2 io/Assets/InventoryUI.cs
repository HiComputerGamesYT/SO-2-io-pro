using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Елементи (призначте в інспекторі)")]
    public Transform slotsParent;
    public GameObject inventorySlotPrefab;
    public Image draggedItemIcon;

    private List<InventorySlotUI> uiSlots = new List<InventorySlotUI>();

    void Start()
    {
        if (InventoryManager.instance == null)
        {
            Debug.LogError("InventoryUI: InventoryManager.instance не знайдено!");
            enabled = false;
            return;
        }

        InventoryManager.instance.onInventoryChangedCallback += UpdateUI;

        if (draggedItemIcon != null)
        {
            draggedItemIcon.enabled = false;
        }

        InitializeUI();
    }

    void OnDestroy()
    {
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.onInventoryChangedCallback -= UpdateUI;
        }
    }

    void InitializeUI()
    {
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        uiSlots.Clear();

        for (int i = 0; i < InventoryManager.instance.inventorySize; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, slotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.slotIndex = i;
                uiSlots.Add(slotUI);
            }
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        for (int i = 0; i < uiSlots.Count; i++)
        {
            InventorySlot inventorySlot = InventoryManager.instance.GetSlot(i);
            if (inventorySlot != null)
            {
                uiSlots[i].UpdateSlot(inventorySlot);
            }
        }
    }
}