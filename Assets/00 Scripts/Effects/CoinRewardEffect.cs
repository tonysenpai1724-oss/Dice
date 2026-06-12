using System.Collections.Generic;

public static class CoinRewardEffect
{
    public static void Apply(int amount)
    {
        if (amount <= 0 || IPlayerResource.Instance == null)
            return;

        IPlayerResource.Instance.AddResource(
            new List<GameResource>
            {
                new CommonResource(ECommonResource.Token, amount)
            },
            EResourceFrom.TimeReward
        );
    }
}
