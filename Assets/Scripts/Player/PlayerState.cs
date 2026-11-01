using UnityEngine;

public abstract class PlayerState : MonoBehaviour
{

    public bool isComplete { get; protected set; }
    float startTime;
    public float time => Time.time - startTime;

    public float currentGravity;



    protected Player player;  
    
    
    protected Rigidbody2D rb;
    protected Animator anim;
    protected BoxCollider2D bc;

   
    public virtual void Initialize(Player playerRef)
    {
        this.player = playerRef;
        this.rb = playerRef.GetComponent<Rigidbody2D>();
        this.anim = playerRef.GetComponent<Animator>();
        this.bc = playerRef.GetComponent<BoxCollider2D>();
        
    }

    public virtual void Enter()
    {
        startTime = Time.time;
    }

    public virtual void Exit()
    {



    }

    public virtual void FixedDo() { }

    public virtual void Do() { }


    protected void HandleHorizontalMovement(float acceleration, float deceleration, float turnDeceleration, bool ignoreInput = false, bool allowActiveBrake = true)
    {
        float effectiveInput = ignoreInput ? 0f : player.xInput;

        // Wand in Bewegungsrichtung: currentSpeed auf 0 setzen. currentSpeed ist ein gespeicherter Wert, die Physik
        // blockiert die Figur an der Wand, baut den Wert aber nicht ab. Ohne diese Zeilen "schiebt" das gespeicherte Tempo
        // beim Sprung über die Kante plötzlich nach vorn und der Sprung wird höher (Geschwindigkeitsbonus).
        // Die ursprüngliche Geschwindigkeit wird kurz gemerkt, damit die Ledge Correction sie beibehalten kann.
        if ((player.currentSpeed > 0f && player.CheckWallR()) || (player.currentSpeed < 0f && player.CheckWallL()))
        {
            if (Time.time - player.wallStoppedTime > 0.1f)
            {
                player.wallStoppedSpeed = player.currentSpeed;
                player.wallStoppedTime = Time.time;
            }
            player.currentSpeed = 0f;
        }

        float targetSpeed = 0f;
        float rateToUse;

        float maxSpeed = player.isRunning ? player.runMaxSpeedUnit : player.walkMaxSpeedUnit;

        // set target speed based on input
        if (Mathf.Abs(effectiveInput) > 0.7f)
            targetSpeed = effectiveInput * maxSpeed;
        else
            targetSpeed = 0f;

        // Choose the appropriate rate based on input and current speed
        if (effectiveInput == 0)
        {
            // no input → decelerate. Depends highly on parameter, so we use the parameter instead of a hardcoded value.
            rateToUse = deceleration;
            player.isTurningAround = false;
        }
        else if (Mathf.Sign(effectiveInput) == Mathf.Sign(player.currentSpeed) || Mathf.Abs(player.currentSpeed) < 0.2f)
        {


            if(maxSpeed < Mathf.Abs(player.currentSpeed))
            {
                rateToUse = deceleration;
                player.isTurningAround = false;
            }
            else
            {
                // Same direction or almost stationary → accelerate
                rateToUse = acceleration;
                player.isTurningAround = false;
            }
        }
        else if (allowActiveBrake)
        {
            // Opposite direction → decelerate quickly
            player.isTurningAround = true;
            rateToUse = turnDeceleration;
        }
        else
        {
            // Aktives Bremsen aus: Gegenrichtung wirkt wie "Taste loslassen" (nur passives Auslaufen).
            // Erst bei Stillstand greift der Zweig oben (Beschleunigung in die neue Richtung).
            targetSpeed = 0f;
            rateToUse = deceleration;
            player.isTurningAround = false;
        }

        // Step towards the target speed
        player.currentSpeed = Mathf.MoveTowards(player.currentSpeed, targetSpeed, rateToUse * Time.fixedDeltaTime);

        // Apply the velocity
        rb.linearVelocity = new Vector2(player.currentSpeed, rb.linearVelocity.y);
    }


    public virtual float Gravity => player.currentGravity;
    public virtual float Acceleration => player.currentAcceleration;

    public virtual float Deceleration => player.currentDeceleration;

    public virtual float TurnDeceleration => player.currentTurnDeceleration;

    public virtual string StateName => this.GetType().Name;







}
