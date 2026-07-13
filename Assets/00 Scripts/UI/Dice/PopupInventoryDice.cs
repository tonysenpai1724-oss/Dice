public class PopupInventoryDice : UIBase
{
    public InventoryUIController inventoryUIController;

    public void CloseAll()
    {
        if (HideDetailPopups())
            return;

        if (inventoryUIController != null)
            inventoryUIController.Hide();
        else
            Hide();
    }

    bool HideDetailPopups()
    {
        if (UIManager.Instance == null || UIManager.Instance.lstOpenningUI == null)
            return false;

        bool hasDetailPopup = false;

        foreach (UIBase ui in UIManager.Instance.lstOpenningUI.ToArray())
        {
            if (!(ui is PopupDiceDetail) && !(ui is PopupRuneDetail))
                continue;

            ui.Hide();
            hasDetailPopup = true;
        }

        return hasDetailPopup;
    }
}
