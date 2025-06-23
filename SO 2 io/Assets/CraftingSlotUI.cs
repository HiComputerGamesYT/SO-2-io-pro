using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class CraftingSlotUI : MonoBehaviour
{
    [Header("UI �������� (��������� � ������)")]
    public Image outputItemIcon;
    public TextMeshProUGUI outputItemName;

    private CraftingRecipe currentRecipe;
    private Button craftButton;

    void Awake()
    {
        craftButton = GetComponent<Button>();
        if (craftButton != null)
        {
            craftButton.onClick.AddListener(OnCraftButtonClicked);
        }
    }

    /// <summary>
    /// ��������� ��� ���� ��� ����������� ����������� �������.
    /// </summary>
    public void Setup(CraftingRecipe recipe)
    {
        currentRecipe = recipe;

        if (recipe == null || recipe.outputItem == null) return;

        if (outputItemIcon != null)
        {
            outputItemIcon.sprite = recipe.outputItem.icon;
            outputItemIcon.enabled = true;
        }

        if (outputItemName != null)
        {
            outputItemName.text = recipe.outputItem.itemName;
        }

        // ��������� ������ ������ (�� ����� ���������)
        UpdateCraftableStatus();
    }

    /// <summary>
    /// ��������, �� ����� ��������� �������, � ����� ���� ������.
    /// </summary>
    public void UpdateCraftableStatus()
    {
        if (craftButton != null && currentRecipe != null && CraftingManager.instance != null)
        {
            // ������ ������ (��)�������� ������� �� �������� �������
            bool canCraft = CraftingManager.instance.CanCraft(currentRecipe);
            craftButton.interactable = canCraft;
        }
    }

    /// <summary>
    /// ����������� ��� ��������� �� ������ ������.
    /// </summary>
    private void OnCraftButtonClicked()
    {
        if (currentRecipe != null && CraftingManager.instance != null)
        {
            CraftingManager.instance.Craft(currentRecipe);
        }
    }
}
