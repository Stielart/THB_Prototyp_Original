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

    [Header("Vertikale Dead Zone (wie die horizontale: Solange die Figur im Kasten bleibt, bewegt sich die Kamera nicht)")]
    [Tooltip("Wie weit die Figur über die Kastenmitte steigen darf, bevor die Kamera nach oben folgt. Kleiner = man sieht beim Sprung mehr von dem, was darüber liegt")]
    [SerializeField] public float deadZoneUpThreshold = 1.5f;
    [Tooltip("Wie weit die Figur unter die Kastenmitte fallen darf, bevor die Kamera nach unten folgt")]
    [SerializeField] public float deadZoneDownThreshold = 2f;
    [Tooltip("Glättung der senkrechten Kamerabewegung in Sekunden (siehe CameraAncle). Größer = träger")]
    [SerializeField] public float verticalSmoothTime = 0.12f;

    [Header("Blick nach unten beim Fallen")]
    [Tooltip("So lange (in Sekunden) muss die Figur gefallen sein, bevor die Kamera nach unten blickt. Kurze Absätze bewegen die Kamera nicht")]
    [SerializeField] private float lookDownDelay = 0.25f;
    [Tooltip("Dauer des Wechsels in den Blick nach unten (Sekunden)")]
    [SerializeField] private float lookDownDuration = 0.4f;
    [Tooltip("Dauer der Rückkehr in die Mitte nach der Landung (Sekunden)")]
    [SerializeField] private float returnDuration = 0.8f;


    [Header("Start der Szene")]
    [Tooltip("So viele Bilder lang wird die Kamera beim Start direkt auf ihr Ziel gesetzt (ohne Gleiten aus der alten Position)")]
    [SerializeField] private int startSnapFrames = 3;

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
    private float transitionDurationY = 0.4f;
    private float fallTime;

    public bool isXFrozen = false;


    public bool isLockedRight = true;

    private PlayerState currentState;

    private PlayerState previousState;
    public float deadZoneX;
    public float deadZoneY;
   
    void Awake()
    {
        deadZoneX = player.transform.position.x;
        deadZoneY = player.transform.position.y;


        currentState = player.currentState;
        previousState = currentState;

        StartCoroutine(SnapCameraAtStart());
    }

    /// <summary>
    /// Beim Start der Szene gleitet die Kamera sonst sichtbar von einer alten Position zur richtigen: Die Kamera (Cinemachine) liest die
    /// Zielposition schon im ersten Bild, bevor CameraControl und CameraAncle sie gesetzt haben, und zieht dann mit ihrer Dämpfung nach.
    /// Deshalb werden hier der Anker und das Ziel sofort auf die Startposition gesetzt, und die Kamera behält für die ersten Bilder
    /// keinen alten Zustand (PreviousStateIsValid = false, Dämpfung des Confiners aus). Danach gelten wieder die Inspector-Werte.
    /// </summary>
    private System.Collections.IEnumerator SnapCameraAtStart()
    {
        Transform anchor = transform.parent;
        if (anchor != null)
            anchor.position = new Vector3(deadZoneX, deadZoneY, anchor.position.z);
        transform.localPosition = new Vector2(currentOffsetX, currentOffsetY);

        CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();
        CinemachineConfiner2D confiner = FindFirstObjectByType<CinemachineConfiner2D>();
        float originalDamping = confiner != null ? confiner.Damping : 0f;
        if (confiner != null)
            confiner.Damping = 0f;

        for (int i = 0; i < startSnapFrames; i++)
        {
            if (vcam != null)
                vcam.PreviousStateIsValid = false;
            yield return null;
        }

        if (confiner != null)
            confiner.Damping = originalDamping;
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



        // Debug.Log("targetX: " + targetX);

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
        // Fallzeit mitzählen: Erst nach lookDownDelay blickt die Kamera nach unten (kurze Absätze lösen nichts aus)
        fallTime = isFallingOrWallFalling ? fallTime + Time.deltaTime : 0f;

        if (isFallingOrWallFalling)
        {
            if (fallTime >= lookDownDelay)
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


    

    void UpdateBoxPositionY()
    {
        // Senkrechter Kasten: Die Kamera folgt erst, wenn die Figur ihn oben oder unten verlässt. Der Kasten wird dann mitgeschoben.
        float distanceY = player.transform.position.y - deadZoneY;

        if (distanceY > deadZoneUpThreshold)
            deadZoneY = player.transform.position.y - deadZoneUpThreshold;
        else if (distanceY < -deadZoneDownThreshold)
            deadZoneY = player.transform.position.y + deadZoneDownThreshold;
    }

    void UpdateBoxPosition()
    {
        UpdateBoxPositionY(); // gilt auch, wenn X eingefroren ist

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
        // (Hier stand früher auch transitionStartY = currentOffsetY: Das setzte die Y-Übergänge mitten im Ablauf zurück und ließ die Kamera springen, sobald sich X-Ziel änderte.)
        transitionStartTimeX = Time.time;    // "JETZT" als Startzeitpunkt merken
        targetX = newTargetX;               // das eigentliche Ziel setzen
                    // das eigentliche Ziel setzen
    }

    void StartTransitionY(float newTargetY)
    {
        transitionStartY = currentOffsetY; // "wo bin ich JETZT" wird als neuer Ausgangspunkt festgehalten
        transitionStartTimeY = Time.time;    // "JETZT" als Startzeitpunkt merken
        targetY = newTargetY;               // das eigentliche Ziel setzen
        // Blick nach unten schnell, Rückkehr (Landung, Wandrutschen) langsamer
        transitionDurationY = newTargetY < currentOffsetY ? lookDownDuration : returnDuration;
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
        // Kein Sprung mehr bei der Landung: Auch am Boden gleitet der Versatz weich zurück auf 0 (Ziel wird in UpdateTargetY gesetzt)
        currentOffsetY = Mathf.Lerp(transitionStartY, targetY, Mathf.SmoothStep(0f, 1f, progressY));
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

        // Senkrechter Kasten (gelbe Linien: oben und unten)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(deadZoneX - 1f, deadZoneY + deadZoneUpThreshold, 0), new Vector3(deadZoneX + 1f, deadZoneY + deadZoneUpThreshold, 0));
        Gizmos.DrawLine(new Vector3(deadZoneX - 1f, deadZoneY - deadZoneDownThreshold, 0), new Vector3(deadZoneX + 1f, deadZoneY - deadZoneDownThreshold, 0));

        // Linke Schwelle (blaue Linie)
        Gizmos.color = Color.blue;
        Vector3 leftThresholdPos = new Vector3(deadZoneX - deadZoneLeftThreshold, transform.position.y - 1f, 0);
        Vector3 leftThresholdPosTop = new Vector3(deadZoneX - deadZoneLeftThreshold, transform.position.y + 1f, 0);
        Gizmos.DrawLine(leftThresholdPos, leftThresholdPosTop);
    }
}
