using UnityEngine;

public class InventoryDiceButton : MonoBehaviour
{
    public void OnClick()
    {
        UIManager.Instance.ShowInventoryDice();
        //  GameplayManager.Instance.SetState(EGamePlayState.Running);

    }
}