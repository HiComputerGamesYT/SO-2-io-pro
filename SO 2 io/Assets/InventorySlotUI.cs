using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // �������! ��� ���������� Drag & Drop
using System.Collections.Generic; // ������� ��� List, ���� � ����� ���� �� ��������������� �������

// ��� ������ ���� ����������� �� ������� �������� ����� ��������� (�� ������� InventorySlotPrefab)
// ³� �������� ����� Drag & Drop ��� ����������� �����.
public class InventorySlotUI : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public Image itemIcon; // ������ �������� � �����
    public Text quantityText; // ����� ���������� ��������
    public GameObject lockedOverlay; // ������� ��� ��������������� ������
    public Image slotBackground; // ��� �����

    [HideInInspector] public InventorySlot inventorySlot; // ������ �� ���������� ���� �� InventoryManager
    [HideInInspector] public int slotIndex; // ������ ����� ����� � ������ ������ InventoryManager
    [HideInInspector] public InventoryUI parentUI; // ������ �� ������������ InventoryUI

    private RectTransform rectTransform; // RectTransform ����� UI ��������
    private Canvas canvas; // ������ �� ������������ Canvas
    private CanvasGroup canvasGroup; // ��� ���������� ������������� � ��������������� �� ����� ��������������

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>(); // ������� ������������ Canvas
        canvasGroup = GetComponent<CanvasGroup>(); // �������� CanvasGroup ��� ���������, ���� ��� ���
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // ��������� ������ �� �������� �������� (���� �� ��������� � ����������)
        // ���������� ?. (null-conditional operator) ��� ����������� �������, ���� ������� �� �������
        if (itemIcon == null) itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
        if (quantityText == null) quantityText = transform.Find("QuantityText")?.GetComponent<Text>();
        if (lockedOverlay == null) lockedOverlay = transform.Find("LockedOverlay")?.gameObject;
        if (slotBackground == null) slotBackground = GetComponent<Image>();
    }

    // ���������� ����������� ����������� ����� �� ������ ������ inventorySlot
    public void UpdateSlotDisplay()
    {
        if (inventorySlot.isLocked) // ���� ���� ������������
        {
            if (lockedOverlay != null) lockedOverlay.SetActive(true); // ���������� ������� �����
            if (itemIcon != null) itemIcon.enabled = false; // �������� ������
            if (quantityText != null) quantityText.enabled = false; // �������� ����� ����������
            if (slotBackground != null) slotBackground.color = new Color(0.2f, 0.2f, 0.2f, 1f); // ����� ������ ���� ����
        }
        else // ���� ���� �������������
        {
            if (lockedOverlay != null) lockedOverlay.SetActive(false); // �������� ������� �����
            if (slotBackground != null) slotBackground.color = new Color(0.5f, 0.5f, 0.5f, 1f); // ������� ���� ����

            if (inventorySlot.item != null && inventorySlot.quantity > 0) // ���� � ����� ���� �������
            {
                if (itemIcon != null)
                {
                    itemIcon.sprite = inventorySlot.item.icon; // ������������� ������ ��������
                    itemIcon.enabled = true; // ���������� ������
                }
                if (quantityText != null)
                {
                    // ���������� ����������, ������ ���� ��� ������ 1
                    quantityText.text = inventorySlot.quantity > 1 ? inventorySlot.quantity.ToString() : "";
                    quantityText.enabled = true; // ���������� ����� ����������
                }
            }
            else // ���� ���� ����
            {
                if (itemIcon != null) itemIcon.enabled = false; // �������� ������
                if (quantityText != null) quantityText.enabled = false; // �������� ����� ����������
            }
        }
    }

    // --- ���������� ����������� Drag & Drop ---

    // ����������, ����� ������ �������� �� �������
    public void OnPointerDown(PointerEventData eventData)
    {
        // ���������� ��� ���������� ������ Drag & Drop
    }

    // ���������� ��� ������ �������������� ��������
    public void OnBeginDrag(PointerEventData eventData)
    {
        // �� ��������� �������������, ���� ���� ���� ��� ������������
        if (inventorySlot.IsEmpty() || inventorySlot.isLocked)
        {
            eventData.pointerDrag = null; // ���������� ������ �� ��������������� �������
            return;
        }

        // �������� ������������� InventoryUI � ������ ��������������
        parentUI.StartDraggingItem(inventorySlot, slotIndex, itemIcon.sprite);
        canvasGroup.alpha = 0.6f; // ������� ���������� ���� ����� ���������� �� ����� ��������������
        canvasGroup.blocksRaycasts = false; // ��������� ����� ��������� ������ ���� ������� � �������� �����
    }

    // ���������� �� ����� �������������� ��������
    public void OnDrag(PointerEventData eventData)
    {
        // ���������� ���������� �������, ������� ���������������, �� ��������
        if (parentUI.draggedItemImage != null && canvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPos);
            parentUI.draggedItemImage.rectTransform.localPosition = localPointerPos;
        }
    }

    // ���������� ����� ���������� �������������� ��������
    public void OnEndDrag(PointerEventData eventData)
    {
        parentUI.EndDraggingItem(); // �������� ������������� InventoryUI � ���������� ��������������
        canvasGroup.alpha = 1f; // ������� ������ �������������� ����������� �����
        canvasGroup.blocksRaycasts = true; // ����� ����������� ����
    }

    // ����������, ����� ������ ������� UI ������������ �� ���� �������
    public void OnDrop(PointerEventData eventData)
    {
        // ���� �������� �� ���� ��� �� ������������� �������
        if (eventData.pointerDrag == null || eventData.pointerDrag == gameObject) return;

        // �������� ��������� InventorySlotUI �� ��������� �����
        InventorySlotUI sourceSlotUI = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        if (sourceSlotUI != null)
        {
            // ������������ ����� � ������������ InventoryUI
            parentUI.HandleItemDrop(sourceSlotUI.slotIndex, this.slotIndex);
        }
    }
}
