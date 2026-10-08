using System;
using TMPro;
using UnityEngine;
[Serializable]
public class GridCellData 
{
    public Vector2Int position;
    public GridObjectType objectType;
    public Player player;

    public GridCellData(Vector2Int position)
    {
        this.position = position;
        objectType = GridObjectType.Empty;
    }

    public GridCellData(Vector2Int position, GridObjectType objectType)
    {
        this.position = position;
        this.objectType = objectType;
    }
}

public enum GridObjectType
{
    Empty,
    Wall,
    Box,
    Bomb
}
