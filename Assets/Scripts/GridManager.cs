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
    [SerializeField] private int boxAmount = 10;
    [SerializeField] private float chanceFindBuff = 0.5f;


    private void Awake()
    {
        //Initialize();
    }

    private void SetPositionOffset()
    {
        offset = new Vector3((width - 1) / 2f, (height - 1) / 2f);

        tilemapObject.transform.position = -offset;
        tilemapBackground.transform.position = -offset;

        tilemapObject.transform.position = -offset - new Vector3(0.5f, 0.5f, 0f);
        tilemapBackground.transform.position = -offset - new Vector3(0.5f, 0.5f, 0f);
    }

    private void SetRandomBox()
    {
        int d = 0;
        int attempts = width * height * 10;
        while (d < boxAmount && attempts > 0)
        {
            int x = Random.Range(1, width - 1);
            int y = Random.Range(1, height - 1);
            Vector2Int position = new Vector2Int(x, y);

            if (CanSetBox(position) == false)
                continue;

            SetTile(tilemapObject, position, BoxTile);
            SetGridObjectType(position, ObjectType.Box);
            d++;
            attempts--;
        }
    }

    private bool CanSetBox(Vector2Int position)
    {
        if (GetObjectTypeOnCell(position) != ObjectType.Empty)
            return false;
        if (GetPlayerOnCell(position) != null)
            return false;
        return true;
    }

    public void BuildMapFromServer(int serverWidth, int serverHeight, int[] mapData)
    {
        this.width = serverWidth;
        this.height = serverHeight;

        SetPositionOffset();

        gridCellsDatas = new GridCellData[width, height];

        for(int i = 0; i < width; i++)
        {
            for(int j =0; j < height; j++)
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

    private void Initialize()
    {
        SetPositionOffset();
        gridCellsDatas = new GridCellData[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                InitGridCell(i, j);
            }
        }

        SetRandomBox();
    }

    private void InitGridCell(int i, int j)
    {
        Vector2Int gridPosition = new Vector2Int(i, j);


        bool isWall = i == 0 || j == 0 || i == width - 1 || j == height - 1;

        if (isWall)
        {
            gridCellsDatas[i, j] = new GridCellData(gridPosition, ObjectType.Wall);
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
        if (!InsideGrid(position))
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

    public Vector2Int GetRandomGridPosition()
    {
        List<Vector2Int> emptyGridList = new List<Vector2Int>();

        for (int i = 1; i < width - 1; i++)
        {
            for (int j = 1; j < height - 1; j++)
            {
                if (gridCellsDatas[i, j].objectType == ObjectType.Empty)
                    emptyGridList.Add(new Vector2Int(i, j));
            }
        }

        return emptyGridList[Random.Range(0, emptyGridList.Count)];
    }

    public Vector3 GetWordPosition(Vector2Int position)
    {
        return new Vector3(position.x, position.y) - offset;
    }

    public void CreateExplodeEffect(Vector2Int position, bool canPierceWall)
    {
        if (!CanExplode(position, canPierceWall))
            return;

        SetTile(tilemapObject, position, explosionTile);
        StartCoroutine(ResetTileCo(position, GetObjectTypeOnCell(position)));

        Player player = GetPlayerOnCell(position);
        if (player != null)
            player.TakeDamage();
    }

    private IEnumerator ResetTileCo(Vector2Int position, ObjectType objectType)
    {
        if (objectType == ObjectType.Bomb)
        {
            Bomb bomb = GetBombOnCell(position);
            if (!bomb.wasExplode)
            {
                bomb.Explode();
            }

        }

        yield return new WaitForSeconds(0.2f);

        if (objectType == ObjectType.Wall)
        {
            SetTile(tilemapObject, position, wallTile);
        }
        else if (objectType == ObjectType.Empty || objectType == ObjectType.Buff)
        {
            SetTile(tilemapObject, position, null);
        }
        else if (objectType == ObjectType.Bomb)
        {
            ResetObjectTile(position);
            SetGridObjectType(position, ObjectType.Empty);
        }
        else if (objectType == ObjectType.Box)
        {
            if (FindBuff())
            {
                SetTile(tilemapObject, position, buffTile);
                SetGridObjectType(position, ObjectType.Buff);
            }
            else
            {
                ResetObjectTile(position);
                SetGridObjectType(position, ObjectType.Empty);
            }
        }
    }

    private bool CanExplode(Vector2Int position, bool canPierceWall)
    {
        if (!InsideGrid(position))
            return false;

        if (GetObjectTypeOnCell(position) == ObjectType.Wall && !canPierceWall)
            return true;

        return true;
    }

    public bool CanPlace(Vector2Int position)
    {
        if (GetObjectTypeOnCell(position) == ObjectType.Bomb)
            return false;
        return true;
    }

    public void SetGridObjectType(Vector2Int position, ObjectType objectType)
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

    public void SetBombOnCell(Vector2Int position, Bomb bomb)
    {
        gridCellsDatas[position.x, position.y].bomb = bomb;
        SetGridObjectType(position, ObjectType.Bomb);
    }

    public void ResetBombOnCell(Vector2Int position)
    {
        gridCellsDatas[position.x, position.y].bomb = null;
        SetGridObjectType(position, ObjectType.Empty);
    }

    private Player GetPlayerOnCell(Vector2Int position) => gridCellsDatas[position.x, position.y].player;
    private Bomb GetBombOnCell(Vector2Int position) => gridCellsDatas[position.x, position.y].bomb;
    private bool FindBuff() => Random.Range(0f, 1f) < chanceFindBuff;
}
