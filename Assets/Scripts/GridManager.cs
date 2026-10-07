using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    private GridCell[,] gridCells;
    [SerializeField] private GameObject cellPrefab;


    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        gridCells = new GridCell[width, height];

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
        GameObject cellObject = CreateGridCell();
        cellObject.transform.SetParent(this.transform);
        GridCell gridCell = cellObject.GetComponent<GridCell>();
        gridCells[i, j] = gridCell;

        Vector2Int position = new Vector2Int(i, j);
        gridCell.gridCellData = new GridCellData(position);

        gridCell.textMeshPro.text = $"[{position.x},{position.y}]";
        Vector3 offset = new Vector3((width - 1) / 2f, (height - 1) / 2f);
        gridCell.transform.position = new Vector3(position.x, position.y) - offset;

        if (i == 0 || j == 0 || i == width - 1 || j == height - 1)
        {
            gridCell.gridCellData.objectType = GridObjectType.Wall;
            gridCell.sr.color = Color.red;
        }
    }

    private GameObject CreateGridCell()
    {
        return Instantiate(cellPrefab, transform.position, Quaternion.identity);
    }

    public bool CanMove(Vector2Int position)
    {
        GridObjectType objectType = GetCellObjectType(position);
        if (objectType == GridObjectType.Wall)
            return false;
        if (objectType == GridObjectType.Box)
            return false;
        if (objectType == GridObjectType.Bomb)
            return false;
        if (!InsideGrid(position))
            return false;
        return true;
    }

    public GridObjectType GetCellObjectType(Vector2Int position)
    {
        return gridCells[position.x, position.y].gridCellData.objectType;
    }

    private bool InsideGrid(Vector2Int position)
    {
        if (position.x < 0 || position.x >= width - 1)
            return false;
        if (position.y < 0 || position.y >= height - 1)
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
                if (gridCells[i, j].gridCellData.objectType == GridObjectType.Empty)
                    emptyGridList.Add(new Vector2Int(i, j));
            }
        }

        return emptyGridList[Random.Range(0, emptyGridList.Count)];
    }

    public Vector3 GetGridPosition(Vector2Int position)
    {
        return gridCells[position.x, position.y].transform.position;
    }
}
