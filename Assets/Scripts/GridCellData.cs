using System;
using TMPro;
using UnityEngine;
[Serializable]
public class GridCellData 
{
    public Vector2Int position;
    public ObjectType objectType;
    public Player player;
    public Bomb bomb;

    public GridCellData(Vector2Int position)
    {
        this.position = position;
        objectType = ObjectType.Empty;
    }

    public GridCellData(Vector2Int position, ObjectType objectType)
    {
        this.position = position;
        this.objectType = objectType;
    }
}

public enum ObjectType
{
    Empty,
    Wall,
    Box,
    Bomb,
    Buff
}
