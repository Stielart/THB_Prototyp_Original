using UnityEngine;

/// <summary>
/// Anker der Kamera. X folgt dem Kasten der Dead Zone (CameraControl.deadZoneX), Y dem senkrechten Kasten (deadZoneY).
/// Das Skript läuft nach CameraControl, damit es in jedem Bild den aktuellen Wert dieses Frames liest.
/// Die senkrechte Bewegung wird leicht geglättet, damit die Kamera nicht ruckartig anfährt, sobald die Figur den Kasten verlässt.
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraAncle : MonoBehaviour
{

    [SerializeField] private CameraControl cameraControl;
    [SerializeField] private Player player; 

    private float smoothY;
    private float velocityY;
    private bool initialized;

    void LateUpdate()
    {
        if (!initialized)
        {
            smoothY = cameraControl.deadZoneY;
            initialized = true;
        }

        smoothY = Mathf.SmoothDamp(smoothY, cameraControl.deadZoneY, ref velocityY, cameraControl.verticalSmoothTime);
        transform.position = new Vector3(cameraControl.deadZoneX, smoothY, 0);
    }
}
