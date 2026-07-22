using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Relic Database")]
public class RelicDatabaseSO : ScriptableObject
{
    public List<RelicData> relicDatas = new();
}
