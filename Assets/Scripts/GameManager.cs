using System;
using System.Collections.Concurrent;
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
    private Player[] players;

    private void Awake()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        players = FindObjectsByType<Player>(FindObjectsSortMode.None);
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
                
                foreach(Player player in players)
                {
                    if (string.IsNullOrEmpty(player.playerId))
                    {
                        Vector2Int newGridPosition = new Vector2Int(playerPosition.x, playerPosition.y);
                        player.InitPlayerTransform(newGridPosition);
                        player.SetGridPosition(newGridPosition);
                        player.playerId = playerPosition.playerId;

                        Debug.Log($"Đã gán ID {playerPosition.playerId} cho nhân vật tại {player.transform.position}");
                        break;
                    }
                }
            }
            else if(msg.action == "UPDATE_POSITION")
            {
                PlayerPositionMessage playerPosition = JsonUtility.FromJson<PlayerPositionMessage>(jsonMessage);

                foreach (Player player in players)
                {
                    if (player.playerId == playerPosition.playerId)
                    {
                        Vector2Int newGridPosition = new Vector2Int(playerPosition.x, playerPosition.y);
                        player.SyncPositionFromServer(newGridPosition);
                        break;
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
