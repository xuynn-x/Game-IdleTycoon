using System;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager I { get; private set; }

    public int money = 0;
    public event Action<int> OnMoneyChanged;

    private void Awake()
    {
        I = this;
        OnMoneyChanged?.Invoke(money);
    }

    public void Add(int amount)
    {
        money += Mathf.Max(0, amount);
        OnMoneyChanged?.Invoke(money);
    }
}
