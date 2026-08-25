using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.ShaderData;

public class PlayerWallSlideState : PlayerAirState
{
    private BufferTimer stickTimer;
    private bool wasSticking;

    private float wallSite;

    float wallCheck;
    public override void Enter()
    {
        Debug.Log("bin drin");
        
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


        if (player.currentSpeed < 0)
            player.currentSpeed *= -1;
        float wallSlideSpeed = player.currentSpeed * player.wallSlideSpeedModifier;
        if(wallSlideSpeed == 0)
            wallSlideSpeed = player.wallSlideMinSpeedUnit;


        player.currentGravity = player.jumpGravityUnit * player.wallSlideGravityMod;
        Debug.Log($"WallSlideState Enter, wallSlideSpeed: {wallSlideSpeed}, currentGravity: {player.currentGravity}");

        player.jumpVelocity = player.CalculateJumpVelocity(
         wallSlideSpeed,
         player.pixelApexFrames,
         Player.PhysicsFPS
     );



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
            player.SwitchState(player.wallJumpState);
        }
    }

    override public float Gravity => player.wallSlideGravityUnit;
    //override public float Acceleration => player.wallSlideAccelerationUnit;
    //override public float Deceleration => player.wallSlideDecelerationUnit;
    //override public float TurnDeceleration => player.wallSlideTurnDecelerationUnit;

}