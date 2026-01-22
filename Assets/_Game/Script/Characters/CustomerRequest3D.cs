using System;
using System.Collections.Generic;
using UnityEngine;
using Shop;

public class CustomerRequest3D : MonoBehaviour
{
    [Header("Anchor to spawn 3D thought item")]
    public Transform thoughtItemAnchor3D;

    [Header("GameConfig (unlocked products)")]
    public GameConfig gameConfig;

    [Serializable]
    public class ThoughtPrefab
    {
        public ProductType type;
        public GameObject prefab3D;
    }

    [Header("Thought prefabs per product")]
    public List<ThoughtPrefab> thoughtPrefabs = new List<ThoughtPrefab>();

    // Hệ cũ (ProductType)
    public ProductType RequestedProduct { get; private set; }
    public bool IsRequestVisible => currentThoughtInstance != null;

    // Hệ mới (Shop.ProductId) - Employee đọc cái này
    [HideInInspector]
    public Shop.ProductId requestedProduct = Shop.ProductId.WaterBottle;

    private GameObject currentThoughtInstance;

    public void HideRequest()
    {
        if (currentThoughtInstance != null)
        {
            Destroy(currentThoughtInstance);
            currentThoughtInstance = null;
        }
    }

    // Hàm cho ProductType (hệ cũ)
    public void ShowRequest(ProductType type)
    {
        HideRequest();

        RequestedProduct = type;
        requestedProduct = MapToProductId(type);

        var prefab = GetThoughtPrefab(type);
        if (prefab == null || thoughtItemAnchor3D == null) return;

        currentThoughtInstance = Instantiate(prefab, thoughtItemAnchor3D);
        currentThoughtInstance.transform.localPosition = Vector3.zero;
        currentThoughtInstance.transform.localRotation = Quaternion.identity;
    }

    // Hàm cho ProductId (hệ mới)
    public void ShowRequest(Shop.ProductId productId)
    {
        requestedProduct = productId;
        
        // Convert về ProductType để dùng prefab hiện tại
        var type = MapToProductType(productId);
        ShowRequest(type);
    }

    public void RandomizeRequestFromUnlocked()
    {
        if (gameConfig == null || gameConfig.unlockedProductTypes == null || gameConfig.unlockedProductTypes.Count == 0)
        {
            ShowRequest(ProductType.Water);
            return;
        }

        var unlocked = gameConfig.unlockedProductTypes;
        int index = UnityEngine.Random.Range(0, unlocked.Count);
        ShowRequest(unlocked[index]);
    }

    private GameObject GetThoughtPrefab(ProductType type)
    {
        for (int i = 0; i < thoughtPrefabs.Count; i++)
        {
            if (thoughtPrefabs[i].type == type)
                return thoughtPrefabs[i].prefab3D;
        }
        return null;
    }

    // Mapping giữa 2 enum
    private Shop.ProductId MapToProductId(ProductType type)
    {
        switch (type)
        {
            case ProductType.Water:
                return Shop.ProductId.WaterBottle;
            default:
                return Shop.ProductId.WaterBottle;
        }
    }

    private ProductType MapToProductType(Shop.ProductId id)
    {
        switch (id)
        {
            case Shop.ProductId.WaterBottle:
                return ProductType.Water;
            default:
                return ProductType.Water;
        }
    }
}
