using System;
using UnityEngine;

public class PlayerGroundState : PlayerState
{
    float absSpeed;
    float speedRatio;
    public override void Enter()
    {
 

        //anim.Play("Idle"); // Wenn du später Animationen hast
        player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, 0f);

    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void FixedDo()
    {
        HandleHorizontalMovement(player.walkAccelerationUnit, player.walkDecelerationUnit, player.turnDecelerationUnit);
    }

    public override void Do()
    {
        Debug.Log($"[Ground.Do] isGrounded={player.isGrounded}, buffer={player.jumpBuffer.IsActive}");
        player.isGrounded = player.CheckGround();
        HandleAnimationState();


        //if(player.isGrounded && player.)


        
        if (player.isGrounded && player.jumpBuffer.IsActive) {

            
            player.SwitchState(player.jumpState);
            player.jumpBuffer.Consume();
            return;


        }
       
        if (!player.isGrounded)
        {
            player.isGrounded = false;
            player.SwitchState(player.fallState);
            player.coyoteTimer.Start(player.coyoteTimeDuration);
        }


      
        
    }

    private void HandleAnimationState()
    {
        if (player.isTurningAround)
        {
            player.anim.Play("Turn");
            return;
        } 
        absSpeed = Mathf.Abs(player.currentSpeed);
        if (absSpeed < 0.1f)
            player.anim.Play("Idle");

        else if(absSpeed >= 0.1f && absSpeed <= player.walkMaxSpeedUnit)
        {
            speedRatio =  Mathf.InverseLerp(0f, player.walkMaxSpeedUnit, Mathf.Abs(absSpeed));
            player.anim.Play("Walk");

        }
        else if (absSpeed > player.walkMaxSpeedUnit && absSpeed <= player.runMaxSpeedUnit - 3f)
        {
            speedRatio = Mathf.InverseLerp(player.walkMaxSpeedUnit, player.runMaxSpeedUnit, Mathf.Abs(absSpeed));
            player.anim.Play("Run");
        }
        else if(absSpeed > player.runMaxSpeedUnit - 3f)
        {
            
            player.anim.Play("MaxSpeed");
        }

        player.isTurningAround = false;

    }


    public override string StateName => this.GetType().Name;
}
