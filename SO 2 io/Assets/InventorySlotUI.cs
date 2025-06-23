using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Важливо! Для інтерфейсів Drag & Drop
using System.Collections.Generic; // Потрібно для List, хоча в цьому класі не використовується напряму

// Цей скрипт буде прикріплений до кожного окремого слота інвентарю (до префаба InventorySlotPrefab)
// Він обробляє логіку Drag & Drop для конкретного слота.
public class InventorySlotUI : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public Image itemIcon; // Иконка предмета в слоте
    public Text quantityText; // Текст количества предмета
    public GameObject lockedOverlay; // Оверлей для заблокированных слотов
    public Image slotBackground; // Фон слота

    [HideInInspector] public InventorySlot inventorySlot; // Ссылка на логический слот из InventoryManager
    [HideInInspector] public int slotIndex; // Индекс этого слота в списке слотов InventoryManager
    [HideInInspector] public InventoryUI parentUI; // Ссылка на родительский InventoryUI

    private RectTransform rectTransform; // RectTransform этого UI элемента
    private Canvas canvas; // Ссылка на родительский Canvas
    private CanvasGroup canvasGroup; // Для управления прозрачностью и взаимодействием во время перетаскивания

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>(); // Находим родительский Canvas
        canvasGroup = GetComponent<CanvasGroup>(); // Получаем CanvasGroup или добавляем, если его нет
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Получение ссылок на дочерние элементы (если не назначены в Инспекторе)
        // Используем ?. (null-conditional operator) для безопасного доступа, если объекты не найдены
        if (itemIcon == null) itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
        if (quantityText == null) quantityText = transform.Find("QuantityText")?.GetComponent<Text>();
        if (lockedOverlay == null) lockedOverlay = transform.Find("LockedOverlay")?.gameObject;
        if (slotBackground == null) slotBackground = GetComponent<Image>();
    }

    // Обновление визуального отображения слота на основе данных inventorySlot
    public void UpdateSlotDisplay()
    {
        if (inventorySlot.isLocked) // Если слот заблокирован
        {
            if (lockedOverlay != null) lockedOverlay.SetActive(true); // Показываем оверлей замка
            if (itemIcon != null) itemIcon.enabled = false; // Скрываем иконку
            if (quantityText != null) quantityText.enabled = false; // Скрываем текст количества
            if (slotBackground != null) slotBackground.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Более темный цвет фона
        }
        else // Если слот разблокирован
        {
            if (lockedOverlay != null) lockedOverlay.SetActive(false); // Скрываем оверлей замка
            if (slotBackground != null) slotBackground.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Обычный цвет фона

            if (inventorySlot.item != null && inventorySlot.quantity > 0) // Если в слоте есть предмет
            {
                if (itemIcon != null)
                {
                    itemIcon.sprite = inventorySlot.item.icon; // Устанавливаем иконку предмета
                    itemIcon.enabled = true; // Показываем иконку
                }
                if (quantityText != null)
                {
                    // Показываем количество, только если оно больше 1
                    quantityText.text = inventorySlot.quantity > 1 ? inventorySlot.quantity.ToString() : "";
                    quantityText.enabled = true; // Показываем текст количества
                }
            }
            else // Если слот пуст
            {
                if (itemIcon != null) itemIcon.enabled = false; // Скрываем иконку
                if (quantityText != null) quantityText.enabled = false; // Скрываем текст количества
            }
        }
    }

    // --- Реализация интерфейсов Drag & Drop ---

    // Вызывается, когда курсор нажимает на элемент
    public void OnPointerDown(PointerEventData eventData)
    {
        // Необходимо для корректной работы Drag & Drop
    }

    // Вызывается при начале перетаскивания элемента
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Не разрешаем перетаскивать, если слот пуст или заблокирован
        if (inventorySlot.IsEmpty() || inventorySlot.isLocked)
        {
            eventData.pointerDrag = null; // Сбрасываем ссылку на перетаскиваемый элемент
            return;
        }

        // Сообщаем родительскому InventoryUI о начале перетаскивания
        parentUI.StartDraggingItem(inventorySlot, slotIndex, itemIcon.sprite);
        canvasGroup.alpha = 0.6f; // Сделать визуальный слот более прозрачным во время перетаскивания
        canvasGroup.blocksRaycasts = false; // Разрешить лучам проходить сквозь этот элемент к целевому слоту
    }

    // Вызывается во время перетаскивания элемента
    public void OnDrag(PointerEventData eventData)
    {
        // Перемещаем визуальный элемент, который перетаскивается, за курсором
        if (parentUI.draggedItemImage != null && canvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPos);
            parentUI.draggedItemImage.rectTransform.localPosition = localPointerPos;
        }
    }

    // Вызывается после завершения перетаскивания элемента
    public void OnEndDrag(PointerEventData eventData)
    {
        parentUI.EndDraggingItem(); // Сообщаем родительскому InventoryUI о завершении перетаскивания
        canvasGroup.alpha = 1f; // Вернуть полную непрозрачность визуального слота
        canvasGroup.blocksRaycasts = true; // Снова блокировать лучи
    }

    // Вызывается, когда другой элемент UI сбрасывается на этот элемент
    public void OnDrop(PointerEventData eventData)
    {
        // Если сбросили на себя или не перетаскивали предмет
        if (eventData.pointerDrag == null || eventData.pointerDrag == gameObject) return;

        // Получаем компонент InventorySlotUI из исходного слота
        InventorySlotUI sourceSlotUI = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        if (sourceSlotUI != null)
        {
            // Обрабатываем сброс в родительском InventoryUI
            parentUI.HandleItemDrop(sourceSlotUI.slotIndex, this.slotIndex);
        }
    }
}
