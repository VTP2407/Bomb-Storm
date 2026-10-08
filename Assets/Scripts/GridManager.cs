using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Collections;

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


    private void Awake()
    {
        SetPositionOffset();

        Initialize();
    }

    private void SetPositionOffset()
    {
        offset = new Vector3((width - 1) / 2f, (height - 1) / 2f);

        tilemapObject.transform.position = -offset;
        tilemapBackground.transform.position = -offset;

        tilemapObject.transform.position = -offset - new Vector3(0.5f, 0.5f, 0f);
        tilemapBackground.transform.position = -offset - new Vector3(0.5f, 0.5f, 0f);
    }

    private void Initialize()
    {
        gridCellsDatas = new GridCellData[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                InitGridCell(i, j);
            }
        }
    }

    private void InitGridCell(int i, int j)
    {
        Vector2Int gridPosition = new Vector2Int(i, j);


        bool isWall = i == 0 || j == 0 || i == width - 1 || j == height - 1;

        if (isWall)
        {
            gridCellsDatas[i, j] = new GridCellData(gridPosition, GridObjectType.Wall);
            SetTile(tilemapObject, gridPosition, wallTile);
        }
        else
        {
            gridCellsDatas[i, j] = new GridCellData(gridPosition);
        }

        if (tilemapBackground != null)
        {
            SetTile(tilemapBackground, gridPosition, normalTile);
        }
    }

    public void SetTile(Tilemap tilemap, Vector2Int position, TileBase tile)
    {
        Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
        tilemap.SetTile(tilePosition, tile);
    }

    public bool CanMove(Vector2Int position)
    {
        if (!InsideGrid(position))
            return false;

        GridObjectType objectType = GetCellObjectType(position);

        if (objectType == GridObjectType.Wall)
            return false;
        if (objectType == GridObjectType.Box)
            return false;
        if (objectType == GridObjectType.Bomb)
            return false;
        return true;
    }

    public GridObjectType GetCellObjectType(Vector2Int position)
    {
        return gridCellsDatas[position.x, position.y].objectType;
    }

    private bool InsideGrid(Vector2Int position)
    {
        if (position.x < 0 || position.x > width - 1)
            return false;
        if (position.y < 0 || position.y > height - 1)
            return false;
        return true;
    }

    public Vector2Int GetRandomPosition()
    {
        List<Vector2Int> emptyGridList = new List<Vector2Int>();

        for (int i = 1; i < width - 1; i++)
        {
            for (int j = 1; j < height - 1; j++)
            {
                if (gridCellsDatas[i, j].objectType == GridObjectType.Empty)
                    emptyGridList.Add(new Vector2Int(i, j));
            }
        }

        return emptyGridList[Random.Range(0, emptyGridList.Count)];
    }

    public Vector3 GetGridPosition(Vector2Int position)
    {
        return new Vector3(position.x, position.y) - offset;
    }

    public void CreateExplodeEffect(Vector2Int position, bool canPierceWall)
    {
        if (!CanExplode(position, canPierceWall))
            return;

        SetTile(tilemapObject, position, explosionTile);
        StartCoroutine(ResetTileCo(position, GetCellObjectType(position)));

        Player player = GetPlayerOnCell(position);
        if (player != null)
            player.TakeDamage();
    }

    private IEnumerator ResetTileCo(Vector2Int position, GridObjectType objectType)
    {
        yield return new WaitForSeconds(0.2f);
        if (objectType == GridObjectType.Wall)
        {
            SetTile(tilemapObject, position, wallTile);
        }
        else
        {
            SetTile(tilemapObject, position, null);
        }
    }

    private bool CanExplode(Vector2Int position, bool canPierceWall)
    {
        if (!InsideGrid(position))
            return false;

        if (GetCellObjectType(position) == GridObjectType.Wall && !canPierceWall)
            return true;

        return true;
    }

    public bool CanPlace(Vector2Int position)
    {
        if (GetCellObjectType(position) == GridObjectType.Bomb)
            return false;
        return true;
    }

    public void SetGridCellData(Vector2Int position, GridObjectType objectType)
    {
        //Debug.Log("Set data for grid cell: " + position + " Type: " + objectType);
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

    private Player GetPlayerOnCell(Vector2Int position) => gridCellsDatas[position.x, position.y].player;
}
