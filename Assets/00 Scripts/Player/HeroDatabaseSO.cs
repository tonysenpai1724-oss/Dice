using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Hero Database")]
public class HeroDatabaseSO : ScriptableObject
{
    public List<HeroData> heroes = new();

    public HeroData GetHero(HeroType type)
    {
        if (heroes == null)
            return null;

        for (int i = 0; i < heroes.Count; i++)
        {
            HeroData hero = heroes[i];
            if (hero != null && hero.type == type)
                return hero;
        }

        return null;
    }

    public HeroData GetHeroByName(string heroName)
    {
        if (string.IsNullOrEmpty(heroName) || heroes == null)
            return null;

        for (int i = 0; i < heroes.Count; i++)
        {
            HeroData hero = heroes[i];
            if (hero != null && hero.name == heroName)
                return hero;
        }

        return null;
    }

    public HeroData GetDefaultHero()
    {
        if (heroes == null || heroes.Count == 0)
            return null;

        for (int i = 0; i < heroes.Count; i++)
        {
            if (heroes[i] != null)
                return heroes[i];
        }

        return null;
    }
}
