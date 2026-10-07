using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private GridCellData gridCellData;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private GameObject bombPrefab;
    private Animator anim;
    private PlayerInputActions input;

    private Vector2 moveInput;

    private Coroutine moveCo;
    private bool canInput = true;
    [SerializeField] private int bombCount = 0;
    [SerializeField] private int maxBombCount = 1;

    private void Awake()
    {
        input = new PlayerInputActions();
        anim = GetComponent<Animator>();
        gridManager = FindFirstObjectByType<GridManager>();

        gridCellData = new GridCellData(new Vector2Int(0,0));
        gridCellData.objectType = GridObjectType.Player;
        InitPlayerTransform();
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
        gridCellData.position = gridManager.GetRandomPosition();
        transform.position = gridManager.GetGridPosition(gridCellData.position);
    }

    private bool CanPlace() => bombCount < maxBombCount && canInput;
    private bool CanMove()
    {
        Vector2Int nextGridPosition = gridCellData.position + GetMoveInputGridPosition();
        return gridManager.CanMove(nextGridPosition);
    }

    public void DecreaseMombCount() => bombCount--;

    [ContextMenu("Add Max Bomb Count")]
    public void AddMaxBombCount() => maxBombCount++;

    private void CreateBomb()
    {
        if (CanPlace() == false)
            return;

        GameObject bomb = Instantiate(bombPrefab, transform.position, Quaternion.identity);
        bomb.GetComponent<Bomb>().player = this;
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

        while (Vector2.Distance(transform.position, newPosition) > 0.05f)
        {
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
        gridCellData.position += GetMoveInputGridPosition();
    }

    private Vector2Int GetMoveInputGridPosition()
    {
        return new Vector2Int((int)moveInput.x, (int)moveInput.y);
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
