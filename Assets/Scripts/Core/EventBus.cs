using System;

public static class EventBus
{
    public static event Action<int> OnCurrencyChanged;
    

    public static void RaiseCurrencyChanged(int newCurrency)
    {
        OnCurrencyChanged?.Invoke(newCurrency);
    }
}