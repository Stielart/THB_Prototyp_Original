using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private const float PixelsPerUnit = 16f;
    public const float PhysicsFPS = 60f;

    public Rigidbody2D rb { get; private set; }
    public Animator anim;
    public PlayerInput playerInput { get; private set; }

    



    // === STATE MACHINE ===
    public PlayerState currentState;

    public PlayerState previousState;
    public PlayerGroundState groundState;

    public PlayerAirState airState;

    public PlayerFallState fallState;
    public PlayerJumpState jumpState;
    public PlayerWallSlideState wallSlideState;

    public PlayerWallJumpState wallJumpState;

    public PlayerWallSlideFallState wallSlideFallState;

    // === INPUT (public, damit States zugreifen können) ===
    public float xInput;
    public float yInput;

    private Vector2 moveInput;

    [Header("Raycast Positions")]
    [SerializeField] private Transform airbornePosL;
    [SerializeField] private Transform airbornePosR;

    [SerializeField] public Transform ceilingCheckR;
    [SerializeField] public Transform ceilingCheckL;

    // === STATE BOOLS (public für States) ===
    public bool isGrounded = true;
    
    public bool isWallDetected = false;
    public bool isRunning = false;
    public bool isFacingRight = true;

    public bool isHoldingJump = false;

    public bool isTurningAround = false;

    public bool apexAnimationFinished = false;

    public bool fallAnimationFinished = false;

    public bool turningLocked = false;


    [Header("Speed in Pixels per second")]

    [SerializeField] public float pixelWalkSpeed = 75f;
    [SerializeField] public float pixelRunSpeed = 135f;

    [SerializeField] public float pixelWalkAcceleration = 337.5f;
    [SerializeField] public float pixelWalkDeceleration = 562.5f;

    [SerializeField] public float pixelTurnDeceleration = 1125f;

    [SerializeField] public float pixelWallJumpSpeedX = 300f;

    [SerializeField] public float pixelWallJumpSpeedY = 150f;


    [Header("Fall speed in Pixels per second")]
    [SerializeField] public float pixelFallMaxSpeed = 800f;

    [SerializeField] public float pixelFallWallSlideMaxSpeed = 30f;

    [SerializeField] public float pixelFallWallJumpMaxSpeed = 30f;

    [Header("Fall speed while holding jump in Pixels per second")]

    [SerializeField] public float pixelFallHoldJumpMaxSpeed = 550f;

    [SerializeField] public float pixelFallWallSlideHoldJumpMaxSpeed = 20f;

    [SerializeField] public float pixelFallWallJumpHoldJumpMaxSpeed = 20f;


    public float walkMaxSpeedUnit;
    public float runMaxSpeedUnit;
    public float jumpHeightUnit;

    public float wallJumpSpeedXUnit;
    public float wallJumpSpeedYUnit;

    public float wallSlideSpeedUnit;
    public float walkAccelerationUnit;
    public float walkDecelerationUnit;
    public float turnDecelerationUnit;

    public float wallJumpTurnDecelerationUnit;

    public float wallJumpDecelerationUnit;

    public float inputLockedUntil = -1f; // Zeitpunkt (Time.time), bis zu dem Input ignoriert wird
    public float postBounceInputLockDuration = 0.1f; // Dauer, für die Input nach einem Wall-Bounce ignoriert wird
    #region "Fall Speed in Units per second"
    public float fallMaxSpeedUnit;
    public float fallWallSlideMaxSpeedUnit;

    public float fallWallJumpMaxSpeedUnit;

    public float fallHoldJumpMaxSpeedUnit;

    public float fallWallSlideHoldJumpMaxSpeedUnit;

    public float fallWallJumpHoldJumpMaxSpeedUnit;

    #endregion


    [Header("Momentum Jump Settings")]
    [SerializeField] public float pixelBaseJumpHeight = 90f;
    [SerializeField] public float pixelMaxJumpBonus = 50f;
    [SerializeField] public float pixelMinSpeedForBonus = 100f;

    public float minSpeedForBonusUnit;

    [Header("Jump Settings")]


    [SerializeField] public float pixelApexFrames = 18f;

    [SerializeField] public float jumpCutMultiplier = 0.5f;

    [SerializeField] public float jumpCutGraceTime = 0.1f;

    public float jumpVelocity;

    public float jumpGravityUnit { get; private set; }

    public float wallSlideGravityUnit { get; private set; }

    public float wallJumpGravityUnit { get; private set; }
    public float wallJumpVelocityX { get; private set; }

    public float wallJumpVelocityY { get; private set; }

    public float fallGravityUnit { get; private set; }

    public float wallSlideFallGravityUnit { get; private set; }
    [Header("Jump modifiers")]
    
    [SerializeField] public float airAccelerationMultiplier = 0.75f;
    [SerializeField] public float airDecelerationMultiplier = 0.05f; 
    [SerializeField] public float airTurnMultiplier = 0.65f;

    [SerializeField] public float wallJumpTurnMultiplier = 0.2f;

    [SerializeField] public float wallJumpDecelerationMultiplier = 0.5f;

    [SerializeField] public float wallJumpAccelerationMultiplier = 1.6f;

    [SerializeField] public float wallJumpGravityMod = 0.2f;

    [SerializeField] public float wallJumpHoldGravityMod = 0.5f;
    [Header("Wall Slide Settings")]

    [SerializeField] public float wallSlideSpeedModifier = 1.5f;
     public float pixelWallSlideSpeed;

    public float pixelWallSlideMinSpeed = 80f;

    [SerializeField] public float stickDuration = 5f;

    [Header("Apex Hang Settings")]
    [SerializeField] public float apexHangMultiplier = 0.2f;  // Gravity auf 20% reduzieren
    [SerializeField] public float apexHangRange = 1.5f;

    [SerializeField] public float apexHangMinTime = 0.15f;

    [Space(10)]

    [Header("Buffer jump settings")]
   
    public const float jumpBufferDuration = 0.15f;

    public float coyoteTimeDuration = 0.25f;

    public BufferTimer jumpBuffer;

    public BufferTimer coyoteTimer;

    public BufferTimer stickTimer;


    public float airAccelerationUnit;
    public float airDecelerationUnit;
    public float airTurnDecelerationUnit;
    public float wallJumpAccelerationUnit;

    public float wallSlideMinSpeedUnit;

    [SerializeField] public float fallGravityMod = 1.5f;


    [SerializeField] public float wallSlideGravityMod = 1.2f;

    [SerializeField] public float wallSlideFallGravityMod = 0.5f;




    // === MOVEMENT SETTINGS (public für States) ===


    #region "Current Movement Settings"
    [Header("Current Movement Settings")]

    public float currentGravity;

    public float currentSpeed = 0f;

    public float currentJumpVelocity;

   public float currentDeceleration;

    public float currentTurnDeceleration;

    public float currentAcceleration;

    #endregion

    private BoxCollider2D bc;


    [Header("Raycast Settings")]

    [SerializeField] private Vector2 groundCheckOffset;

    [SerializeField] private Vector2 ceilingCheckOffsetL;
    [SerializeField] private Vector2 ceilingCheckOffsetR;

    [SerializeField] private Vector2 wallCheckOffsetR;

    [SerializeField] private Vector2 wallCheckOffsetL;

    [SerializeField] public float groundRaycastLength = 0.1f;

    [SerializeField] public float wallRaycastLength = 0.1f;
    [SerializeField] public float ceilingRaycastLength = 0.5f;

    [SerializeField] public LayerMask groundMask;

    [Header("Animation Curves")]
    [SerializeField] public AnimationCurve jumpCutCurve;

    [SerializeField] public AnimationCurve wallJumpGravityCurve;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bc = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        playerInput = new PlayerInput();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;



        #region "State Machine AddComponents"
        // === STATE MACHINE INITIALISIERUNG ===

        groundState = gameObject.AddComponent<PlayerGroundState>();
        airState = gameObject.AddComponent<PlayerAirState>();
        jumpState = gameObject.AddComponent<PlayerJumpState>();
        fallState = gameObject.AddComponent<PlayerFallState>();
        wallSlideState = gameObject.AddComponent<PlayerWallSlideState>();
        wallJumpState = gameObject.AddComponent<PlayerWallJumpState>();
        wallSlideFallState = gameObject.AddComponent<PlayerWallSlideFallState>();

        #endregion


        #region "State Machine Initialize"
        // States initialisieren
        groundState.Initialize(this);
        airState.Initialize(this);
        jumpState.Initialize(this);
        fallState.Initialize(this);
        wallSlideState.Initialize(this);
        wallJumpState.Initialize(this);
        wallSlideFallState.Initialize(this);

        #endregion
        // Start-State setzen
        currentState = groundState;
        currentState.Enter();
       
    }
    void Start()
    { //Pixel values in Units umrechnen
        jumpGravityUnit =
    PixelsToUnits(
        CalculateGravity(pixelBaseJumpHeight, pixelApexFrames, PhysicsFPS)
    );
        fallGravityUnit = jumpGravityUnit * fallGravityMod;  // Fallgeschwindigkeit ist 1,5x schneller als Aufstieg
        wallSlideGravityUnit = jumpGravityUnit * wallSlideGravityMod;
        wallJumpGravityUnit = jumpGravityUnit * wallJumpGravityMod;
        wallSlideFallGravityUnit = jumpGravityUnit * wallSlideFallGravityMod;
        //Debug.Log($"Jump Gravity: {jumpGravityUnit}, Fall Gravity: {fallGravityUnit}");
        walkMaxSpeedUnit = PixelsPerSecondToUnits(pixelWalkSpeed);
        runMaxSpeedUnit = PixelsPerSecondToUnits(pixelRunSpeed);



        fallMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallMaxSpeed);
        fallWallSlideMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallWallSlideMaxSpeed);
        fallWallJumpMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallWallJumpMaxSpeed);
        fallHoldJumpMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallHoldJumpMaxSpeed);
        fallWallSlideHoldJumpMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallWallSlideHoldJumpMaxSpeed);
        fallWallJumpHoldJumpMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallWallJumpHoldJumpMaxSpeed);
        wallSlideMinSpeedUnit = PixelsPerSecondToUnits(pixelWallSlideMinSpeed);

        walkAccelerationUnit =
            PixelsPerSecondSquaredToUnits(pixelWalkAcceleration);

        walkDecelerationUnit =
            PixelsPerSecondSquaredToUnits(pixelWalkDeceleration);

        turnDecelerationUnit =
            PixelsPerSecondSquaredToUnits(pixelTurnDeceleration);

        minSpeedForBonusUnit = 
            PixelsPerSecondToUnits(pixelMinSpeedForBonus);

        wallJumpSpeedXUnit = 
            PixelsPerSecondToUnits(pixelWallJumpSpeedX);
        // Debug.Log("runMaxSpeedUnit: " + runMaxSpeedUnit); 
        // Debug.Log("wallJumpSpeedXUnit: " + wallJumpSpeedXUnit);

        wallJumpDecelerationUnit = PixelsPerSecondSquaredToUnits(pixelWalkDeceleration * wallJumpDecelerationMultiplier);
        wallJumpAccelerationUnit = PixelsPerSecondSquaredToUnits(pixelWalkAcceleration * wallJumpAccelerationMultiplier);

        wallJumpSpeedYUnit = 
            PixelsPerSecondToUnits(pixelWallJumpSpeedY);


        airAccelerationUnit = walkAccelerationUnit * airAccelerationMultiplier;
        airDecelerationUnit = walkDecelerationUnit * airDecelerationMultiplier;
        airTurnDecelerationUnit = turnDecelerationUnit * airTurnMultiplier;
        wallJumpTurnDecelerationUnit = turnDecelerationUnit * wallJumpTurnMultiplier;

        currentGravity = jumpGravityUnit;
        wallSlideGravityUnit = jumpGravityUnit * wallSlideGravityMod;
        wallJumpSpeedXUnit = PixelsPerSecondToUnits(pixelWallJumpSpeedX);


    }
    void Update()
    {
        // === INPUT LESEN ===
        xInput = playerInput.ingame.Movement.ReadValue<Vector2>().x;
        yInput = playerInput.ingame.Movement.ReadValue<Vector2>().y;
        HandleInputTurn(xInput);
        isRunning = playerInput.ingame.speedUp.IsPressed();
        isHoldingJump = playerInput.ingame.Jump.IsPressed();

        // === STATE UPDATE ===
        currentState?.Do();  // Ruft die Do()-Methode des aktuellen States auf
    }

    void FixedUpdate()
    {
        jumpBuffer.Tick(Time.fixedDeltaTime);
        coyoteTimer.Tick(Time.fixedDeltaTime);
       // stickTimer.Tick(Time.fixedDeltaTime);
        currentState?.FixedDo();  // Ruft die FixedDo()-Methode des aktuellen States auf
    }

    private void OnEnable()
    {
        playerInput.Enable();
        //playerInput.ingame.Jump.performed += ctx => JumpButton();
        playerInput.ingame.Jump.started += OnJumpStarted;
        

    }

    private void OnDisable()
    {
        playerInput.Disable();
        playerInput.ingame.Jump.started -= OnJumpStarted;
    }

  





    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        // Debug.Log("OnJumpStarted feuert!");
        // Wird garantiert nur einmal pro Tastendruck ausgelöst,
        // egal wie lange danach gehalten wird
        jumpBuffer.Start(jumpBufferDuration);
    }









    public void Turn(bool turnRight)
    {
        if (turnRight)
        {
            isFacingRight = true;
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            

        }
        else
        {
            isFacingRight = false;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            
        }



    }

    private void HandleInputTurn(float xInput)
    {
        if (turningLocked == true)
            return;  
      

        if (xInput > 0 && !isFacingRight)
        {

            Turn(true);
        }
        else if (xInput < 0 && isFacingRight)
        {
            Turn(false);
        }
    }


    public void SwitchState(PlayerState newState)
    {
        // Debug.Log($"=== SwitchState: {currentState.GetType().Name} -> {newState.GetType().Name} ===");
        currentState.Exit();
        previousState = currentState;
        currentState = newState;
        currentState.Enter();
        


    }

    private void OnDrawGizmos()
    {
        if (isGrounded) 
            Gizmos.color = Color.green;


        else
            Gizmos.color = Color.red;
       
       
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundRaycastLength);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * wallRaycastLength);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * wallRaycastLength);

       // Gizmos.DrawLine(ceilingCheckL.position, ceilingCheckL.position + Vector3.up * ceilingRaycastLength);
       // Gizmos.DrawLine(ceilingCheckR.position, ceilingCheckR.position + Vector3.up * ceilingRaycastLength);

    }


    #region "Raycast Checks"

    public bool CheckGround()
    {
        Vector2 origin = (Vector2)transform.position + groundCheckOffset;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundRaycastLength, groundMask);
        Debug.DrawRay(origin, Vector2.down * groundRaycastLength, hit.collider != null ? Color.green : Color.red);
        return hit.collider != null;
    }

    /// <summary>
    /// Prüft per Raycast, ob die Decke links und/oder rechts blockiert ist.
    /// </summary>
    /// <returns>
    /// -1 = linke Seite blockiert, 1 = rechte Seite blockiert, 0 = beide Seiten blockiert oder frei.
    /// </returns>
    public int CheckCeiling()
    {
        int ceilingCheck = 0;
        Vector2 origin = (Vector2)transform.position + ceilingCheckOffsetL;
        RaycastHit2D hitL = Physics2D.Raycast(origin, Vector2.up, ceilingRaycastLength, groundMask);
        origin = (Vector2)transform.position + ceilingCheckOffsetR;
        RaycastHit2D hitR = Physics2D.Raycast(origin, Vector2.up, ceilingRaycastLength, groundMask);
        Debug.DrawRay((Vector2)transform.position + ceilingCheckOffsetL, Vector2.up * ceilingRaycastLength, hitL.collider != null ? Color.green : Color.red);
        Debug.DrawRay((Vector2)transform.position + ceilingCheckOffsetR, Vector2.up * ceilingRaycastLength, hitR.collider != null ? Color.green : Color.red);

        if (hitL.collider == true && hitR.collider == false)
        {
            ceilingCheck = -1; // Linke Seite ist blockiert
        }
        else if (hitR.collider == true && hitL.collider == false)
        {
            ceilingCheck = 1; // Rechte Seite ist blockiert
        }
        else if ((hitL.collider == true && hitR.collider == true) || (hitL.collider == false && hitR.collider == false))
        {
            ceilingCheck = 0; // Beide Seiten sind blockiert oder frei
        }

        return ceilingCheck;
    }


    public float CheckWallSite()
    {
        float direction = 0f;
        if (CheckWallL()) 
            direction = 1f;
        else if (CheckWallR()) 
            direction = -1f;
        else 
            direction = 0f;
        //Debug.Log($"CheckWallSite() called: WallL={CheckWallL()}, WallR={CheckWallR()}, direction={direction}");
        return direction;
    }

    public bool CheckWallL()
    {
        
        Vector2 origin = (Vector2)transform.position + wallCheckOffsetL;
        Vector2 direction = Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallRaycastLength, groundMask);
        Debug.DrawRay(origin, direction * wallRaycastLength, hit.collider != null ? Color.green : Color.red);
        // Debug.Log($"Hit = {hit.collider != null}");
        return hit.collider != null;
    }
    public bool CheckWallR()
    {
        Vector2 origin = (Vector2)transform.position + wallCheckOffsetR;
        Vector2 direction = Vector2.right;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallRaycastLength, groundMask);
        Debug.DrawRay(origin, direction * wallRaycastLength, hit.collider != null ? Color.green : Color.red);
        return hit.collider != null;
    }


    #endregion
    public float CalculateGravity(float jumpHeight, float apexFrames, float PhysicsFPS)
    {

        float apexTime = apexFrames / PhysicsFPS;

        // Debug.Log($"Calculated Gravity: {(2f * jumpHeight) / (apexTime * apexTime)}");
        return (2f * jumpHeight) / (apexTime * apexTime);
    }


    public float CalculateJumpVelocity(float jumpHeight, float apexFrames, float PhysicsFPS)
    {
        float gravity = CalculateGravity(
            jumpHeight,
            apexFrames,
            PhysicsFPS
        );

        float apexTime = apexFrames / PhysicsFPS;

        return gravity * apexTime;
    }
    public float CalculateSpeedBasedJumpHeight(float currentSpeed, float runMaxSpeedUnit)
    {
        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(currentSpeed) / runMaxSpeedUnit);
        float bonusHeight;

        if (Mathf.Abs(currentSpeed) < minSpeedForBonusUnit)
            bonusHeight = 0;

        else
        {
            bonusHeight = normalizedSpeed * pixelMaxJumpBonus;
        }

        bonusHeight = Mathf.Clamp(bonusHeight, 0, pixelMaxJumpBonus);
        float actualJumpHeight;

        return actualJumpHeight = pixelBaseJumpHeight + bonusHeight;

    }

    public float PixelsToUnits(float pixels)
    {
        return pixels / PixelsPerUnit;
    }

    public float UnitsToPixels(float units)
    {
        return units * PixelsPerUnit;
    }

    public float PixelsPerSecondToUnits(float value)
    {
        return value / PixelsPerUnit;
    }

    public float PixelsPerSecondSquaredToUnits(float value)
    {
        return value / PixelsPerUnit;
    }


    private void OnEndJumpAnimationDone()
    {
        // wird automatisch aufgerufen, sobald der Animator den markierten Frame erreicht
        apexAnimationFinished = true;
    }

    private void OnEndFallAnimationDone()
    {
        // wird automatisch aufgerufen, sobald der Animator den markierten Frame erreicht
        fallAnimationFinished = true;
    }


    //private void OnDrawGizmosSelected() { 


    //    if (CheckNoGround())
    //        Gizmos.color = Color.yellow;

    //    else
    //        Gizmos.color = Color.blue;
    //    Gizmos.DrawLine(airbornePosL.transform.position, airbornePosL.transform.position + Vector3.down * groundRaycastLength);

    //    if(CheckNoGround())
    //        Gizmos.color = Color.red;
    //    else
    //        Gizmos.color = Color.green;
    //    Gizmos.DrawLine(airbornePosR.transform.position, airbornePosR.transform.position + Vector3.down * groundRaycastLength);
    //}



    //public bool CheckNoGround()
    //{

    //    RaycastHit2D hit = Physics2D.Raycast(
    //        airbornePosL.position,
    //        Vector2.down,
    //        groundRaycastLength,
    //        groundMask
    //    );
    //    if(hit.collider == null)
    //    {
    //        return false;  // Wenn nichts getroffen → true
    //    }

    //    // 
    //    RaycastHit2D hit2 = Physics2D.Raycast(
    //        airbornePosR.position,
    //        Vector2.down,
    //        this.groundRaycastLength,
    //        this.groundMask
    //    );
    //    return hit2.collider == null;  // Wenn nichts getroffen → true

    //}



}


