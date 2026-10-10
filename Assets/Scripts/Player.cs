using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public string playerId = "";
    [SerializeField] private BombStats bombStats;
    public Vector2Int gridPosition;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private float moveSpeed = 2f;
    [Header("Player Stats")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int currentHealth;
    private Animator anim;
    [SerializeField] private Vector2 moveInput;

    private Coroutine moveCo;
    public bool canInput = true;
    [SerializeField] private int bombCount = 0;

    public void SetMoveInput(int x, int y)
    {
        moveInput = new Vector2(x, y);
    }

    private void Awake()
    {
        anim = GetComponent<Animator>();
        gridManager = FindAnyObjectByType<GridManager>();

        //InitPlayerTransform();
        currentHealth = maxHealth;
        anim.SetFloat("LastMoveY", 1);
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(playerId)) return;

        AnimHandle();
    }

    public void InitPlayerTransform(Vector2Int position)
    {
        transform.position = gridManager.GetWordPosition(position);
        gridManager.SetPlayerOnCellData(position, this);
    }

    private bool CanPlace() => bombCount < bombStats.maxBombCount && canInput && gridManager.CanPlace(gridPosition);
    public bool CanMove(Vector2Int position)
    {
        return gridManager.CanMove(position);
    }

    public void DecreaseBombCount() => bombCount--;

    [ContextMenu("Add Max Bomb Count")]
    public void AddMaxBombCount() => bombStats.maxBombCount++;

    private void CreateBomb()
    {
        if (CanPlace() == false)
            return;

        GameObject bomb = Instantiate(bombPrefab, transform.position, Quaternion.identity);
        Bomb bombScript = bomb.GetComponent<Bomb>();
        bombScript.player = this;
        bombScript.SetUp(gridManager, bombStats);
        bombScript.SetGridData(gridPosition);
        //Debug.Log("Create bomb in :" + gridPosition);
        bombCount++;
    }

    public void SyncPositionFromServer(Vector2Int serverGridPosition)
    {
        if (moveCo != null) StopCoroutine(moveCo);
        moveCo = StartCoroutine(MoveCo(serverGridPosition));
    }

    private IEnumerator MoveCo(Vector2Int serverGridPosition)
    {
        canInput = false;
        bool wasRemove = false;
        bool wasAdd = false;

        Vector3 newPosition = gridManager.GetWordPosition(serverGridPosition);

        while (true)
        {
            float distance = Vector2.Distance(transform.position, newPosition);
            if (distance <= 0.05f)
                break;
            if (distance < 0.7f && !wasAdd)
            {
                wasAdd = true;
                gridManager.SetPlayerOnCellData(serverGridPosition, this);
                if (gridManager.GetObjectTypeOnCell(serverGridPosition) == ObjectType.Buff)
                {
                    gridManager.ResetObjectTile(serverGridPosition);
                    AddMaxBombCount();
                    Debug.Log("Get buff in: " + serverGridPosition);
                }
            }
            if (distance < 0.3f && !wasRemove)
            {
                wasRemove = true;
                gridManager.RemovePlayerOnCell(gridPosition, this);
                gridPosition = serverGridPosition;
            }
            transform.position = Vector3.MoveTowards(transform.position, newPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = newPosition;
        canInput = true;
        SetMoveInput(0, 0);
    }

    private void AnimHandle()
    {
        anim.SetFloat("MoveX", moveInput.x);
        anim.SetFloat("MoveY", moveInput.y);

        if (moveInput != Vector2.zero)
        {
            anim.SetBool("isMoving", true);

            anim.SetFloat("LastMoveX", moveInput.x);
            anim.SetFloat("LastMoveY", moveInput.y);
        }
        else
        {
            anim.SetBool("isMoving", false);
        }
    }

    public void TakeDamage()
    {
        Debug.Log("Take damage");
        currentHealth--;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void SetGridPosition(Vector2Int position)
    {
        gridPosition = position;
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
