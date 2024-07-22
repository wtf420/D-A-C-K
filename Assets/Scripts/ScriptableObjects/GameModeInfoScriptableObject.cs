using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GameModeData
{
    public string GameModeName;
    public GameMode GameModePrefab;
    public List<UnityEngine.Object> AvailableScene;
}

[CreateAssetMenu(fileName = "GameModeInfo", menuName = "ScriptableObjects/GameModeInfo", order = 1)]
public class GameModeInfoScriptableObject : ScriptableObject
{
    public List<GameModeData> gameModeDatas;
}

