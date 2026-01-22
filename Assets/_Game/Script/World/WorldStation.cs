using UnityEngine;

public class WorldStation : MonoBehaviour
{
    public ProductType productType = ProductType.Water;

    [Header("Where employee stands to gather item")]
    public Transform standPoint;

    [Header("Time to gather item (seconds)")]
    public float gatherTime = 5f;

    [Header("Prefab that employee carries after gathering")]
    public GameObject carryItemPrefab;
}
