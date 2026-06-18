using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class SelectionUI : MonoBehaviour
{
    public SelectionSystem selectionSystem;
    public Text nameText;
    public Text descriptionText;

    private int lastSelectionCount = -1;
    private ISelectable lastSelectedItem = null;

    void Update()
    {
        if (selectionSystem == null || nameText == null)
            return;

        var selected = selectionSystem.Selected;
        ISelectable currentItem = selected.Count == 1 ? selected.First() : null;

        // Only update if count changed OR if the single selected item changed
        if (selected.Count != lastSelectionCount || currentItem != lastSelectedItem)
        {
            UpdateUI(selected);
            lastSelectionCount = selected.Count;
            lastSelectedItem = currentItem;
        }
    }

    void UpdateUI(IReadOnlyCollection<ISelectable> selected)
    {
        if (selected.Count == 0)
        {
            nameText.text = "";
            if (descriptionText != null) descriptionText.text = "";
        }
        else if (selected.Count == 1)
        {
            ISelectable item = selected.First();
            nameText.text = item.Name;
            if (descriptionText != null) descriptionText.text = item.Description;
        }
        else
        {
            nameText.text = $"{selected.Count} Items Selected";
            if (descriptionText != null) descriptionText.text = "Multiple objects selected.";
        }
    }
}
