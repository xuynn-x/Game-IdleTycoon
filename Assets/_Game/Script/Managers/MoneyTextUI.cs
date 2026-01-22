using TMPro;
using UnityEngine;

public class MoneyTextUI : MonoBehaviour
{
    public MoneyManager moneyManager;
    public TMP_Text text;

    private void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (moneyManager == null) moneyManager = MoneyManager.I;
        if (moneyManager != null) moneyManager.OnMoneyChanged += Refresh;

        Refresh(moneyManager != null ? moneyManager.money : 0);
    }

    private void OnDestroy()
    {
        if (moneyManager != null) moneyManager.OnMoneyChanged -= Refresh;
    }

    private void Refresh(int value)
    {
        if (text != null) text.text = value.ToString();
    }
}
