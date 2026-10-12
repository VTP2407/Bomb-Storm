using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private ClientWebSocket ws;
    private Uri serverUri = new Uri("ws://localHost:8080/game");

    private ConcurrentQueue<string> messangeQueue = new ConcurrentQueue<string>();

    private GridManager gridManager;
    private Dictionary<string, Player> players = new Dictionary<string, Player>();
    private Player[] scenePlayers;

    [Header("Prefab")]
    [SerializeField] private GameObject bombPrefab;
    private Dictionary<string, GameObject> activeBombs = new Dictionary<string, GameObject>();

    private void Awake()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        scenePlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
    }

    async void Start()
    {
        ws = new ClientWebSocket();
        try
        {
            Debug.Log("Connecting to server...");

            await ws.ConnectAsync(serverUri, CancellationToken.None);

            Debug.Log("Connected");

            _ = ReceiveMessange();

            SendWebSocketMessage("{\"action\": \"HELLO\", \"message\": \"Chào Server, tôi là Unity!\"}");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Can't connect to server " + e.Message);
        }
    }

    private void Update()
    {
        while (messangeQueue.TryDequeue(out string message))
        {
            ProcessServerMessage(message);
        }
    }

    private async Task ReceiveMessange()
    {
        var buffer = new Byte[1024 * 4];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messangeQueue.Enqueue(message);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                Debug.Log("Disconnected");
            }
        }
    }

    public async void SendWebSocketMessage(String jsonMessage)
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(jsonMessage);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private void ProcessServerMessage(string jsonMessage)
    {
        Debug.Log("Nhận được từ Server: " + jsonMessage);

        try
        {
            ServerMessage msg = JsonUtility.FromJson<ServerMessage>(jsonMessage);

            if (msg.action == "INIT_MAP")
            {
                Debug.Log("Server yêu cầu tạo Map!");

                gridManager.BuildMapFromServer(msg.width, msg.height, msg.mapData);
            }
            else if (msg.action == "INIT_PLAYER")
            {
                PlayerPositionMessage playerPosition = JsonUtility.FromJson<PlayerPositionMessage>(jsonMessage);

                foreach (Player sp in scenePlayers)
                {
                    if (string.IsNullOrEmpty(sp.playerId))
                    {
                        Vector2Int newGridPosition = new Vector2Int(playerPosition.x, playerPosition.y);
                        sp.InitPlayerTransform(newGridPosition);
                        sp.SetGridPosition(newGridPosition);
                        sp.playerId = playerPosition.playerId;

                        players.Add(sp.playerId, sp);

                        Debug.Log($"Đã gán ID {playerPosition.playerId} cho nhân vật tại {sp.transform.position}");
                        break;
                    }
                }
            }
            else if (msg.action == "UPDATE_POSITION")
            {
                PlayerPositionMessage playerPosition = JsonUtility.FromJson<PlayerPositionMessage>(jsonMessage);

                if (players.TryGetValue(playerPosition.playerId, out Player targetPlayer))
                {
                    Vector2Int newGridPosition = new Vector2Int(playerPosition.x, playerPosition.y);
                    targetPlayer.SyncPositionFromServer(newGridPosition);
                }
            }
            else if (msg.action == "SPAWN_BOMB")
            {
                SpawnBombMessage bombMessage = JsonUtility.FromJson<SpawnBombMessage>(jsonMessage);
                Vector2Int bombPosition = new Vector2Int(bombMessage.x, bombMessage.y);
                Vector3 wordPosition = gridManager.GetWordPosition(bombPosition);

                GameObject newBomb = Instantiate(bombPrefab, wordPosition, Quaternion.identity);

                Bomb bombScript = newBomb.GetComponent<Bomb>();
                if (bombScript != null)
                {
                    bombScript.SetUp(bombMessage.lifeTime);
                }

                gridManager.SetGridObjectType(bombPosition, ObjectType.Bomb);

                activeBombs[bombMessage.bombId] = newBomb;
            }
            else if (msg.action == "EXPLODE_BOMB")
            {
                ExplodeBombMessage explodeBombMessage = JsonUtility.FromJson<ExplodeBombMessage>(jsonMessage);

                //Destroy bomb
                if (activeBombs.TryGetValue(explodeBombMessage.bombId, out GameObject bomb))
                {
                    Destroy(bomb);
                    activeBombs.Remove(explodeBombMessage.bombId);
                }

                //Explode 
                foreach (Vector2IntDto cell in explodeBombMessage.explodedCells)
                {
                    Vector2Int position = new Vector2Int(cell.x, cell.y);
                    gridManager.CreateExplodeEffect(position, false);
                }

                //Take damage
                foreach (DamagedPlayerDto damaged in explodeBombMessage.damagedPlayers)
                {
                    if (players.TryGetValue(damaged.playerId, out Player targetPlayer))
                    {
                        targetPlayer.TakeDamage();

                        if (!damaged.isAlive || damaged.health <= 0)
                        {
                            targetPlayer.Die();
                            players.Remove(targetPlayer.playerId);
                            Debug.Log($"Player {targetPlayer.playerId} đã chết!");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Lỗi khi đọc JSON từ Server: " + e.Message);
        }
    }

    private async void OnApplicationQuit()
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Game quit", CancellationToken.None);
        }
    }
}

[Serializable]
public class ServerMessage
{
    public string action;
    public int width;
    public int height;
    public int[] mapData;
}

[Serializable]
public class PlayerPositionMessage
{
    public string action;
    public string playerId;
    public int x;
    public int y;
}

[Serializable]
public class PlayerActionMessage
{
    public string action;
    public string playerId;
    public string direction;
}

[Serializable]
public class SpawnBombMessage
{
    public string action;
    public string bombId;
    public int x;
    public int y;
    public float lifeTime;
}

[Serializable]
public class ExplodeBombMessage
{
    public string action;
    public string bombId;
    public Vector2IntDto[] explodedCells;
    public DamagedPlayerDto[] damagedPlayers;
}

[Serializable]
public class Vector2IntDto { public int x; public int y; }

[Serializable]
public class DamagedPlayerDto
{
    public string playerId;
    public int health;
    public bool isAlive;
}