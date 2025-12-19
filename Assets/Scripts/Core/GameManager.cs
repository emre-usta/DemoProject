using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Loading,
        MainMenu,
        Playing,
        PaintingMinigame,
        GameOver
    }

    public GameState State { get; private set; }

    [SerializeField] private int currency = 0;
    public int Currency 
    { 
        get => currency; 
        private set => currency = value; 
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetState(GameState newState)
    {
        State = newState;
        // Fire global GameState change event
        //EventBus.RaiseGameStateChanged(newState);
    }

    public void AddCurrency(int amount)
    {
        currency += amount;
        EventBus.RaiseCurrencyChanged(currency);
    }

    public bool SpendCurrency(int amount)
    {
        if (currency >= amount)
        {
            currency -= amount;
            EventBus.RaiseCurrencyChanged(currency);
            return true;
        }
        return false;
    }
}
