using System;

public enum EGameState
{
    Loading,
    Home,
    Gameplay,
}

public enum EGamePlayState
{
    Cinematic,
    Running,
    GameOver,
    Pause
}
public enum EGameType
{
    Campaign,
    Endless,
}
public enum EAnimationEffectType
{
    None = 0,
}

public enum EItemIngame
{
}

public enum EAchievementType
{
    Login,
    LevelPlay,
    LevelWin,
    LevelCompleted,
    EndlessPlay,
    ReachEnlessStage,
    WinEndlessStage,
    SpendCoin,
    SpendEnergy,
    SpinLuckyWheel,
}

public enum ESfx
{
    ButtonSfx = 0,
    RewardedSfx = 1,
    ReceiveCoinSfx = 2,
    WinSfx = 3,
    LoseSfx = 4,
}
public enum ERarity
{
    Common,
    Uncommon,
    Great,
    Rare,
    Epic,
    Legend,
    Legendary,
    Mythical,
}
public enum EStats
{
    Null,
    Damage,
    Max_Hp,
    Hp_Recovery,
    HP_Steal,
    Crit_Rate,
    Crit_Damage,
    Move_Speed,
    Cooldown,
    Exp,
    Item_Absorb_Range,
    Effect_Area,
    Effect_Duration,
    Damage_Reviced_Reduction,
    Luck,
    Projectile_Number,
    Push_Back_Force,
    Revival,
    Dodge,
    Max_Shield,
    Shield_Recovery,
    Bullet_Speed,
    Coin_Value,
    [Obsolete("Dont Show")] Start_Item,
    [Obsolete("Dont Show")] Start_Weapon,
    Reroll,
    Start_Skill,
    Berserk,
    ShieldDamage,
    LifeGainPerKill,
    Insurance_Shield,
    Heal_On_Level_Up,
    Reselect_Skill,
}

public enum EPassiveSkill
{
    Cooldown_Increase,
    Bullet_Speed_Increase,
    Bullet_Damage_Increase,
    Max_Hp_Increase,
    Exp_Increase,
    Item_Absorb_Range_Increase,
    Effect_Area_Increase,
    Effect_Duration_Increase,
    Coin_Value_Increase,
    Hp_Recovery_Increase,
    Damage_Reviced_Reduction_Increase,
    Luck_Increase,
    Projectile_Number_Increase,
    Move_Speed_Increase,
    Crit_Incease,
    Push_Back_Force_Increase,
    Revival,
}

public enum ECommonResource
{
    Coin,
    Gem,
    Energy,
    Exp,
    ActivePoint,
}
public enum EBlueprint
{

    Blueprint2,
    Blueprint3,
    Blueprint4,
    Blueprint5
}
public enum DiceCommonResource
{


}
public enum EChestType
{
    CommonKey,
    Gold,
    LegendaryKey,
}
public enum EVirtualResource
{

}
public enum EExpireableResource
{

}
public enum EContentActiveResource
{

}
public enum ERewardState
{
    Progress,
    CanClaim,
    Claimed,
}
public enum EIAPPackType
{
    Starter,
    Trainee,
    Carpenter,
    Professional,
    Engineer,
    Master,
    Legend,
    Gem1,
    Gem2,
    Gem3,
    Gem4,
    Gem5,
    Gem6,
}
public enum EResourceFrom
{
    Hack,
    GameDrop,
    LuckySpin,
    TimeReward,
    AdsReward,
    UpgradeGun,
    NormalChest,
    DailyLogin,
    IAP,
    SpendIngame,
    ReviveIngame,
    SpendOpenChest,
    SpendTeam,
    MergeEquipment,
    ServerGenerate,
    MintInInVentory,
    DailyShop,
    DailyQuest,
    InviteFriend,
    PartnerReferal,
    Leaderboard
}
public enum EButtonType
{
    Common,
    ResourceConsume,
    ActionConsume,
}
public enum EButtonColor
{
    Green,
    Yellow,
    Blue,
    Gray,
}
public enum EDiscount
{
    E0,
    E10,
    E20,
    E30,
    E50,
    E75,
}
public enum EShopPurchaseType
{
    Free,
    Ads,
    Coin,
    Gem,
    IAP,
}
public enum EStatusState
{
    None,
    disconnected,
    connected,
    connecting,
    reconnecting,
}
public enum EHoleType
{
    None,
    Empty,
    Locked,
    HasPin,
}
public enum EBarMaterial
{
    Bar1, Bar2, Bar3, Bar4, Bar5, Bar6, Bar7
}
public enum EBackBoardMaterial
{
    Board1, Board2, Board3, Board4, Board5, Board6, Board7
}
public enum ESliderShape
{
    Rectangle,
    Circle,
    Right_Triangle,
    O_Shape,
    L_Shape,
    T_Shape,
    U_Shape,
    Plus_Shape,
    Holed_Rectangle,
    H_Shape,
}
public enum ERandomRotation
{
    E0 = 0,
    E27 = 27,
    E45 = 45,
    E63 = 63,
    E90 = 90,
    E117 = 117,
    E135 = 135,
    E152 = 152,
    E180 = 180,
    E225 = 225,
    E270 = 270,
    E315 = 315,
}
public enum ENodeType
{
    Empty,
    Wall,
    Start,
    End,
    Monster,
    Treasure,
}
public enum EDirection
{
    Up,
    Down,
    Left,
    Right,
}
public enum EGameSetting
{
    Music,
    Sound,
    Vibration,
}
public enum EShopType
{
    IAP,
    Coin,
    Gem,
    Booster,
}
public enum EShopCoin
{
    ShopCoin1,
    ShopCoin2,
    ShopCoin3,
}
public enum EShopEnergy
{
    ShopEnergy1,
    ShopEnergy2,
    ShopEnergy3,
}
public enum EBuildType
{
    Publish,
    Dev,
    Local,
}
public enum EPlatform
{
    Telegram,
    Privy,
    Android,
}
public enum EUIResourceResolution
{
    x100,
    x200,
}

