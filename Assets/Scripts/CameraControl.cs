using UnityEngine;
using Unity.Cinemachine;
using System.Runtime.ExceptionServices;
using System;
using System.Runtime.CompilerServices;

public class CameraControl : MonoBehaviour
{







    [SerializeField] private Player player;

    //[SerializeField] private CameraAncle cameraAncle;

    [Header("Camera Offset Values")]
    [SerializeField] private float rightOffsetXValue = 6f;
    [SerializeField] private float leftOffsetXValue = -6f;

    [SerializeField] private float upOffsetYValue = 2f;
    [SerializeField] private float downOffsetYValue = -2f;

    [SerializeField] public float deadZoneRightThreshold = 3f;
    [SerializeField] public float deadZoneLeftThreshold = 3f;


    private float currentOffsetX = 0;
    private float currentOffsetY = 0;

    private float targetX = 3f;
    private float targetY;

    private float distanceFromBoxCenter = 0f;




    public bool isLockedRight = true;

    private PlayerState currentState;

    private PlayerState previousState;
    public float deadZoneX; 

    void Awake()
    {
        deadZoneX = player.transform.position.x;


        currentState = player.currentState;
        previousState = currentState;
    }
    void Start()
    {
        


    }


    // Update is called once per frame
    void Update()
    {
        
    }


    private void LateUpdate()
    {
        CalculateCameraPosition();
        
       
    }








    private void HandleStateChange()
    {
        currentState = player.currentState;
        if (currentState != previousState)
        {
            previousState = currentState;


        }


    }

    private void CalculateCameraPosition()
    {
        HandleStateChange();
        UpdateBoxPosition();
        UpdateTargetX();
        UpdateTargetY();

        
   
        transform.localPosition = new Vector2(targetX, targetY);

    }

    private void UpdateTargetX()
    {
        bool isNormalGroundedOrAir = currentState == player.groundState
                                    || currentState == player.jumpState
                                    || currentState == player.fallState
                                    || currentState == player.wallJumpState;

        if (isNormalGroundedOrAir)
            if (distanceFromBoxCenter >= deadZoneRightThreshold)
            {
                targetX = rightOffsetXValue; // größerer Offset, weil rechts geschoben wird
                isLockedRight = true;
            }
            else if (distanceFromBoxCenter <= -deadZoneLeftThreshold)
            {
                targetX = leftOffsetXValue;
                isLockedRight = false;
            }
            // sonst: targetX bleibt unverändert (Dead Zone, nichts passiert)
            else if (currentState == player.wallSlideState || currentState == player.wallSlideFallState)
                targetX = 0f;

        Debug.Log("targetX: " + targetX); 
    }


    private void UpdateTargetY()
    {
        

        bool isFallingOrWallFalling = currentState == player.fallState || currentState == player.wallSlideFallState;
        if (isFallingOrWallFalling)
        {
            targetY = downOffsetYValue;
        }
        else if(currentState == player.groundState)
        {
            targetY = 0f;
        }

    }


    

    void UpdateBoxPosition()
    {
        distanceFromBoxCenter = player.transform.position.x - deadZoneX;

        if (distanceFromBoxCenter > deadZoneRightThreshold)
            deadZoneX = player.transform.position.x - deadZoneRightThreshold; // Box wird geschoben, Spieler bleibt GENAU an der Wand
        else if (distanceFromBoxCenter < -deadZoneLeftThreshold)
            deadZoneX = player.transform.position.x + deadZoneLeftThreshold;
    }


    private void OnDrawGizmos()
    {
        // Anker-Position (grüner Punkt)
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(new Vector3(deadZoneX, transform.position.y, 0), 0.1f);

        // Rechte Schwelle (rote Linie)
        Gizmos.color = Color.red;
        Vector3 rightThresholdPos = new Vector3(deadZoneX + deadZoneRightThreshold, transform.position.y - 1f, 0);
        Vector3 rightThresholdPosTop = new Vector3(deadZoneX + deadZoneRightThreshold, transform.position.y + 1f, 0);
        Gizmos.DrawLine(rightThresholdPos, rightThresholdPosTop);

        // Linke Schwelle (blaue Linie)
        Gizmos.color = Color.blue;
        Vector3 leftThresholdPos = new Vector3(deadZoneX - deadZoneLeftThreshold, transform.position.y - 1f, 0);
        Vector3 leftThresholdPosTop = new Vector3(deadZoneX - deadZoneLeftThreshold, transform.position.y + 1f, 0);
        Gizmos.DrawLine(leftThresholdPos, leftThresholdPosTop);
    }
}
