using System;

public static class EventBus
{
    //public static event Action<GameManager.GameState> OnGameStateChanged;
    public static event Action<int> OnCurrencyChanged;

    /*public static void RaiseGameStateChanged(GameManager.GameState newState)
    {
        OnGameStateChanged?.Invoke(newState);
    }*/

    public static void RaiseCurrencyChanged(int newCurrency)
    {
        OnCurrencyChanged?.Invoke(newCurrency);
    }
    // Extend with more events as needed
}