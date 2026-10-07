using TMPro;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    public GridCellData gridCellData;
    public TextMeshPro textMeshPro;
    public SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        textMeshPro = GetComponentInChildren<TextMeshPro>();
    }
}
