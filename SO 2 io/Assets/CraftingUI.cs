using UnityEngine;
using System.Collections.Generic;

public class CraftingUI : MonoBehaviour
{
    [Header("������ ������������")]
    [Tooltip("�������� ��'��� ����� ������, ���� ���� ���������/����������.")]
    public GameObject craftingPanel;
    [Tooltip("������ ��� ��������/�������� ���� ������.")]
    public KeyCode toggleKey = KeyCode.C;

    [Header("UI �������� (��������� � ���������)")]
    [Tooltip("������, �� ������ ������������ �� ����� � ���������.")]
    public Transform recipeSlotsParent;
    [Tooltip("������ ������ ����� (������) ��� �������.")]
    public GameObject craftingSlotPrefab;

    private List<CraftingSlotUI> currentSlots = new List<CraftingSlotUI>();

    void Start()
    {
        if (craftingPanel == null || recipeSlotsParent == null || craftingSlotPrefab == null)
        {
            Debug.LogError("CraftingUI: �� �� ���� ��������� � ���������! (Crafting Panel, Recipe Slots Parent, Crafting Slot Prefab)");
            return;
        }

        // ϳ��������� �� ���� ���� ���������, ��� ���������� ������
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.onInventoryChangedCallback += UpdateAllSlotsStatus;
        }

        // ���������� � ������ ���� ��� �����
        InitializeCraftingUI();
        craftingPanel.SetActive(false);
    }

    void Update()
    {
        // ³��������/��������� ���� �� ���������� ������
        if (Input.GetKeyDown(toggleKey))
        {
            bool isActive = !craftingPanel.activeSelf;
            craftingPanel.SetActive(isActive);

            // ���� ���� �����������, ��������� ������ �������
            if (isActive)
            {
                UpdateAllSlotsStatus();
            }
        }
    }

    void OnDestroy()
    {
        // ������� ���������� �� ��䳿, ��� �������� �������
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.onInventoryChangedCallback -= UpdateAllSlotsStatus;
        }
    }

    /// <summary>
    /// ������� ����� ��� ��� ��������� �������.
    /// </summary>
    void InitializeCraftingUI()
    {
        foreach (Transform child in recipeSlotsParent)
        {
            Destroy(child.gameObject);
        }
        currentSlots.Clear();

        CraftingManager craftingManager = CraftingManager.instance;
        if (craftingManager != null && craftingManager.recipes != null)
        {
            foreach (CraftingRecipe recipe in craftingManager.recipes)
            {
                GameObject slotGO = Instantiate(craftingSlotPrefab, recipeSlotsParent);
                CraftingSlotUI slotUI = slotGO.GetComponent<CraftingSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(recipe);
                    currentSlots.Add(slotUI);
                }
            }
        }
    }

    /// <summary>
    /// ������� ������ (�����/�� ����� ���������) ��� ��� �����.
    /// </summary>
    void UpdateAllSlotsStatus()
    {
        if (craftingPanel.activeSelf == false) return; // �� ����������, ���� ���� �������

        foreach (CraftingSlotUI slot in currentSlots)
        {
            slot.UpdateCraftableStatus();
        }
    }
}
