using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "RuneDice/Rune/Relic Base")]
public class RelickillData : SerializedScriptableObject
{
    public RelicType TargetType;

    public int stackApply;

    public int valueApply = 1;

    public virtual void Execute()
    {
        switch (TargetType)
        {

        }
    }
}


public static class PhantomStrikeRelicSkillData
{
    public static void Execute()
    {
    }
}

public enum RelicType
{
    PhantomStrike


}
