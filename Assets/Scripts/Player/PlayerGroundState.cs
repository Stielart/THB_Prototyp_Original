using UnityEngine;

public class PlayerGroundState : PlayerState
{
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
        
        player.isGrounded = player.CheckGround();

       
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




   


}
