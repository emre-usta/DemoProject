using UnityEngine;
using System.Collections;

public class UnlockAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useLightUpEffect = true;
    [SerializeField] private Color lightUpColor = Color.yellow;
    [SerializeField] private float lightUpIntensity = 2f;

    private Vector3 originalScale;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Material[] lightUpMaterials;

    private void Awake()
    {
        originalScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>();
        
        if (useLightUpEffect && renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            lightUpMaterials = new Material[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
                lightUpMaterials[i] = new Material(originalMaterials[i]);
                lightUpMaterials[i].EnableKeyword("_EMISSION");
                lightUpMaterials[i].SetColor("_EmissionColor", lightUpColor * lightUpIntensity);
            }
        }
    }

    public void PlayUnlockAnimation()
    {
        StartCoroutine(AnimateUnlock());
    }

    private IEnumerator AnimateUnlock()
    {
        transform.localScale = Vector3.zero;
        
        if (useLightUpEffect && renderers.Length > 0)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && lightUpMaterials[i] != null)
                {
                    renderers[i].material = lightUpMaterials[i];
                }
            }
        }

        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float curveValue = scaleCurve.Evaluate(t);
            
            transform.localScale = originalScale * curveValue;
            
            if (useLightUpEffect && renderers.Length > 0)
            {
                float fadeOut = 1f - t; // Fade from full brightness to normal
                Color currentColor = Color.Lerp(lightUpColor, Color.white, t);
                
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && lightUpMaterials[i] != null)
                    {
                        lightUpMaterials[i].SetColor("_EmissionColor", currentColor * lightUpIntensity * fadeOut);
                    }
                }
            }
            
            yield return null;
        }

        transform.localScale = originalScale;
        
        if (useLightUpEffect && renderers.Length > 0)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && originalMaterials[i] != null)
                {
                    renderers[i].material = originalMaterials[i];
                }
            }
        }
    }
}






