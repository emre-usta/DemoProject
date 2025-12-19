using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI currencyTextMesh; 
    
    [Header("Display Settings")]
    [SerializeField] private string currencyPrefix = "Currency: ";
    [SerializeField] private string currencySuffix = "";

    private void Awake()
    {
        if (currencyTextMesh == null)
        {
            currencyTextMesh = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        EventBus.OnCurrencyChanged += UpdateCurrencyDisplay;
        UpdateCurrencyDisplay();
    }

    private void OnDisable()
    {
        EventBus.OnCurrencyChanged -= UpdateCurrencyDisplay;
    }

    private void Start()
    {
        UpdateCurrencyDisplay();
    }

    private void UpdateCurrencyDisplay()
    {
        int currency = GameManager.Instance != null ? GameManager.Instance.Currency : 0;
        UpdateCurrencyDisplay(currency);
    }

    private void UpdateCurrencyDisplay(int currency)
    {
        string displayText = currencyPrefix + currency + currencySuffix;

        if (currencyTextMesh != null)
        {
            currencyTextMesh.text = displayText;
        }
    }
}