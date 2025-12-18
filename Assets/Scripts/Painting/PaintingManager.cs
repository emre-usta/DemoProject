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

    private Texture2D paintTexture;
    private Color[] clearColors;

    private const int TexSize = 512;
    private void Start()
    {
        InitPaintTexture();
        brushSizeSlider.onValueChanged.AddListener(SetBrushSize);
        
        if (brushSizeSlider.maxValue <= 1.01f)
            brushSizeSlider.value = Mathf.InverseLerp(minBrushSize, maxBrushSize, brushSize);
        else
            brushSizeSlider.value = brushSize;
    }

    void InitPaintTexture()
    {
        paintTexture = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
        clearColors = new Color[TexSize * TexSize];

        for (int i = 0; i < clearColors.Length; i++)
            clearColors[i] = Color.white;

        paintTexture.SetPixels(clearColors);
        paintTexture.Apply();

        boardRenderer.material.mainTexture = paintTexture;
    }

    void Update()
    {
        if (!GameStateManager.Instance.IsPaintingMode) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        
        if (Input.GetMouseButton(0))
        {
            Paint();
        }
    }

    void Paint()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject != boardRenderer.gameObject) return;

            Vector2 uv = hit.textureCoord;
            int x = (int)(uv.x * paintTexture.width);
            int y = (int)(uv.y * paintTexture.height);

            DrawBrush(x, y);
            UpdateProgress();
        }
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

                // Brush UV (0–1)
                float u = (x + r) / (float)(2 * r);
                float v = (y + r) / (float)(2 * r);

                Color brushColor = brushTexture.GetPixelBilinear(u, v);

                if (brushColor.a <= 0f)
                    continue;

                Color baseColor = paintTexture.GetPixel(px, py);

                // Alpha ile yumuşak karışım
                Color finalColor = Color.Lerp(
                    baseColor,
                    currentColor,
                    brushColor.a * 0.5f // ← yumuşaklık burada
                );

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
            if (p != Color.white) count++;

        float percent = (float)count / pixels.Length * 100f;
        progressText.text = "%" + percent.ToString("0");

        if (percent >= 99f)
        {
            GameStateManager.Instance.FinishPainting();
        }
    }

    void SetBrushSize(float value)
    {
        brushSize = value;
    }

    public void SetColor(Color color)
    {
        currentColor = color;
    }
    
    public void SelectRed()
    {
        SetColor(Color.red);
    }

    public void SelectBlue()
    {
        SetColor(Color.blue);
    }

    public void SelectYellow()
    {
        SetColor(Color.yellow);
    }
}
