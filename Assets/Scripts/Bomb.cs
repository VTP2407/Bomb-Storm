using System.Collections;
using UnityEngine;
using UnityEngine.Windows;

public class Bomb : MonoBehaviour
{
    public Vector2Int gridPosition;
    public Player player;
    private BombStats bombStats;
    public GridManager gridManager;
    private Animator anim;

    public void SetUp(GridManager gridManager, BombStats bombStats)
    {
        this.bombStats = bombStats;
        this.gridManager = gridManager;

        anim.SetFloat("speed", 1f / bombStats.lifeTime);
    }

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        Destroy(gameObject, bombStats.lifeTime);
    }

    private void OnDestroy()
    {
        Explode();
        player.DecreaseBombCount();
    }

    public void SetGridData(Vector2Int position)
    {
        gridPosition = position;
        gridManager.SetGridCellData(position, GridObjectType.Bomb);
    }

    private void Explode()
    {
        Debug.Log("Boom");
        gridManager.CreateExplodeEffect(gridPosition, bombStats.canPierceWall);
        gridManager.SetGridCellData(gridPosition, GridObjectType.Empty);
        int currentRange = 0;
        while (currentRange < bombStats.rangeExplode)
        {
            currentRange++;
            gridManager.CreateExplodeEffect(gridPosition + new Vector2Int(currentRange, 0), bombStats.canPierceWall);
            gridManager.CreateExplodeEffect(gridPosition + new Vector2Int(-currentRange, 0), bombStats.canPierceWall);
            gridManager.CreateExplodeEffect(gridPosition + new Vector2Int(0, currentRange), bombStats.canPierceWall);
            gridManager.CreateExplodeEffect(gridPosition + new Vector2Int(0, -currentRange), bombStats.canPierceWall);
        }
    }
}
