using UnityEngine;

public class CameraFreezeEvent : MonoBehaviour
{
   
        [SerializeField] private CameraControl cameraControl;

        private void OnTriggerEnter2D(Collider2D other)
        {
        if (other.CompareTag("Player"))
        {

            cameraControl.FreezeXAt(transform.position.x);
        }
                
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            cameraControl.UnfreezeX();
    }
    }






