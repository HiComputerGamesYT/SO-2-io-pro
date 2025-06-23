using UnityEngine;

 Атрибут CreateAssetMenu дозволяє створювати об'єкти цього типу через меню Assets/Create
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Tooltip("Унікальна назва предмета.")]
    public string itemName = "New Item";
    [Tooltip("Опис предмета.")]
    [TextArea(3, 5)]
    public string description = "Опис предмета.";
    [Tooltip("Іконка, що відображається в інвентарі.")]
    public Sprite icon = null;
    [Tooltip("Максимальна кількість предмета в одному слоті інвентарю.")]
    [Range(1, 99)] // Обмеження до 99, як ви просили
    public int maxStack = 1;
}
