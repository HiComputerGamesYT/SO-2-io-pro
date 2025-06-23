using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI Елементи (призначте в префабі)")]
    public Image itemIcon;
    public Text quantityText;
    public Image highlight;

    private static InventorySlotUI draggedSlot;
    private static bool dropHappened;

    private Image draggedItemIcon;
    private CanvasGroup canvasGroup;
    private CanvasGroup draggedIconCanvasGroup;

    [HideInInspector]
    public int slotIndex;
    private InventorySlot currentInventorySlot;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        InventoryUI inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            draggedItemIcon = inventoryUI.draggedItemIcon;
            if (draggedItemIcon != null)
            {
                draggedIconCanvasGroup = draggedItemIcon.GetComponent<CanvasGroup>();
                if (draggedIconCanvasGroup == null)
                {
                    draggedIconCanvasGroup = draggedItemIcon.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }
    }

    public void UpdateSlot(InventorySlot slotData)
    {
        currentInventorySlot = slotData;
        bool hasItem = slotData != null && slotData.item != null;

        if (itemIcon != null)
        {
            itemIcon.enabled = hasItem;
            if (hasItem)
            {
                itemIcon.sprite = slotData.item.icon;
                itemIcon.color = Color.white;
            }
        }

        if (quantityText != null)
        {
            bool showQuantity = hasItem && slotData.quantity > 1;
            quantityText.enabled = showQuantity;
            if (showQuantity)
            {
                quantityText.text = slotData.quantity.ToString();
            }
        }

        if (highlight != null)
        {
            highlight.enabled = (InventoryManager.instance.activeSlotIndex == slotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentInventorySlot == null || currentInventorySlot.item == null) return;

        draggedSlot = this;
        dropHappened = false;

        canvasGroup.blocksRaycasts = false;

        if (draggedItemIcon != null)
        {
            draggedItemIcon.sprite = itemIcon.sprite;
            draggedItemIcon.enabled = true;
            if (draggedIconCanvasGroup != null)
            {
                draggedIconCanvasGroup.blocksRaycasts = false;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedItemIcon != null)
        {
            draggedItemIcon.rectTransform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (!dropHappened && draggedSlot != null)
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                InventoryManager.instance.DropItem(draggedSlot.slotIndex);
            }
        }

        if (draggedItemIcon != null)
        {
            draggedItemIcon.enabled = false;
            if (draggedIconCanvasGroup != null)
            {
                draggedIconCanvasGroup.blocksRaycasts = true;
            }
        }
        draggedSlot = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot != null && draggedSlot != this)
        {
            InventoryManager.instance.SwapSlots(draggedSlot.slotIndex, this.slotIndex);
            dropHappened = true;
        }
    }
}