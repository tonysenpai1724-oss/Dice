using UnityEngine;

public class HeroSelectionSession : MonoBehaviour
{
    public static HeroSelectionSession Instance;

    [SerializeField] HeroData selectedHeroData;

    public HeroData SelectedHeroData => selectedHeroData;

    void Awake()
    {

        Instance = this;
        DontDestroyOnLoad(this);
    }

    public static HeroSelectionSession GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject sessionObject = new GameObject("HeroSelectionSession");
        return sessionObject.AddComponent<HeroSelectionSession>();
    }

    public void SetSelectedHero(HeroData heroData)
    {
        if (heroData == null)
            return;

        selectedHeroData = heroData;
    }

    public HeroData GetSelectedHero()
    {
        return selectedHeroData;
    }

    public bool HasSelectedHero()
    {
        return selectedHeroData != null;
    }

    public void ClearSelection()
    {
        selectedHeroData = null;
    }
}
