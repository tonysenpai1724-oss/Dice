using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Equipment/Equipment Database")]
public class EquipmentDatabaseSO : ScriptableObject
{
    public List<BaseEquiment> equipments = new();

    public BaseEquiment FindById(string equipmentId)
    {
        if (string.IsNullOrEmpty(equipmentId) || equipments == null)
            return null;

        for (int i = 0; i < equipments.Count; i++)
        {
            BaseEquiment equipment = equipments[i];
            if (equipment == null)
                continue;

            if (equipment.equipmentId == equipmentId)
                return equipment;
        }

        return null;
    }
}
