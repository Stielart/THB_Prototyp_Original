using UnityEngine;

public class CameraAncle : MonoBehaviour
{

    [SerializeField] private CameraControl cameraControl;
    [SerializeField] private Player player; 

    void LateUpdate()
    {
        transform.position = new Vector3(cameraControl.deadZoneX, player.transform.position.y, 0);
    }
}
