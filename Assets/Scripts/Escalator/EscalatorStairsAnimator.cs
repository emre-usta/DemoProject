using UnityEngine;

/// <summary>
/// Animates escalator stairs by scrolling the texture to create movement illusion
/// </summary>
public class EscalatorStairsAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Yürüyen merdiven basamaklarının MeshRenderer'ı")]
    [SerializeField] private MeshRenderer stairsMeshRenderer;
    
    [Tooltip("Animasyon hızı (pozitif = yukarı, negatif = aşağı)")]
    [SerializeField] private float scrollSpeed = 0.5f;
    
    [Tooltip("Hangi texture property'si kaydırılacak")]
    [SerializeField] private string texturePropertyName = "_MainTex";
    
    [Header("Direction")]
    [Tooltip("Kaydırma yönü (Y = yukarı/aşağı, X = sağa/sola)")]
    [SerializeField] private Vector2 scrollDirection = new Vector2(0, 1); // Y ekseni = yukarı
    
    [Header("Control")]
    [Tooltip("Animasyon otomatik başlasın mı?")]
    [SerializeField] private bool playOnStart = true;
    
    [Tooltip("Animasyon çalışıyor mu?")]
    [SerializeField] private bool isPlaying = true;
    
    private Material stairsMaterial;
    private Vector2 currentOffset = Vector2.zero;

    private void Start()
    {
        // MeshRenderer otomatik bul
        if (stairsMeshRenderer == null)
        {
            stairsMeshRenderer = GetComponent<MeshRenderer>();
        }

        if (stairsMeshRenderer == null)
        {
            Debug.LogError("EscalatorStairsAnimator: MeshRenderer bulunamadı!");
            enabled = false;
            return;
        }

        // Material'i kopyala (orijinali değiştirmemek için)
        stairsMaterial = stairsMeshRenderer.material;

        if (!playOnStart)
        {
            isPlaying = false;
        }

        Debug.Log($"✅ Escalator animation başlatıldı. Hız: {scrollSpeed}, Yön: {scrollDirection}");
    }

    private void Update()
    {
        if (!isPlaying || stairsMaterial == null) return;

        // Texture offset'i güncelle
        float offsetY = Time.time * scrollSpeed * scrollDirection.y;
        float offsetX = Time.time * scrollSpeed * scrollDirection.x;
        
        currentOffset = new Vector2(offsetX, offsetY);
        
        // Material'e uygula
        stairsMaterial.SetTextureOffset(texturePropertyName, currentOffset);
    }

    /// <summary>
    /// Animasyonu başlat
    /// </summary>
    public void Play()
    {
        isPlaying = true;
    }

    /// <summary>
    /// Animasyonu durdur
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
    }

    /// <summary>
    /// Animasyon hızını değiştir
    /// </summary>
    public void SetSpeed(float speed)
    {
        scrollSpeed = speed;
    }

    /// <summary>
    /// Animasyon yönünü ters çevir
    /// </summary>
    public void ReverseDirection()
    {
        scrollSpeed = -scrollSpeed;
    }

    private void OnDestroy()
    {
        // Material'i temizle (memory leak önlemek için)
        if (stairsMaterial != null)
        {
            Destroy(stairsMaterial);
        }
    }

    // Inspector'da test etmek için
    [ContextMenu("Play Animation")]
    private void TestPlay()
    {
        Play();
    }

    [ContextMenu("Stop Animation")]
    private void TestStop()
    {
        Stop();
    }

    [ContextMenu("Reverse Direction")]
    private void TestReverse()
    {
        ReverseDirection();
    }
}