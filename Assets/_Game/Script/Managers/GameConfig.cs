using System.Collections.Generic;
using UnityEngine;

public class GameConfig : MonoBehaviour
{
    public static GameConfig I { get; private set; }

    public List<ProductType> unlockedProductTypes = new() { ProductType.Water };
    public List<ProductDefinition> productDefinitions = new();

    private Dictionary<ProductType, ProductDefinition> map;

    private void Awake()
    {
        I = this;

        map = new Dictionary<ProductType, ProductDefinition>();
        foreach (var def in productDefinitions)
            if (def != null) map[def.type] = def;
    }

    public IReadOnlyList<ProductType> GetUnlockedProducts() => unlockedProductTypes;

    public ProductDefinition GetDefinition(ProductType type)
    {
        map.TryGetValue(type, out var def);
        return def;
    }
}
