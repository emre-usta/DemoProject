using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PaintingManager : MonoBehaviour
{
    [Header("Board")]
    public MeshRenderer boardRenderer;
    public Camera mainCamera;

    [Header("Brush")]
    public Texture2D brushTexture;
    public float brushSize = 24f;
    public float minBrushSize = 5f;
    public float maxBrushSize = 40f;
    public Color currentColor = Color.red;
    
    [Header("UI")]
    public Slider brushSizeSlider;
    public TMP_Text progressText;

    [Header("Debug")]
    public bool showDebugLogs = false;
    public TMP_Text debugText; // Ekstra debug için

    private Texture2D paintTexture;
    private Color[] clearColors;
    private const int TexSize = 512;
    private Collider boardCollider;
    
    // ============= DPI SCALING FIX =============
    private float dpiScale = 1f;
    private bool dpiCalculated = false;
    // ===========================================

    private void Start()
    {
        InitPaintTexture();
        
        if (brushSizeSlider != null)
        {
            brushSizeSlider.onValueChanged.AddListener(SetBrushSize);
            
            if (brushSizeSlider.maxValue <= 1.01f)
                brushSizeSlider.value = Mathf.InverseLerp(minBrushSize, maxBrushSize, brushSize);
            else
                brushSizeSlider.value = brushSize;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (boardRenderer != null)
        {
            boardCollider = boardRenderer.GetComponent<Collider>();
            if (boardCollider == null)
            {
                Debug.LogError("❌ Board'da Collider yok! Mesh Collider ekleyin.");
            }
        }

        // ============= DPI SCALE HESAPLA =============
        CalculateDPIScale();
        // ============================================

        if (showDebugLogs)
        {
            Debug.Log($"✅ Painting başlatıldı");
            Debug.Log($"Screen: {Screen.width}x{Screen.height}");
            Debug.Log($"DPI Scale: {dpiScale}");
            Debug.Log($"Camera: {mainCamera?.name}");
            Debug.Log($"Board: {boardRenderer?.name}");
        }
    }

    // ============= YENİ: DPI SCALE HESAPLAMA =============
    private void CalculateDPIScale()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL'de gerçek canvas boyutunu al
        dpiScale = GetCanvasScale();
        dpiCalculated = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"🔍 WebGL DPI Scale: {dpiScale}");
        }
        #else
        dpiScale = 1f;
        dpiCalculated = true;
        #endif
    }

    // JavaScript'ten canvas scale'i al
    private float GetCanvasScale()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            // Canvas'ın gerçek CSS boyutunu al
            float canvasWidth = GetCanvasClientWidth();
            float canvasHeight = GetCanvasClientHeight();
            
            if (canvasWidth > 0 && Screen.width > 0)
            {
                float scaleX = Screen.width / canvasWidth;
                float scaleY = Screen.height / canvasHeight;
                
                // Ortalama scale'i kullan
                return (scaleX + scaleY) / 2f;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"DPI scale hesaplanamadı: {e.Message}");
        }
        #endif
        
        return 1f;
    }

    // JSLIB fonksiyonları için placeholder (gerçekte JSLIB'den çağrılacak)
    private float GetCanvasClientWidth()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Bu fonksiyon WebGLTemplates/jslib'den implement edilmeli
        // Şimdilik Screen.width / devicePixelRatio kullan
        return Screen.width / GetDevicePixelRatio();
        #else
        return Screen.width;
        #endif
    }

    private float GetCanvasClientHeight()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        return Screen.height / GetDevicePixelRatio();
        #else
        return Screen.height;
        #endif
    }

    private float GetDevicePixelRatio()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Varsayılan olarak en yaygın değerler
        // Retina/High DPI: 2.0, Normal: 1.0
        if (Screen.width > 2000)
            return 2f; // Muhtemelen high DPI
        #endif
        return 1f;
    }
    // =======================================================

    void InitPaintTexture()
    {
        paintTexture = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
        paintTexture.filterMode = FilterMode.Bilinear;
        paintTexture.wrapMode = TextureWrapMode.Clamp;
        
        clearColors = new Color[TexSize * TexSize];

        for (int i = 0; i < clearColors.Length; i++)
            clearColors[i] = Color.white;

        paintTexture.SetPixels(clearColors);
        paintTexture.Apply();

        if (boardRenderer != null)
        {
            boardRenderer.material.mainTexture = paintTexture;
        }
    }

    void Update()
    {
        if (!GameStateManager.Instance.IsPaintingMode) 
        {
            if (showDebugLogs && debugText != null)
            {
                debugText.text = "⚠️ Painting Mode KAPALI!";
            }
            return;
        }

        bool isPointing = false;
        Vector3 inputPosition = Vector3.zero;

        // Input al
        if (Input.GetMouseButton(0))
        {
            isPointing = true;
            inputPosition = Input.mousePosition;
        }

        #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                isPointing = true;
                inputPosition = touch.position;
            }
        }
        #endif

        if (isPointing)
        {
            // UI kontrolü
            if (IsPointerOverUI(inputPosition))
            {
                if (showDebugLogs && debugText != null)
                {
                    debugText.text = "⚠️ UI üzerinde";
                }
                return;
            }

            PaintAtPosition(inputPosition);
        }
    }

    private bool IsPointerOverUI(Vector3 inputPosition)
    {
        if (EventSystem.current == null) return false;

        #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        return EventSystem.current.IsPointerOverGameObject();
        #else
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        return false;
        #endif
    }

    void PaintAtPosition(Vector3 screenPosition)
    {
        if (mainCamera == null || boardCollider == null)
        {
            if (showDebugLogs && debugText != null)
            {
                debugText.text = "❌ Camera/Collider null!";
            }
            return;
        }

        // ============= DPI SCALE UYGULA =============
#if UNITY_WEBGL && !UNITY_EDITOR
// WebGL'de bazı tarayıcılarda Input.mousePosition zaten düzeltilmiş olabilir
// Eğer hala sorun varsa, manuel scaling yapın:
if (Screen.width > 2000) // Retina/High DPI
{
    screenPosition.x *= 2f;
    screenPosition.y *= 2f;
}
#endif
        // ===========================================

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        
        if (showDebugLogs && debugText != null)
        {
            debugText.text = $"Input: {Input.mousePosition}\n";
            debugText.text += $"Scaled: {screenPosition}\n";
            debugText.text += $"DPI: {dpiScale}\n";
            debugText.text += $"Ray: {ray.origin} -> {ray.direction}";
        }
        
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            if (hit.collider == boardCollider)
            {
                Vector2 uv = hit.textureCoord;
                
                if (showDebugLogs && debugText != null)
                {
                    debugText.text += $"\n✅ HIT! UV: {uv}";
                }
                
                PaintAtUV(uv);
                return;
            }
            else
            {
                if (showDebugLogs && debugText != null)
                {
                    debugText.text += $"\n⚠️ Yanlış obje: {hit.collider.name}";
                }
            }
        }
        else
        {
            if (showDebugLogs && debugText != null)
            {
                debugText.text += "\n❌ NO HIT!";
            }
        }
    }

    void PaintAtUV(Vector2 uv)
    {
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
        {
            return;
        }

        int x = Mathf.Clamp((int)(uv.x * paintTexture.width), 0, paintTexture.width - 1);
        int y = Mathf.Clamp((int)(uv.y * paintTexture.height), 0, paintTexture.height - 1);

        DrawBrush(x, y);
        UpdateProgress();
    }

    void DrawBrush(int cx, int cy)
    {
        int r = Mathf.RoundToInt(brushSize);
        int texW = paintTexture.width;
        int texH = paintTexture.height;

        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                int px = cx + x;
                int py = cy + y;

                if (px < 0 || py < 0 || px >= texW || py >= texH)
                    continue;

                float u = (x + r) / (float)(2 * r);
                float v = (y + r) / (float)(2 * r);

                Color brushColor = brushTexture.GetPixelBilinear(u, v);

                if (brushColor.a <= 0f)
                    continue;

                Color baseColor = paintTexture.GetPixel(px, py);
                Color finalColor = Color.Lerp(baseColor, currentColor, brushColor.a * 0.5f);

                paintTexture.SetPixel(px, py, finalColor);
            }
        }

        paintTexture.Apply();
    }

    void UpdateProgress()
    {
        Color[] pixels = paintTexture.GetPixels();
        int count = 0;

        foreach (var p in pixels)
        {
            if (!IsColorClose(p, Color.white, 0.1f))
                count++;
        }

        float percent = (float)count / pixels.Length * 100f;
        
        if (progressText != null)
        {
            progressText.text = "%" + percent.ToString("0");
        }

        if (percent >= 99f)
        {
            GameStateManager.Instance.FinishPainting();
        }
    }

    private bool IsColorClose(Color a, Color b, float threshold)
    {
        return Mathf.Abs(a.r - b.r) < threshold &&
               Mathf.Abs(a.g - b.g) < threshold &&
               Mathf.Abs(a.b - b.b) < threshold;
    }

    void SetBrushSize(float value)
    {
        if (brushSizeSlider != null && brushSizeSlider.maxValue <= 1.01f)
        {
            brushSize = Mathf.Lerp(minBrushSize, maxBrushSize, value);
        }
        else
        {
            brushSize = Mathf.Clamp(value, minBrushSize, maxBrushSize);
        }
    }

    public void SetColor(Color color)
    {
        currentColor = color;
    }
    
    public void SelectRed() => SetColor(Color.red);
    public void SelectBlue() => SetColor(Color.blue);
    public void SelectYellow() => SetColor(Color.yellow);
}