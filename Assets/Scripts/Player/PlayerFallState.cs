using UnityEngine;

public class PlayerFallState : PlayerAirState
{
    override public void Do()
    {
        base.Do();
    }

    override public void Enter()
    {
        base.Enter();
        
        //anim.Play("Fall"); 
    }

    override public void Exit()
    {
       // base.Exit();
    }

    override public void FixedDo()
    {
        base.FixedDo();
        if (player.isHoldingJump)
        {
            //0.7f is a multiplier to reduce gravity. Should be adjusted as a variable in the future.
            player.currentGravity = Mathf.Lerp(player.currentGravity, player.jumpGravityUnit * 0.7f, 0.9f);
            rb.linearVelocity -= Vector2.up * player.currentGravity * Time.fixedDeltaTime;
            //Debug.Log("Falling with jump held, currentGravity: " + player.currentGravity);
        }
        else if(!player.isHoldingJump) 
        {
            player.currentGravity = Mathf.Lerp(player.currentGravity, Gravity, 0.9f);
            rb.linearVelocity -= Vector2.up * player.currentGravity * Time.fixedDeltaTime;
            //Debug.Log("Falling without jump held, currentGravity: " + player.currentGravity);
        }
        if (player.coyoteTimer.IsActive && player.jumpBuffer.IsActive)
        {
            player.SwitchState(player.fallState);
            player.coyoteTimer.Consume();
        }
        if (player.CheckWallL() || player.CheckWallR())
            player.SwitchState(player.wallSlideFallState);
    }

    override public void Initialize(Player playerRef)
    {
        base.Initialize(playerRef);
    }

    public override float Gravity => player.fallGravityUnit;

   




}
