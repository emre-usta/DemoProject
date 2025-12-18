using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public bool IsPaintingMode { get; private set; }

    public CameraFollow cameraFollow;
    public Transform paintingCameraPoint;
    public GameObject paintingUI;
    public GameObject joystickRoot;
    public PlayerController playerControl;

    private void Awake()
    {
        Instance = this;
    }

    public void EnterPaintingMode()
    {
        IsPaintingMode = true;

        // Player kontrolünü kapat
        playerControl.enabled = false;
        
        if (joystickRoot != null)
            joystickRoot.SetActive(false);


        // UI aç
        paintingUI.SetActive(true);

        // Kamerayı board'a al
        cameraFollow.SetTarget(paintingCameraPoint, CameraFollow.CameraMode.Painting);
    }

    public void FinishPainting()
    {
        Debug.Log("PAINTING COMPLETE!");
        // Level Complete, Win UI, etc.
    }
}