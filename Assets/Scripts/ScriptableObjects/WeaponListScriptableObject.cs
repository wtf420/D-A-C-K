using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct WeaponData
{
    public string WeaponID;
    public GameObject WeaponPrefab;
}

[CreateAssetMenu(fileName = "Weapons", menuName = "ScriptableObjects/Weapons", order = 0)]
public class WeaponListScriptableObject : ScriptableObject
{
    public List<WeaponData> weaponDatas;
}
