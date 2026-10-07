using System;
using TMPro;
using UnityEngine;
[Serializable]
public class GridCellData 
{
    public Vector2Int position;
    public GridObjectType objectType;

    public GridCellData(Vector2Int position)
    {
        this.position = position;
        objectType = GridObjectType.Empty;
    }
}

public enum GridObjectType
{
    Empty,
    Wall,
    Box,
    Bomb, 
    Player
}
