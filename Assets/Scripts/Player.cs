using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private BombStats bombStats;
    private Vector2Int gridPosition;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private float moveSpeed = 2f;
    [Header("Player Stats")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int currentHealth;
    private Animator anim;
    private PlayerInputActions input;

    private Vector2 moveInput;

    private Coroutine moveCo;
    private bool canInput = true;
    [SerializeField] private int bombCount = 0;

    private void Awake()
    {
        input = new PlayerInputActions();
        anim = GetComponent<Animator>();
        gridManager = FindAnyObjectByType<GridManager>();

        InitPlayerTransform();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (canInput)
        {
            InputHandle();
            if (CanMove()) MoveHandle();
        }
        AnimHandle();
    }

    private void InitPlayerTransform()
    {
        gridPosition = gridManager.GetRandomGridPosition();
        transform.position = gridManager.GetGridPosition(gridPosition);
        gridManager.SetPlayerOnCellData(gridPosition, this);
    }

    private bool CanPlace() => bombCount < bombStats.maxBombCount && canInput && gridManager.CanPlace(gridPosition);
    private bool CanMove()
    {
        Vector2Int nextGridPosition = gridPosition + GetMoveInputGridPosition();
        return gridManager.CanMove(nextGridPosition);
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

    private void MoveHandle()
    {
        if (moveInput.x != 0)
        {
            SetPosition(moveInput.x, 0);
        }
        else if (moveInput.y != 0)
        {
            SetPosition(0, moveInput.y);
        }
    }

    private void SetPosition(float x, float y)
    {
        Vector3 newPosition = transform.position + new Vector3(x, y);
        Standardize(new Vector2(x, y));

        if (moveCo != null)
        {
            StopCoroutine(moveCo);
        }

        moveCo = StartCoroutine(MoveCo(newPosition));
    }

    private IEnumerator MoveCo(Vector3 newPosition)
    {
        canInput = false;

        bool wasRemove = false;
        bool wasAdd = false;
        while (true)
        {
            float distance = Vector2.Distance(transform.position, newPosition);
            if (distance <= 0.05f)
                break;
            if (distance < 0.7f && !wasAdd)
            {
                wasAdd = true;
                Vector2Int newGridPosition = gridPosition + GetMoveInputGridPosition();
                gridManager.SetPlayerOnCellData(newGridPosition, this);
                if(gridManager.GetObjectTypeOnCell(newGridPosition)==ObjectType.Buff)
                {
                    gridManager.ResetObjectTile(newGridPosition);
                    AddMaxBombCount();
                    Debug.Log("Get buff in: " + newGridPosition);
                }
            }
            if (distance < 0.3f && !wasRemove)
            {
                wasRemove = true;
                gridManager.RemovePlayerOnCell(gridPosition, this);
            }
            transform.position = Vector3.MoveTowards(transform.position, newPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        canInput = true;
        transform.position = newPosition;
        SetGridPosition();
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

    private void InputHandle()
    {
        moveInput = input.Player.Move.ReadValue<Vector2>();

        if (moveInput != Vector2.zero)
        {
            Standardize(moveInput);
        }
    }

    private void Standardize(Vector2 inputValue)
    {
        if (Mathf.Abs(inputValue.x) > Mathf.Abs(inputValue.y))
        {
            moveInput = new Vector2(Mathf.Sign(inputValue.x), 0);
        }
        else
        {
            moveInput = new Vector2(0, Mathf.Sign(inputValue.y));
        }
    }

    private void SetGridPosition()
    {
        gridPosition += GetMoveInputGridPosition();
    }

    private Vector2Int GetMoveInputGridPosition()
    {
        return new Vector2Int((int)moveInput.x, (int)moveInput.y);
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

    private void Die()
    {
        Destroy(gameObject);
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Place.performed += ctx => CreateBomb();
    }

    private void OnDisable()
    {
        input.Disable();
    }
}
