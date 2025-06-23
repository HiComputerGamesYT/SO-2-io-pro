using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // �������! ��� ���������� Drag & Drop
using System.Collections.Generic;

// �������� ���� InventoryUI, �� � ����������� MonoBehaviour
public class InventoryUI : MonoBehaviour
{
    [Header("UI ��������")]
    [Tooltip("������ (RectTransform), ��� ������ �� ����� ���������. � �� ���� ��������� GridLayoutGroup.")]
    public RectTransform slotsParent; // ����������� ��'��� ��� �����
    [Tooltip("������ ������ ����� ��������� (UI GameObject) � ����������� �������� InventorySlotUI.")]
    public GameObject inventorySlotPrefab;
    [Tooltip("������ ��� ������������� �����.")]
    public Button unlockSlotButton;
    [Tooltip("��������� ��'��� ��� ����������� ������� ������������� �����.")]
    public Text unlockCostText;

    // --- �������� ��� Drag & Drop ---
    [Header("Drag & Drop UI")]
    [Tooltip("���������� (Image), ��� ���� �������������� �� ��������. ������� ���� ������� GameObject.")]
    public Image draggedItemImage; // �� ������� GameObject Image, ���� ���� ��������������
    [Tooltip("RectTransform ����� ���������, ��� ����������� �������� �������� ���� ��� (���������).")]
    public RectTransform inventoryPanelRectTransform; // ��� �������� �������� �� ������ ���������

    private InventoryManager inventoryManager; // ��������� �� InventoryManager
    private List<GameObject> uiSlots = new List<GameObject>(); // ������ ��������� UI �����

    [HideInInspector] public InventorySlot draggedSlotData; // ��� �����, �� ������������ (�������� ��� InventorySlotUI)
    [HideInInspector] public int draggedSlotIndex; // ������ �����, �� ������������ (�������� ��� InventorySlotUI)
    private Canvas canvas; // ��������� �� �������� Canvas ��� ���������� �������������� Drag & Drop ��������

    // ����� Awake ����������� ��� ����������� ��'����
    void Awake()
    {
        canvas = GetComponentInParent<Canvas>(); // �������� ��������� �� Canvas, �� ����� ����������� UI
        if (canvas == null)
        {
            Debug.LogError("InventoryUI: Canvas �� �������� �� ����������� ��'���. Drag&Drop ���� ��������� ����������.");
        }
    }

    // ����� Start ����������� ����� ������ ���������� �����
    void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>(); // ��������� InventoryManager � ����
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryUI: InventoryManager �� �������� � ����. ����������� ��������� �� �����������.");
            enabled = false; // �������� ������, ���� �������� �������
            return;
        }

        // ����������, �� ��������� �������� ��� Drag & Drop
        if (draggedItemImage == null)
        {
            Debug.LogError("InventoryUI: draggedItemImage �� ����������. ���������� Drag&Drop �� �����������.");
        }
        else
        {
            draggedItemImage.gameObject.SetActive(false); // �������� ��������� ������� �������������
        }

        if (inventoryPanelRectTransform == null)
        {
            Debug.LogError("InventoryUI: inventoryPanelRectTransform �� ����������. ������� ��������� �������� � ��� �� �����������.");
        }

        // ϳ��������� �� ���� ���� ��������� �� InventoryManager
        inventoryManager.onInventoryChangedCallback += UpdateUI;

        // ���������� UI ���������
        InitializeUI();
        UpdateUI(); // ����� ��������� UI ��� �������

        // ������������ ������ �������������
        if (unlockSlotButton != null)
        {
            unlockSlotButton.onClick.AddListener(OnUnlockSlotButtonClicked); // ������ �������� ���������� ������
            if (unlockCostText != null)
            {
                unlockCostText.text = "������������ ���� �� " + inventoryManager.unlockSlotCost; // ³��������� �������
            }
        }
    }

    // �������� ������� ����� ��������� (������� GameObject'� �����)
    void InitializeUI()
    {
        // ������� �� ������� ����� ����� ���������� ����� (��� ��������� ����������)
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        uiSlots.Clear();

        // ��������� ������� ����� �������� �� ������ ���������
        for (int i = 0; i < inventoryManager.inventorySize; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, slotsParent); // ��������� ���� � �������
            uiSlots.Add(slotGO); // ������ ���� �� ������ UI �����

            // ����������� InventorySlotUI ������ �� �������� �������� ���������
            // ������� InventorySlotUI ����� ������� ������, �� ���� ������ �������� ��� ������
            InventorySlotUI slotUIComponent = slotGO.GetComponent<InventorySlotUI>();
            if (slotUIComponent == null)
            {
                slotUIComponent = slotGO.AddComponent<InventorySlotUI>(); // ������, ���� ���� ���� � ������
            }
            slotUIComponent.inventorySlot = inventoryManager.GetSlot(i); // �������� ����� ��� �����
            slotUIComponent.slotIndex = i; // �������� ������ �����
            slotUIComponent.parentUI = this; // �������� ��������� �� ��� InventoryUI

            slotUIComponent.UpdateSlotDisplay(); // ��������� ���������� ������ �����
        }
    }

    // ������� ���� UI ��������� (����������� ��� ��� ���������)
    void UpdateUI()
    {
        for (int i = 0; i < uiSlots.Count; i++)
        {
            InventorySlotUI slotUIComponent = uiSlots[i].GetComponent<InventorySlotUI>();
            if (slotUIComponent != null)
            {
                slotUIComponent.inventorySlot = inventoryManager.GetSlot(i); // ��������� ����� ���
                slotUIComponent.UpdateSlotDisplay(); // ��������� �������� �����������
            }
        }
        CheckUnlockButtonState(); // ��������� ���� ������ �������������
    }

    // ����������� InventorySlotUI ��� ������� ������������� ��������
    public void StartDraggingItem(InventorySlot slotData, int slotIndex, Sprite itemSprite)
    {
        draggedSlotData = slotData; // �������� ��� �����, �� ������������
        draggedSlotIndex = slotIndex; // �������� ������ �����, �� ������������

        draggedItemImage.sprite = itemSprite; // ������������ ������ �������� �� ���������� �������������
        draggedItemImage.gameObject.SetActive(true); // �������� ����������
        draggedItemImage.SetNativeSize(); // ������������ ����������� ����� �������
        draggedItemImage.transform.SetParent(canvas.transform); // ��������� �� Canvas, ��� ���� ������������ ������ ��� ����� �������� UI
    }

    // ����������� InventorySlotUI ��� ��������� ������������� ��������
    public void EndDraggingItem()
    {
        draggedItemImage.gameObject.SetActive(false); // ��������� ���������� �������������
        draggedItemImage.transform.SetParent(transform); // ��������� �� ���� (��� �������)
        draggedItemImage.rectTransform.localPosition = Vector3.zero; // ������� �������

        // ����������, �� ��� ������� �������� ���� ������ ��������� (��� �������� ����)
        // EventSystem.current.currentSelectedGameObject == null ��������, �� ������ ��� ������� UI ���������
        // RectTransformUtility.RectangleContainsScreenPoint ��������, �� ������ ����������� �������� RectTransform
        if (EventSystem.current.currentSelectedGameObject == null ||
            (inventoryPanelRectTransform != null && !RectTransformUtility.RectangleContainsScreenPoint(inventoryPanelRectTransform, Input.mousePosition, Camera.main)))
        {
            DropItemInWorld(draggedSlotData, draggedSlotIndex); // ��������� ����� ��������� � ���
        }

        draggedSlotData = null; // ������� ��� ��� ������������� ����
        draggedSlotIndex = -1; // ������� ������
        UpdateUI(); // ��������� ���� UI �� �������, ���� ���� ��������
    }

    // �������� �������� �������� � ������ ����� �� ����� (����������� InventorySlotUI)
    public void HandleItemDrop(int sourceIndex, int targetIndex)
    {
        if (sourceIndex == targetIndex) return; // ���� ������� ������� �� ��� ����� ����, ����� �� ������

        // ��������� ����� ����� ����� � InventoryManager
        inventoryManager.SwapSlots(sourceIndex, targetIndex);
        UpdateUI(); // ��������� UI ���� �����
    }

    // ����� ��� ��������� �������� � ��� (���� �� ����� ���������)
    private void DropItemInWorld(InventorySlot itemToDrop, int sourceSlotIndex)
    {
        if (itemToDrop == null || itemToDrop.IsEmpty()) return;

        // ��� ��� ���� ��ò�� ������ ��������� GAMEOBJECT �������� � �²Ҳ
        // ���������: Instantiate(itemToDrop.item.worldPrefab, playerTransform.position, Quaternion.identity);
        Debug.Log($"�������� {itemToDrop.quantity} x {itemToDrop.item.itemName} � ��� � ����� {sourceSlotIndex}.");

        // ������� ���� � ��������
        inventoryManager.GetSlot(sourceSlotIndex).ClearSlot();
        inventoryManager.onInventoryChangedCallback?.Invoke(); // ����������� UI ��� ����
    }


    // �������� ���������� ������ ������������� �����
    public void OnUnlockSlotButtonClicked()
    {
        for (int i = 0; i < inventoryManager.slots.Count; i++)
        {
            if (inventoryManager.slots[i].isLocked) // ��������� ������ ������������ ����
            {
                // ��� ����� ������ �������� �� �������� ������� � ������ ��� �������������
                if (inventoryManager.UnlockSlot(i)) // �������� ������������ ����
                {
                    UpdateUI(); // ��������� UI
                    CheckUnlockButtonState(); // ��������� ���� ������ �������������
                    return; // �������� ���� �������� �������������
                }
                break; // �������� ���� ������ ������������ ������ ���������
            }
        }
        Debug.Log("���� ������������ ����� ��� ������������� ��� ����������� �������.");
    }

    // �������� ���� ������ ������������� (�� � �� ���������� �����)
    private void CheckUnlockButtonState()
    {
        if (unlockSlotButton != null)
        {
            bool hasLockedSlots = false;
            foreach (var slot in inventoryManager.slots)
            {
                if (slot.isLocked)
                {
                    hasLockedSlots = true;
                    break;
                }
            }
            unlockSlotButton.interactable = hasLockedSlots; // �������� ������, ���� ���� ������������ �����
        }
    }
}
