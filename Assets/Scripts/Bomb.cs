using UnityEngine;

public class Bomb : MonoBehaviour
{
    public Player player;
    private float lifeTime = 2f;
    private void Start()
    {
        Destroy(gameObject,2f);
    }

    private void OnDestroy()
    {
        Debug.Log("Boom");
        player.DecreaseMombCount();
    }
}
