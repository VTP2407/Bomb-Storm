using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;

public class GridManager : MonoBehaviour
{
    private GridCellData[,] gridCellsDatas;
    public int width = 10;
    public int height = 10;
    private Vector3 offset;

    [Header("Tilemap Details")]
    [SerializeField] private Tilemap tilemapBackground;
    [SerializeField] private Tilemap tilemapObject;
    [SerializeField] private TileBase normalTile;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private TileBase explosionTile;
    [SerializeField] private TileBase footTile;
    [SerializeField] private TileBase BoxTile;
    [SerializeField] private TileBase buffTile;

    private void Awake()
    {
        //Initialize();
    }

    private void SetPositionOffset()
    {
        offset = new Vector3((width - 1) / 2f, (height - 1) / 2f);

        tilemapObject.transform.position = -offset - new Vector3(0.5f, 0.5f, 0f);
        tilemapBackground.transform.position = -offset - new Vector3(0.5f, 0.5f, 0f);
    }

    public void BuildMapFromServer(int serverWidth, int serverHeight, int[] mapData)
    {
        this.width = serverWidth;
        this.height = serverHeight;

        SetPositionOffset();

        gridCellsDatas = new GridCellData[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2Int gridPosition = new Vector2Int(i, j);
                int index = j * width + i;
                int tileType = mapData[index];

                if (tilemapBackground != null)
                {
                    SetTile(tilemapBackground, gridPosition, normalTile);
                }

                if (tileType == 1) // 1 = Tường cứng (Wall)
                {
                    gridCellsDatas[i, j] = new GridCellData(gridPosition, ObjectType.Wall);
                    SetTile(tilemapObject, gridPosition, wallTile);
                }
                else if (tileType == 2) // 2 = Hộp gỗ (Box)
                {
                    gridCellsDatas[i, j] = new GridCellData(gridPosition, ObjectType.Box);
                    SetTile(tilemapObject, gridPosition, BoxTile);
                }
                else // 0 = Ô trống (Empty)
                {
                    gridCellsDatas[i, j] = new GridCellData(gridPosition, ObjectType.Empty);
                }
            }
        }
    }

    private void SetTile(Tilemap tilemap, Vector2Int position, TileBase tile)
    {
        Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
        tilemap.SetTile(tilePosition, tile);
    }

    public void ResetObjectTile(Vector2Int position)
    {
        SetTile(tilemapObject, position, null);
    }

    public bool CanMove(Vector2Int position)
    {
        if (gridCellsDatas == null || !InsideGrid(position))
            return false;

        ObjectType objectType = GetObjectTypeOnCell(position);

        if (objectType == ObjectType.Wall)
            return false;
        if (objectType == ObjectType.Box)
            return false;
        if (objectType == ObjectType.Bomb)
            return false;
        return true;
    }

    public ObjectType GetObjectTypeOnCell(Vector2Int position)
    {
        return gridCellsDatas[position.x, position.y].objectType;
    }

    public bool InsideGrid(Vector2Int position)
    {
        if (position.x < 0 || position.x > width - 1)
            return false;
        if (position.y < 0 || position.y > height - 1)
            return false;
        return true;
    }

    public Vector3 GetWordPosition(Vector2Int position)
    {
        return new Vector3(position.x, position.y) - offset;
    }

    public void CreateExplodeEffect(Vector2Int position, bool canPierceWall)
    {
        if (!InsideGrid(position)) return;

        ObjectType preType = GetObjectTypeOnCell(position);
        if (preType == ObjectType.Wall && !canPierceWall) return;

        SetTile(tilemapObject, position, explosionTile);
        StartCoroutine(ResetTileCo(position, GetObjectTypeOnCell(position)));
    }

    private IEnumerator ResetTileCo(Vector2Int position, ObjectType preType)
    {
        yield return new WaitForSeconds(0.2f);

        if (preType == ObjectType.Wall)
        {
            SetTile(tilemapObject, position, wallTile);
        }
        else
        {
            ResetObjectTile(position);
            SetGridObjectType(position, ObjectType.Empty);
        }
    }

    public void SetGridObjectType(Vector2Int position, ObjectType objectType)
    {
        //Debug.Log("Set data for grid cell: " + position + " Type: " + preType);
        gridCellsDatas[position.x, position.y].objectType = objectType;
    }

    public void SetPlayerOnCellData(Vector2Int position, Player player)
    {
        //Debug.Log("Set player in cell: "+position);
        gridCellsDatas[position.x, position.y].player = player;
        SetTile(tilemapBackground, position, footTile);
    }

    public void RemovePlayerOnCell(Vector2Int position, Player player)
    {
        //Debug.Log("Remove player in cell: "+position);
        gridCellsDatas[position.x, position.y].player = null;
        SetTile(tilemapBackground, position, normalTile);
    }

    public void ResetBombOnCell(Vector2Int position)
    {
        gridCellsDatas[position.x, position.y].bomb = null;
        SetGridObjectType(position, ObjectType.Empty);
    }
}
