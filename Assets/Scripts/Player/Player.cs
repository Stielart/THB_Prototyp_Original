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


    [Header("Bewegungsprofile (Tasten 1 bis 3 wechseln, B schaltet aktives Bremsen)")]
    [SerializeField] public MovementProfile[] profiles = MovementProfile.CreateDefaults();
    [SerializeField] public int activeProfileIndex = 1;
    [SerializeField] public bool activeBrakingEnabled = true;

    public MovementProfile ActiveProfile => profiles[activeProfileIndex];

    /// <summary>Höhe der Prototyp-Figur in Pixeln (Collider-Höhe), Grundlage für die Skalierung der Profile.</summary>
    public float figureHeightPx { get; private set; }

    /// <summary>Faktor Prototyp-Figur zu Originalfigur des aktiven Profils.</summary>
    public float profileScale { get; private set; } = 1f;

    // Die folgenden Pixel-Werte werden beim Start und bei jedem Profilwechsel aus dem aktiven
    // Profil gesetzt (ApplyProfile). Änderungen hier im Inspector werden dadurch überschrieben.
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
   
    // ---- Fehlertoleranz (F4): einzeln schaltbar, Dauer in Physik-Frames (60 pro Sekunde) ----
    [Header("Fehlertoleranz (Tasten 4, 5, 6 einzeln, T alle)")]
    [Tooltip("Coyote Time: Sprung noch möglich, nachdem die Figur die Kante verlassen hat")]
    public bool coyoteEnabled = true;
    [Tooltip("Dauer der Coyote Time in Frames")]
    public int coyoteFrames = 6;

    [Tooltip("Jump Buffer: Eine zu früh gedrückte Sprungtaste wird gespeichert und bei der Landung ausgeführt")]
    public bool jumpBufferEnabled = true;
    [Tooltip("Dauer des Jump Buffers in Frames")]
    public int jumpBufferFrames = 9;

    [Tooltip("Ledge Correction: Verfehlt die Figur eine Kante knapp, wird sie auf die Kante gehoben")]
    public bool ledgeCorrectionEnabled = true;
    [Tooltip("Maximale Korrekturhöhe in Pixeln (Prototyp-Pixel, 16 Pixel = 1 Einheit)")]
    public float ledgeCorrectionPixels = 8f;

    [Tooltip("Corner Correction: Stößt der Kopf beim Aufstieg an eine Deckenkante, wird die Figur seitlich daran vorbei geschoben")]
    public bool cornerCorrectionEnabled = true;

    [Header("Gefahren und Respawn (Taste 0 für die Hitbox-Toleranz)")]
    [Tooltip("Hitbox-Toleranz: Die Hitboxen der Stacheln sind kleiner als ihre Grafik. Aus = Hitbox genauso groß wie die Grafik")]
    public bool hitboxToleranceEnabled = true;
    [Tooltip("Größe der Stachel-Hitbox im Verhältnis zur Grafik bei eingeschalteter Toleranz (0,6 = 60 % der Breite und Höhe)")]
    [Range(0.2f, 1f)] public float hitboxToleranceScale = 0.6f;
    [Tooltip("Dauer zwischen Berührung und Respawn in Physik-Frames (60 pro Sekunde)")]
    public int respawnDelayFrames = 24;

    /// <summary>Die Figur ist gerade getroffen und wartet auf den Respawn (keine Eingabe, keine Physik).</summary>
    [HideInInspector] public bool isDead;

    /// <summary>Faktor, mit dem Stacheln ihre Hitbox gegenüber der Grafik verkleinern (1, wenn die Toleranz aus ist).</summary>
    public float HitboxScale => hitboxToleranceEnabled ? hitboxToleranceScale : 1f;

    /// <summary>Versetzt die Figur in den "getroffen"-Zustand (unsichtbar, Physik und Eingabe aus) oder zurück.</summary>
    public void SetDead(bool dead)
    {
        isDead = dead;
        if (dead)
        {
            rb.linearVelocity = Vector2.zero;
            currentSpeed = 0f;
        }
        rb.simulated = !dead;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.enabled = !dead;
    }

    /// <summary>
    /// Setzt die Figur an eine Position zurück und löscht alles, was vom letzten Versuch übrig ist
    /// (Geschwindigkeit, Timer, Sperren). Der Zustand wechselt auf Fall, am Boden erkennt der nächste Frame den Boden.
    /// </summary>
    public void RespawnAt(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        Physics2D.SyncTransforms();

        rb.linearVelocity = Vector2.zero;
        currentSpeed = 0f;
        isTurningAround = false;
        turningLocked = false;
        inputLockedUntil = 0f;
        wallStoppedTime = -10f;
        jumpBuffer.Consume();
        coyoteTimer.Consume();
        SetSpriteTint(Color.white);

        SwitchState(fallState);
        SetDead(false);
    }

    [Header("Spin Dash (Taste 9, nur Profile mit Spin Dash)")]
    [Tooltip("Spin Dash global ein- und ausschalten. Wirkt nur in Profilen, bei denen 'Spin Dash' angehakt ist")]
    public bool spinDashEnabled = true;
    [Tooltip("Farbe der Figur während des Aufladens (wird mit jedem Druck stärker)")]
    public Color spinDashChargeColor = new Color(1f, 0.7f, 0.2f);
    [Tooltip("Farbe, zwischen der die Figur blinkt, wenn die Ladung voll ist")]
    public Color spinDashReadyColor = new Color(1f, 0.25f, 0.25f);

    /// <summary>Spin Dash ist verfügbar: global eingeschaltet und im aktiven Profil angehakt.</summary>
    public bool SpinDashAvailable => spinDashEnabled && ActiveProfile.spinDash;

    // Zustand des Spin Dash, wird vom PlayerGroundState geschrieben und von OnGUI gelesen
    [HideInInspector] public bool spinDashCharging;
    [HideInInspector] public int spinDashPresses;

    private SpriteRenderer spriteRenderer;

    /// <summary>Färbt die Figur ein (Weiß = Originalfarben).</summary>
    public void SetSpriteTint(Color color)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    private string SpinDashHudText()
    {
        if (!ActiveProfile.spinDash) return "nicht in diesem Profil";
        if (!spinDashEnabled) return "aus";
        int full = Mathf.Max(1, ActiveProfile.spinDashPressesForFull);
        if (spinDashCharging)
            return spinDashPresses >= full ? "BEREIT, nach oben loslassen" : $"lädt {spinDashPresses}/{full}";
        return "an (im Stand unten halten + Sprung)";
    }

    [Header("Wandmechanik (Taste 8)")]
    [Tooltip("Wandrutschen und Wandsprung. Ausgeschaltet bleibt die Figur an Wänden im normalen Sprung- oder Fall-Zustand (zum Testen der Ledge Correction)")]
    public bool wallMechanicsEnabled = true;

    /// <summary>Wirksame Coyote-Dauer in Frames (0, wenn ausgeschaltet).</summary>
    public int CoyoteFrames => coyoteEnabled ? Mathf.Max(0, coyoteFrames) : 0;

    /// <summary>Wirksame Jump-Buffer-Dauer in Frames. Ausgeschaltet gilt ein Tastendruck nur im selben Physik-Frame (1).</summary>
    public int JumpBufferFrames => jumpBufferEnabled ? Mathf.Max(1, jumpBufferFrames) : 1;

    public FrameTimer jumpBuffer;

    public FrameTimer coyoteTimer;

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

    /// <summary>Geschwindigkeit im Moment, als die Figur an einer Wand gestoppt wurde (für die Ledge Correction).</summary>
    [HideInInspector] public float wallStoppedSpeed;
    [HideInInspector] public float wallStoppedTime = -10f;

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
    [Tooltip("Abstand der äußeren Boden-Strahlen von der Collider-Kante nach innen (Einheiten, 16 Pixel = 1 Einheit)")]
    [SerializeField] public float groundRayEdgeInset = 0.05f;

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
        figureHeightPx = bc.size.y * Mathf.Abs(transform.lossyScale.y) * PixelsPerUnit;
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

        // Messmodus (Taste M), siehe MeasurementRecorder
        gameObject.AddComponent<MeasurementRecorder>().Initialize(this);

        // Start-State setzen
        currentState = groundState;
        currentState.Enter();
       
    }
    void Start()
    { //Pixel values in Units umrechnen
        if (profiles == null || profiles.Length == 0)
            profiles = MovementProfile.CreateDefaults();
        ApplyProfile(activeProfileIndex);
        //Debug.Log($"Jump Gravity: {jumpGravityUnit}, Fall Gravity: {fallGravityUnit}");



        fallMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallMaxSpeed);
        fallWallSlideMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallWallSlideMaxSpeed);
        fallWallJumpMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallWallJumpMaxSpeed);
        fallHoldJumpMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallHoldJumpMaxSpeed);
        fallWallSlideHoldJumpMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallWallSlideHoldJumpMaxSpeed);
        fallWallJumpHoldJumpMaxSpeedUnit = PixelsPerSecondToUnits(pixelFallWallJumpHoldJumpMaxSpeed);
        wallSlideMinSpeedUnit = PixelsPerSecondToUnits(pixelWallSlideMinSpeed);

        minSpeedForBonusUnit = 
            PixelsPerSecondToUnits(pixelMinSpeedForBonus);

        wallJumpSpeedXUnit = 
            PixelsPerSecondToUnits(pixelWallJumpSpeedX);
        // Debug.Log("runMaxSpeedUnit: " + runMaxSpeedUnit); 
        // Debug.Log("wallJumpSpeedXUnit: " + wallJumpSpeedXUnit);

        wallJumpSpeedYUnit = 
            PixelsPerSecondToUnits(pixelWallJumpSpeedY);


        currentGravity = jumpGravityUnit;
        wallSlideGravityUnit = jumpGravityUnit * wallSlideGravityMod;
        wallJumpSpeedXUnit = PixelsPerSecondToUnits(pixelWallJumpSpeedX);


    }
    /// <summary>
    /// Übernimmt ein Bewegungsprofil: schreibt die Profilwerte in die Pixel-Felder und
    /// rechnet alle abhängigen Unity-Einheiten neu.
    /// </summary>
    public void ApplyProfile(int index)
    {
        activeProfileIndex = Mathf.Clamp(index, 0, profiles.Length - 1);
        MovementProfile p = profiles[activeProfileIndex];

        // Profilwerte stehen in Spielpixeln des Originalspiels. Der Faktor rechnet sie auf die Größe der Prototyp-Figur um.
        profileScale = figureHeightPx / p.figureHeight;

        pixelWalkSpeed = p.walkSpeed * profileScale;
        pixelRunSpeed = p.runSpeed * profileScale;
        pixelWalkAcceleration = p.acceleration * profileScale;
        pixelWalkDeceleration = p.passiveDeceleration * profileScale;
        pixelTurnDeceleration = p.activeBraking * profileScale;
        airAccelerationMultiplier = p.airAccelerationMultiplier;
        airDecelerationMultiplier = p.airDecelerationMultiplier;
        airTurnMultiplier = p.airTurnMultiplier;

        pixelBaseJumpHeight = p.jumpHeightFigures * figureHeightPx;
        pixelApexFrames = p.apexFrames;
        apexHangMultiplier = p.apexHangMultiplier;
        apexHangRange = PixelsPerSecondToUnits(p.apexHangSpeed * profileScale);
        fallGravityMod = p.fallGravityMultiplier;

        RecalculateMovementUnits();
        RecalculateGravity();
    }

    /// <summary>
    /// Berechnet die Gravitationen aus Sprunghöhe und Frames bis zum Zenit
    /// (vorher direkt in Start(), jetzt ausgelagert, damit ein Profilwechsel sie neu berechnen kann).
    /// </summary>
    private void RecalculateGravity()
    {
        jumpGravityUnit =
            PixelsToUnits(
                CalculateGravity(pixelBaseJumpHeight, pixelApexFrames, PhysicsFPS)
            );
        fallGravityUnit = jumpGravityUnit * fallGravityMod;
        wallSlideGravityUnit = jumpGravityUnit * wallSlideGravityMod;
        wallJumpGravityUnit = jumpGravityUnit * wallJumpGravityMod;
        wallSlideFallGravityUnit = jumpGravityUnit * wallSlideFallGravityMod;
    }

    /// <summary>
    /// Rechnet die profilabhängigen Pixel-Werte in Unity-Einheiten um
    /// (vorher direkt in Start(), jetzt ausgelagert, damit ein Profilwechsel sie neu berechnen kann).
    /// </summary>
    private void RecalculateMovementUnits()
    {
        walkMaxSpeedUnit = PixelsPerSecondToUnits(pixelWalkSpeed);
        runMaxSpeedUnit = PixelsPerSecondToUnits(pixelRunSpeed);

        walkAccelerationUnit =
            PixelsPerSecondSquaredToUnits(pixelWalkAcceleration);

        walkDecelerationUnit =
            PixelsPerSecondSquaredToUnits(pixelWalkDeceleration);

        turnDecelerationUnit =
            PixelsPerSecondSquaredToUnits(pixelTurnDeceleration);

        wallJumpDecelerationUnit = PixelsPerSecondSquaredToUnits(pixelWalkDeceleration * wallJumpDecelerationMultiplier);
        wallJumpAccelerationUnit = PixelsPerSecondSquaredToUnits(pixelWalkAcceleration * wallJumpAccelerationMultiplier);

        airAccelerationUnit = walkAccelerationUnit * airAccelerationMultiplier;
        airDecelerationUnit = walkDecelerationUnit * airDecelerationMultiplier;
        airTurnDecelerationUnit = turnDecelerationUnit * airTurnMultiplier;
        wallJumpTurnDecelerationUnit = turnDecelerationUnit * wallJumpTurnMultiplier;
    }

    /// <summary>
    /// Setzt die Profile auf die Standardwerte aus MovementProfile.CreateDefaults() zurück.
    /// Nötig, wenn die Szene gespeichert wurde, bevor neue Felder dazukamen (Unity behält dann alte Werte).
    /// Aufruf im Inspector: Zahnrad/drei Punkte der Player-Komponente.
    /// </summary>
    [ContextMenu("Profile auf Standardwerte zurücksetzen")]
    private void ResetProfilesToDefaults()
    {
        profiles = MovementProfile.CreateDefaults();
    }

    /// <summary>
    /// Testtasten: 1 bis 3 wechseln das Bewegungsprofil, B schaltet das aktive Bremsen am Boden um.
    /// </summary>
    private void HandleProfileHotkeys()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.digit1Key.wasPressedThisFrame && profiles.Length > 0) ApplyProfile(0);
        if (kb.digit2Key.wasPressedThisFrame && profiles.Length > 1) ApplyProfile(1);
        if (kb.digit3Key.wasPressedThisFrame && profiles.Length > 2) ApplyProfile(2);
        if (kb.bKey.wasPressedThisFrame) activeBrakingEnabled = !activeBrakingEnabled;

        // Fehlertoleranz: 4 Coyote Time, 5 Jump Buffer, 6 Ledge Correction, 7 Corner Correction, T alle zusammen
        if (kb.digit4Key.wasPressedThisFrame) coyoteEnabled = !coyoteEnabled;
        if (kb.digit5Key.wasPressedThisFrame) jumpBufferEnabled = !jumpBufferEnabled;
        if (kb.digit6Key.wasPressedThisFrame) ledgeCorrectionEnabled = !ledgeCorrectionEnabled;
        if (kb.digit7Key.wasPressedThisFrame) cornerCorrectionEnabled = !cornerCorrectionEnabled;
        if (kb.digit8Key.wasPressedThisFrame) wallMechanicsEnabled = !wallMechanicsEnabled;
        if (kb.digit9Key.wasPressedThisFrame) spinDashEnabled = !spinDashEnabled;
        if (kb.digit0Key.wasPressedThisFrame) hitboxToleranceEnabled = !hitboxToleranceEnabled;
        if (kb.tKey.wasPressedThisFrame)
        {
            bool anyOn = coyoteEnabled || jumpBufferEnabled || ledgeCorrectionEnabled || cornerCorrectionEnabled || hitboxToleranceEnabled;
            coyoteEnabled = jumpBufferEnabled = ledgeCorrectionEnabled = cornerCorrectionEnabled = hitboxToleranceEnabled = !anyOn;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 1900, 24),
            $"Profil [1-3]: {ActiveProfile.name} (Skalierung x{profileScale:F2})   |   Aktives Bremsen [B]: {(activeBrakingEnabled ? "an" : "aus")}   |   Wandrutschen und Wandsprung [8]: {(wallMechanicsEnabled ? "an" : "aus")}   |   Spin Dash [9]: {SpinDashHudText()}");
        GUI.Label(new Rect(10, 30, 1900, 24),
            $"Toleranz [T = alle]: Coyote [4] {(coyoteEnabled ? coyoteFrames + " Fr." : "aus")}   |   Buffer [5] {(jumpBufferEnabled ? jumpBufferFrames + " Fr." : "aus")}"
            + $"   |   Ledge [6] {(ledgeCorrectionEnabled ? ledgeCorrectionPixels + " px" : "aus")}   |   Corner [7] {(cornerCorrectionEnabled ? "an" : "aus")}   |   Stachel-Hitbox [0] {Mathf.RoundToInt(HitboxScale * 100f)} % der Grafik");
        if (ledgeCorrectionEnabled)
        {
            bool recent = Time.time - ledgeStatusTime < 0.2f;
            string status = recent ? ledgeStatus : "nicht in der Luft (kein Prüfen am Boden oder beim Wandrutschen)";
            if (Time.time - ledgeLastSuccessTime < 1.5f)
                status = $"KORRIGIERT um {ledgeLastLiftPixels:F1} px  (davor: {status})";
            GUI.Label(new Rect(10, 50, 1900, 24), "Ledge-Prüfung: " + status);
        }
    }

    void Update()
    {
        HandleProfileHotkeys();
        if (isDead)
            return;

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
       // stickTimer.Tick(Time.fixedDeltaTime);
        if (isDead)
            return;
        currentState?.FixedDo();  // Ruft die FixedDo()-Methode des aktuellen States auf

        // Die Frame-Timer zählen nach dem State-Update, damit ein Fenster von n Frames genau n Prüfungen zulässt
        jumpBuffer.Tick();
        coyoteTimer.Tick();
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
        jumpBuffer.Start(JumpBufferFrames);
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
       
       
        if (Application.isPlaying && bc != null)
        {
            GetGroundRayOrigins(out Vector2 gl, out Vector2 gc, out Vector2 gr);
            Gizmos.DrawLine(gl, gl + Vector2.down * groundRaycastLength);
            Gizmos.DrawLine(gc, gc + Vector2.down * groundRaycastLength);
            Gizmos.DrawLine(gr, gr + Vector2.down * groundRaycastLength);
        }
        else
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundRaycastLength);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * wallRaycastLength);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * wallRaycastLength);

       // Gizmos.DrawLine(ceilingCheckL.position, ceilingCheckL.position + Vector3.up * ceilingRaycastLength);
       // Gizmos.DrawLine(ceilingCheckR.position, ceilingCheckR.position + Vector3.up * ceilingRaycastLength);

        DrawLedgeGizmos();
    }


    // ---- Debug-Daten der Ledge Correction (für Gizmos und Bildschirmzeile) ----
    // Stufe: 0 = noch nichts geprüft, 1 = unterer Strahl, 2 = oberer Strahl, 3 = Strahl nach unten, 4 = Platzprüfung
    private int ledgeStage;
    private string ledgeStatus = "";
    private float ledgeStatusTime = -10f;
    private float ledgeLastSuccessTime = -10f;
    private float ledgeLastLiftPixels;
    private Vector2 ledgeDirVec, ledgeLowOrigin, ledgeHighOrigin, ledgeDownOrigin, ledgeDownPoint, ledgeBoxCenter, ledgeBoxSize;
    private float ledgeReachUsed, ledgeDownLength;
    private bool ledgeLowHit, ledgeHighHit, ledgeDownHit, ledgeBoxBlocked;

    private bool LedgeFail(string reason)
    {
        ledgeStatus = reason;
        ledgeStatusTime = Time.time;
        return false;
    }

    /// <summary>
    /// Ledge Correction: Verfehlt die Figur in der Luft eine Plattformkante knapp (Füße unterhalb der Oberkante, aber
    /// höchstens ledgeCorrectionPixels), wird sie auf die Kante gehoben und läuft darüber. Nur wenn nach oben Platz ist
    /// und die Wand oberhalb der Korrekturhöhe nicht weitergeht, sonst ist es eine echte Wand.
    /// Aufruf aus den Luft-States vor der Wand-Erkennung. Jeder Prüfschritt wird für Gizmos und Bildschirmzeile gespeichert.
    /// </summary>
    public bool TryLedgeCorrection()
    {
        ledgeStage = 0;
        if (!ledgeCorrectionEnabled)
            return LedgeFail("aus");
        if (Mathf.Abs(xInput) < 0.5f)
            return LedgeFail("keine Richtung gehalten");

        float dir = Mathf.Sign(xInput);
        Vector2 dir2 = new Vector2(dir, 0f);
        Bounds b = bc.bounds;
        float reach = 0.15f;                              // Abstand vor der Figur, in dem die Kante gesucht wird (Einheiten)
        float maxLift = PixelsToUnits(ledgeCorrectionPixels);
        float edgeX = dir > 0f ? b.max.x : b.min.x;
        float feetY = b.min.y;

        ledgeDirVec = dir2;
        ledgeReachUsed = reach;

        // Schritt 1: Unterer Strahl knapp über den Füßen: trifft er die Kantenwand?
        Vector2 lowOrigin = new Vector2(edgeX - dir * 0.02f, feetY + 0.05f);
        RaycastHit2D low = Physics2D.Raycast(lowOrigin, dir2, reach, groundMask);
        ledgeStage = 1;
        ledgeLowOrigin = lowOrigin;
        ledgeLowHit = low.collider != null;
        if (!ledgeLowHit)
            return LedgeFail("keine Wand vor den Füßen");

        // Schritt 2: Oberer Strahl in Höhe der maximalen Korrektur: muss frei sein, sonst ist die Wand zu hoch
        Vector2 highOrigin = new Vector2(lowOrigin.x, feetY + maxLift + 0.05f);
        ledgeStage = 2;
        ledgeHighOrigin = highOrigin;
        ledgeHighHit = Physics2D.Raycast(highOrigin, dir2, reach, groundMask).collider != null;
        if (ledgeHighHit)
            return LedgeFail("Wand reicht über die Korrekturhöhe hinaus (Füße zu tief unter der Kante)");

        // Schritt 3: Oberkante suchen: von oben nach unten hinter der Kantenwand
        Vector2 downOrigin = new Vector2(low.point.x + dir * 0.05f, feetY + maxLift + 0.05f);
        ledgeDownLength = maxLift + 0.1f;
        RaycastHit2D top = Physics2D.Raycast(downOrigin, Vector2.down, ledgeDownLength, groundMask);
        ledgeStage = 3;
        ledgeDownOrigin = downOrigin;
        ledgeDownHit = top.collider != null;
        ledgeDownPoint = top.point;
        if (!ledgeDownHit)
            return LedgeFail("Oberkante nicht gefunden");

        float lift = top.point.y - feetY + 0.06f;         // Füße knapp über die Kante setzen
        if (lift <= 0f || lift > maxLift + 0.1f)
            return LedgeFail($"Hubhöhe {UnitsToPixels(lift):F1} px liegt außerhalb");

        // Schritt 4: Platz nach oben prüfen: die angehobene Figur darf nirgends im Boden stecken
        Vector2 newCenter = (Vector2)b.center + Vector2.up * lift;
        ledgeStage = 4;
        ledgeBoxCenter = newCenter;
        ledgeBoxSize = b.size * 0.9f;
        ledgeBoxBlocked = Physics2D.OverlapBox(newCenter, ledgeBoxSize, 0f, groundMask) != null;
        if (ledgeBoxBlocked)
            return LedgeFail("über der Kante ist kein Platz für die Figur");

        rb.position += Vector2.up * lift;

        // Tempo behalten: Die Figur wurde gerade erst an der Wand gestoppt (HandleHorizontalMovement setzt currentSpeed auf 0).
        // Nach dem Anheben läuft sie mit der alten Geschwindigkeit über die Kante, wie bei Celeste.
        if (Time.time - wallStoppedTime < 0.1f && Mathf.Sign(wallStoppedSpeed) == dir)
        {
            currentSpeed = wallStoppedSpeed;
            rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);
        }

        ledgeLastSuccessTime = Time.time;
        ledgeLastLiftPixels = UnitsToPixels(lift);
        ledgeStatus = "korrigiert";
        ledgeStatusTime = Time.time;
        return true;
    }

    /// <summary>
    /// Gizmos der Ledge Correction (nur im Play-Modus). Gelbe Linien: Zone, in der die Kantenoberseite liegen muss
    /// (Füße bis Füße plus Korrekturhöhe). Unterer/oberer Strahl: grün = Treffer bzw. frei wie gewünscht, rot = nicht.
    /// Strahl nach unten: grün = Oberkante gefunden. Box: cyan = Platz, rot = blockiert.
    /// </summary>
    private void DrawLedgeGizmos()
    {
        if (!Application.isPlaying || !ledgeCorrectionEnabled || bc == null)
            return;

        // Zone der Korrektur direkt neben der Figur (gelb), immer sichtbar
        Bounds b = bc.bounds;
        float zoneBottom = b.min.y;
        float zoneTop = b.min.y + PixelsToUnits(ledgeCorrectionPixels);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(b.min.x - 0.4f, zoneBottom, 0f), new Vector3(b.max.x + 0.4f, zoneBottom, 0f));
        Gizmos.DrawLine(new Vector3(b.min.x - 0.4f, zoneTop, 0f), new Vector3(b.max.x + 0.4f, zoneTop, 0f));

        if (ledgeStage >= 1)
        {
            Gizmos.color = ledgeLowHit ? Color.green : Color.red;
            Gizmos.DrawLine(ledgeLowOrigin, ledgeLowOrigin + ledgeDirVec * ledgeReachUsed);
        }
        if (ledgeStage >= 2)
        {
            Gizmos.color = ledgeHighHit ? Color.red : Color.green;
            Gizmos.DrawLine(ledgeHighOrigin, ledgeHighOrigin + ledgeDirVec * ledgeReachUsed);
        }
        if (ledgeStage >= 3)
        {
            Gizmos.color = ledgeDownHit ? Color.green : Color.red;
            Gizmos.DrawLine(ledgeDownOrigin, ledgeDownOrigin + Vector2.down * ledgeDownLength);
            if (ledgeDownHit)
                Gizmos.DrawWireSphere(ledgeDownPoint, 0.06f);
        }
        if (ledgeStage >= 4)
        {
            Gizmos.color = ledgeBoxBlocked ? Color.red : Color.cyan;
            Gizmos.DrawWireCube(ledgeBoxCenter, ledgeBoxSize);
        }
    }

    #region "Raycast Checks"

    /// <summary>
    /// Ursprünge der drei Boden-Strahlen: links und rechts am Rand des Colliders (um groundRayEdgeInset nach innen
    /// versetzt) und in der Mitte. Die Höhe kommt weiterhin aus groundCheckOffset.y.
    /// </summary>
    private void GetGroundRayOrigins(out Vector2 left, out Vector2 center, out Vector2 right)
    {
        Bounds b = bc.bounds;
        float y = transform.position.y + groundCheckOffset.y;
        float halfWidth = Mathf.Max(0f, b.extents.x - groundRayEdgeInset);
        left = new Vector2(b.center.x - halfWidth, y);
        center = new Vector2(b.center.x, y);
        right = new Vector2(b.center.x + halfWidth, y);
    }

    /// <summary>
    /// Die Figur gilt als am Boden, solange mindestens einer der drei Strahlen (links, Mitte, rechts) den Boden trifft.
    /// Erst wenn alle drei ins Leere gehen, steht keine Fläche mehr unter dem Collider.
    /// </summary>
    public bool CheckGround()
    {
        GetGroundRayOrigins(out Vector2 left, out Vector2 center, out Vector2 right);
        bool hitL = Physics2D.Raycast(left, Vector2.down, groundRaycastLength, groundMask).collider != null;
        bool hitC = Physics2D.Raycast(center, Vector2.down, groundRaycastLength, groundMask).collider != null;
        bool hitR = Physics2D.Raycast(right, Vector2.down, groundRaycastLength, groundMask).collider != null;
        Debug.DrawRay(left, Vector2.down * groundRaycastLength, hitL ? Color.green : Color.red);
        Debug.DrawRay(center, Vector2.down * groundRaycastLength, hitC ? Color.green : Color.red);
        Debug.DrawRay(right, Vector2.down * groundRaycastLength, hitR ? Color.green : Color.red);
        return hitL || hitC || hitR;
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
        // Profil ohne Momentum-Sprung: immer die Basis-Sprunghöhe (Sprung in allen Profilen gleich)
        if (!ActiveProfile.speedJumpBonus)
            return pixelBaseJumpHeight;

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


