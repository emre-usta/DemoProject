using UnityEngine;

public class BoardPaintController : MonoBehaviour
{
    [Header("Paint Settings")]
    public int textureSize = 512;
    public Color startColor = Color.white;

    private Texture2D paintTexture;
    private Material paintMaterial;

    void Start()
    {
        paintMaterial = GetComponent<MeshRenderer>().material;

        // 🔥 Texture2D RUNTIME OLUŞTURULUYOR
        paintTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        paintTexture.filterMode = FilterMode.Bilinear;
        paintTexture.wrapMode = TextureWrapMode.Clamp;

        // Başlangıçta tek renk doldur
        Color[] colors = new Color[textureSize * textureSize];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = startColor;

        paintTexture.SetPixels(colors);
        paintTexture.Apply();

        // Material’e ata
        paintMaterial.mainTexture = paintTexture;
    }
}