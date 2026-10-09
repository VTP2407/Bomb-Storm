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
    public bool wasExplode { get; private set; } = false;

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
        Invoke(nameof(Explode), bombStats.lifeTime);
    }

    public void SetGridData(Vector2Int position)
    {
        gridPosition = position;
        gridManager.SetBombOnCell(position, this);
    }

    public void Explode()
    {
        if (wasExplode == true) return;
        wasExplode = true;
        Debug.Log("Boom");
        gridManager.CreateExplodeEffect(gridPosition, bombStats.canPierceWall);

        ExplodeDirection(Vector2Int.right);
        ExplodeDirection(Vector2Int.left);
        ExplodeDirection(Vector2Int.up);
        ExplodeDirection(Vector2Int.down);

        player.DecreaseBombCount();
        gridManager.ResetBombOnCell(gridPosition);
        Destroy(gameObject);
    }

    private void ExplodeDirection(Vector2Int direction)
    {
        for (int range = 1; range <= bombStats.rangeExplode; range++)
        {
            Vector2Int newPosition = gridPosition + direction * range;

            if (!gridManager.InsideGrid(newPosition))
                break;

            ObjectType objectType = gridManager.GetObjectTypeOnCell(newPosition);

            if (objectType == ObjectType.Wall && !bombStats.canPierceWall)
                break;

            gridManager.CreateExplodeEffect(newPosition, bombStats.canPierceWall);

            if ((objectType == ObjectType.Box || objectType == ObjectType.Bomb) && !bombStats.canPierceWall)
                break;
        }
    }
}
