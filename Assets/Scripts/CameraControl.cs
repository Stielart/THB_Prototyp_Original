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

    public float targetX = 3f;
    public float targetY;

    private float distanceFromBoxCenter = 0f;

    private float transitionStartX;
    private float transitionStartY;
    private float transitionStartTimeX;
    private float transitionStartTimeY;

    private const float transitionDurationX = 0.4f;
    private const float transitionDurationY = 0.4f;

    public bool isXFrozen = false;


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
                UpdateTransitionX();
                UpdateTransitionY();
        
   
        transform.localPosition = new Vector2(currentOffsetX, currentOffsetY);

    }

    private void UpdateTargetX()
    {

        float previousTargetX = targetX;
        bool isNormalGroundedOrAir = currentState == player.groundState
                                    || currentState == player.jumpState
                                    || currentState == player.fallState
                                    || currentState == player.wallJumpState;
        if (!isXFrozen)
        {
            targetX = transform.localPosition.x; // Kamera bleibt an der aktuellen X-Position


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
        }



        Debug.Log("targetX: " + targetX);

        if (targetX != previousTargetX)
        {
            StartTransitionX(targetX);
        }



    }


    private void UpdateTargetY()
    {
        float previousTargetY = targetY;


        bool isFallingOrWallFalling = currentState == player.fallState || currentState == player.wallSlideFallState;
        bool isWallSliding = currentState == player.wallSlideState;
        if (isFallingOrWallFalling)
        {
            targetY = downOffsetYValue;
        }
        else if(currentState == player.groundState)
        {
            targetY = 0f;
        }

        else if (isWallSliding)
        {
            targetY = upOffsetYValue;
        }
       

        if (targetY != previousTargetY)
        {
            StartTransitionY(targetY);
        }




    }


    

    void UpdateBoxPosition()
    {

        if (isXFrozen) return;
        distanceFromBoxCenter = player.transform.position.x - deadZoneX;

        if (distanceFromBoxCenter > deadZoneRightThreshold)
            deadZoneX = player.transform.position.x - deadZoneRightThreshold; // Box wird geschoben, Spieler bleibt GENAU an der Wand
        else if (distanceFromBoxCenter < -deadZoneLeftThreshold)
            deadZoneX = player.transform.position.x + deadZoneLeftThreshold;
    }

    void StartTransitionX(float newTargetX)
    {
        transitionStartX = currentOffsetX; // "wo bin ich JETZT" wird als neuer Ausgangspunkt festgehalten
        transitionStartY = currentOffsetY; // "wo bin ich JETZT" wird als neuer Ausgangspunkt festgehalten
        transitionStartTimeX = Time.time;    // "JETZT" als Startzeitpunkt merken
        targetX = newTargetX;               // das eigentliche Ziel setzen
                    // das eigentliche Ziel setzen
    }

    void StartTransitionY(float newTargetY)
    {
        transitionStartY = currentOffsetY; // "wo bin ich JETZT" wird als neuer Ausgangspunkt festgehalten
        transitionStartTimeY = Time.time;    // "JETZT" als Startzeitpunkt merken
        targetY = newTargetY;               // das eigentliche Ziel setzen
    }

    void UpdateTransitionX()
    {
        float elapsedX = Time.time - transitionStartTimeX; // wie viele Sekunden sind seit dem Start vergangen?
        float progressX = Mathf.Clamp01(elapsedX / transitionDurationX); // als Prozentsatz (0 bis 1) der Gesamtdauer

        currentOffsetX = Mathf.Lerp(transitionStartX, targetX, progressX);

        
    }

    void UpdateTransitionY()
    {
        float elapsedY = Time.time - transitionStartTimeY; // wie viele Sekunden sind seit dem Start vergangen?
        float progressY = Mathf.Clamp01(elapsedY / transitionDurationY); // als Prozentsatz (0 bis 1) der Gesamtdauer
        if (currentState != player.groundState)
            currentOffsetY = Mathf.Lerp(transitionStartY, targetY, progressY);
        else
            currentOffsetY = 0f;
    }

    public void FreezeXAt(float worldX)
    {
        isXFrozen = true;
        float relativeTarget = worldX - player.transform.position.x;
        if (relativeTarget != targetX)
        {
            targetX = relativeTarget;
            deadZoneX = relativeTarget + worldX;
            StartTransitionX(targetX);
        }
    }

    public void UnfreezeX()
    {
        isXFrozen = false;
        deadZoneX = player.transform.position.x;
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
