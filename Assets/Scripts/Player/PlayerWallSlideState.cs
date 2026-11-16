using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWallSlideState : PlayerAirState
{
    private BufferTimer stickTimer;
    private bool wasSticking;

    private float wallSite;

    float wallCheck;
    public override void Enter()
    {
        // Debug.Log("bin drin");
        
        player.turningLocked = true;
        wasSticking = false;
        stickTimer.Consume();
        player.anim.Play("WallSlide");
        if (player.CheckWallSite() == 1)
            player.Turn(turnRight: true);
        else if (player.CheckWallSite() == -1)
            player.Turn(turnRight: false);

        player.playerInput.ingame.Jump.performed += OnJumpPressed;

        base.Enter();


        // Hochrutschen: Die Höhe kommt aus dem Profil (in Pixeln). Früher kam sie aus currentSpeed * Faktor. Seit currentSpeed an der
        // Wand auf 0 gesetzt wird (HandleHorizontalMovement), war das nur noch ein winziger Rest bei gedrückter Richtungstaste
        // und der Mindestwert nur ohne Taste. Jetzt zählt das Tempo vor dem Wandkontakt (wallStoppedSpeed), nicht die Taste:
        // Stillstand = wallRunUpHeight, Tempo wallRunUpFullSpeed (0 = Höchsttempo) = wallRunUpHeightAtFullSpeed (0 = nicht tempoabhängig).
        // wallRunUpHeight 0 und kein Tempo-Wert: kein Hochrutschen, die Figur haftet sofort und fällt danach wie gewohnt ab.
        MovementProfile profile = player.ActiveProfile;
        float heightPx = profile.wallRunUpHeight;
        if (profile.wallRunUpHeightAtFullSpeed > 0f)
        {
            float approachSpeed = Mathf.Abs(player.currentSpeed);
            if (Time.time - player.wallStoppedTime < 0.25f)
                approachSpeed = Mathf.Abs(player.wallStoppedSpeed);
            float fullSpeed = profile.wallRunUpFullSpeed > 0f
                ? player.PixelsPerSecondToUnits(profile.wallRunUpFullSpeed)
                : player.runMaxSpeedUnit;
            float t = Mathf.Clamp01(approachSpeed / Mathf.Max(0.01f, fullSpeed));
            // quadratisch: normales Laufen bleibt niedrig, erst hohes Tempo (Spin-Dash) rutscht weit
            heightPx = Mathf.Lerp(profile.wallRunUpHeight, profile.wallRunUpHeightAtFullSpeed, t * t);
        }
        float runUpHeight = player.PixelsToUnits(heightPx);

        player.currentGravity = player.jumpGravityUnit * player.wallSlideGravityMod;

        if (runUpHeight > 0f)
        {
            player.jumpVelocity = player.CalculateJumpVelocity(
                runUpHeight,
                player.pixelApexFrames,
                Player.PhysicsFPS
            );
        }
        else
        {
            player.jumpVelocity = 0f;
        }

        player.rb.linearVelocity = new Vector2(
           0,
           player.jumpVelocity);


       

    }
    public override void Exit()
    {
        base.Exit();
        
        player.playerInput.ingame.Jump.performed -= OnJumpPressed;
    }
    override public void FixedDo()
    {
        bool nearApex = Mathf.Abs(player.rb.linearVelocity.y) < 5;
        wallSite = player.CheckWallSite();

        if (nearApex && !stickTimer.IsActive && !wasSticking)
        {
            stickTimer.Start(player.stickDuration);
        }

        if (stickTimer.IsActive)
        {
            stickTimer.Tick(Time.fixedDeltaTime);
            player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, 0f);
            wasSticking = true;
            return;
        }

        if (wasSticking)
        {
            float bounceX = 10f * wallSite;
            player.rb.linearVelocity = new Vector2(bounceX, -2f);
            player.currentSpeed = bounceX;
            wasSticking = false;
            player.turningLocked = false;
            player.inputLockedUntil = Time.time + player.postBounceInputLockDuration; // expliziter Zeitstempel, unabhängig vom State
            player.SwitchState(player.fallState); // ruft PlayerFallState.Enter() → PlayerAirState.Enter() → setzt zunächst Standardwert
            player.currentTurnDeceleration = player.wallJumpTurnDecelerationUnit; // danach gezielt überschreiben, wie beim Wall-Jump
            return;
        }





        player.currentGravity = Mathf.Lerp(player.currentGravity, Gravity, 0.1f);
        player.rb.linearVelocity -= Vector2.up * player.currentGravity * Time.fixedDeltaTime;
    }
    public override void Do()
    {
        base.Do();
        if (player.currentState != this) return;
        if (player.CheckWallSite() == 0 && player.rb.linearVelocity.y > 0)
        {
            player.turningLocked = false;
            player.SwitchState(player.jumpState);
            player.currentSpeed = 0;
            player.currentGravity = player.jumpGravityUnit;

        }

    }

    private void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        wallCheck = player.CheckWallSite();

        if (wallCheck != 0)
        {
            player.turningLocked = false;
            if (player.simpleWallJump)
            {
                // Einfacher Wandsprung (Taste V): nur wenn Wand und Profil ihn erlauben
                if (player.SimpleWallJumpAvailable)
                    player.StartSimpleWallJump();
            }
            else
                player.SwitchState(player.wallJumpState);
        }
    }

    override public float Gravity => player.wallSlideGravityUnit;
    //override public float Acceleration => player.wallSlideAccelerationUnit;
    //override public float Deceleration => player.wallSlideDecelerationUnit;
    //override public float TurnDeceleration => player.wallSlideTurnDecelerationUnit;

}