using UnityEngine;

/// <summary>
/// Trigger zone that detects when player enters near x-ray machine
/// </summary>
public class XRayTriggerZone : MonoBehaviour
{
    [Header("References")]
    public XRayMachine xRayMachine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered x-ray machine zone.");
            if (xRayMachine != null)
            {
                xRayMachine.OnPlayerEnter();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player exited x-ray machine zone.");
            if (xRayMachine != null)
            {
                xRayMachine.OnPlayerExit();
            }
        }
    }
}

