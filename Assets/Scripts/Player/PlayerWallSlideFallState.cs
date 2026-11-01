using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWallSlideFallState : PlayerAirState
{
    public override void Enter()
    {
        base.Enter();
        player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, -Mathf.Abs(player.currentSpeed));
        player.turningLocked = true;
        player.anim.Play("WallSlide");
        if (player.CheckWallSite() == 1)
            player.Turn(turnRight: true);
        else if (player.CheckWallSite() == -1)
            player.Turn(turnRight: false);
        player.playerInput.ingame.Jump.performed += OnJumpPressed;
        // kein Schub, keine Sticky-Logik – Charakter fällt einfach mit reduzierter Gravity weiter
        player.currentGravity = player.jumpGravityUnit * player.wallSlideGravityMod;
    }

    public override void Exit()
    {
        base.Exit();
        
        player.turningLocked = false;
        player.inputLockedUntil = Time.time + player.postBounceInputLockDuration;
        player.playerInput.ingame.Jump.performed -= OnJumpPressed;
    }

    public override void FixedDo()
    {
       // player.currentGravity = Mathf.Lerp(player.currentGravity, Gravity, 0.5f);
       float currentFallSpeed = LerpTowardsHoldTarget(player.rb.linearVelocity.y, -MaxFallSpeed, -MaxFallSpeed, 0.05f);
        // Debug.Log($"currentGravity: {player.currentGravity}, velocity.y: {player.rb.linearVelocity.y}, Gravity target: {Gravity}");
        //player.rb.linearVelocity -= Vector2.up * player.currentGravity *  Time.fixedDeltaTime;
        player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, currentFallSpeed);
        //if (player.rb.linearVelocity.y < MaxFallSpeed)
        //ClampFallSpeed(MaxFallSpeed);


    }

    public override void Do()
    {
        base.Do();
       

        if (player.CheckWallSite() == 0 )
        {
            player.currentSpeed = 0;
            if(player.isGrounded)
                player.SwitchState(player.groundState);
            else
                player.SwitchState(player.fallState);
            player.currentGravity = player.jumpGravityUnit;

        }
        


        // Check for wall contact to know when to slide/fall (no more wall contact)
    }

    private void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        if (player.simpleWallJump)
        {
            // Einfacher Wandsprung (Taste V)
            if (player.CheckWallSite() != 0 && player.SimpleWallJumpAvailable)
                player.StartSimpleWallJump();
            return;
        }

        if (player.CheckWallSite() != 0)
        {
            float boostX = player.wallJumpSpeedXUnit * player.CheckWallSite();
            player.currentSpeed = boostX;
            player.rb.linearVelocity = new Vector2(boostX, player.rb.linearVelocity.y);
            player.SwitchState(player.fallState);
        }
    }

    override public float Gravity => player.wallSlideFallGravityUnit;
    public override float MaxFallSpeed => player.fallWallSlideMaxSpeedUnit;
    //public override float HoldMaxFallSpeed => player.fallWallSlideHoldJumpMaxSpeedUnit;
}