using UnityEngine;

public class PlayerAirState : PlayerState
{


    public override void Do()
    {
        player.isGrounded = player.CheckGround();
        if (player.isGrounded)
        {
            player.SwitchState(player.groundState);
        }
    }

    public override void Enter()
    {
        base.Enter();

      

    }

    public override void Exit()
    {
        base.Exit();
        
        
    }

    public override void FixedDo()
    {
        base.FixedDo();
        HandleHorizontalMovement(player.airAccelerationUnit, player.airDecelerationUnit, player.airTurnDecelerationUnit);
       
    }

    public override void Initialize(Player playerRef)
    {
        base.Initialize(playerRef);
    }


    
}
