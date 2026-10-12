using UnityEngine;

public class InputManager : MonoBehaviour
{
    private GameManager gameManager;
    [SerializeField] private Player player;

    private void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }

    private void Update()
    {
        if (player == null || string.IsNullOrEmpty(player.playerId)) return;

        if (player.canInput)
        {
            TryPlaceBomb();
            TryMove();
        }
    }

    private void TryPlaceBomb()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            string jsonMsg = $"{{\"action\": \"PLACE_BOMB\", \"playerId\": \"{player.playerId}\"}}";
            gameManager.SendWebSocketMessage(jsonMsg);
        }
    }

    private void TryMove()
    {
        string moveDirection = "";
        Vector2Int nextPosition = player.gridPosition;

        int x = 0;
        int y = 0;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) { moveDirection = "UP"; y = 1; }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) { moveDirection = "DOWN"; y = -1; }
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { moveDirection = "LEFT"; x = -1; }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { moveDirection = "RIGHT"; x = 1; }

        player.SetMoveInput(x, y);

        if (string.IsNullOrEmpty(moveDirection))
            return;

        nextPosition += new Vector2Int(x, y);

        if (!player.CanMove(nextPosition))
            return;



        player.canInput = false;

        PlayerActionMessage actionMessage = new PlayerActionMessage
        {
            action = "MOVE",
            playerId = player.playerId,
            direction = moveDirection
        };

        string jsonMsg = JsonUtility.ToJson(actionMessage);
        gameManager.SendWebSocketMessage(jsonMsg);

    }
}
