using UnityEngine;
using UnityEngine.Rendering;

public class PlayerJumpState : PlayerAirState
{
    
    public override void Do()
    {
        //base.Do();
        
    }

    public override void Enter()
    {
        player.anim.Play("Jump"); // Wenn du später Animationen hast
        Debug.Log("JumpState Enter");

        player.currentGravity = player.jumpGravityUnit;

        float heightBasedOnSpeed = player.CalculateSpeedBasedJumpHeight(player.currentSpeed, player.runMaxSpeedUnit);

        player.jumpVelocity = player.CalculateJumpVelocity(
            player.PixelsToUnits(heightBasedOnSpeed),
            player.pixelApexFrames,
            Player.PhysicsFPS
        );

        player.rb.linearVelocity = new Vector2(
            player.rb.linearVelocity.x,
            player.jumpVelocity);

        player.isGrounded = false;

        Debug.Log($"Speed: {player.currentSpeed:F2}, Height: {heightBasedOnSpeed:F1}px");
    }





        //    player.jumpVelocity =
        //  player.PixelsToUnits(
        //      player.CalculateJumpVelocity(
        //          player.pixelMaxHeight,
        //          player.pixelApexFrames,
        //            Player.PhysicsFPS

        //      )
        //  );

        //    player.rb.linearVelocity = new Vector2(
        //player.rb.linearVelocity.x,
        //player.jumpVelocity);

        //    Debug.Log(player.jumpVelocity);
        //    Debug.Log(Gravity);
        //    Debug.Log(player.rb.linearVelocity);

        //anim.Play("Jump"); // Wenn du später Animationen hast


    

    public override void Exit()
    {
        base.Exit();


        
    }

    public override void FixedDo()
    {
        base.FixedDo();
        bool isAtApex = Mathf.Abs(player.rb.linearVelocity.y) < player.apexHangRange
                && time > 0.15f
                && player.isHoldingJump;  // Nur wenn Button gehalten!


 
        float gravityToUse;
        if (isAtApex)
        {
            Debug.Log($"APEX! Velocity.y: {player.rb.linearVelocity.y:F2}");
            gravityToUse = player.jumpGravityUnit * player.apexHangMultiplier;
            player.anim.Play("JumpEnd");
        }
        else
        {

            gravityToUse = player.jumpGravityUnit;
        }

        player.currentGravity = Mathf.Lerp(player.currentGravity, gravityToUse, 0.2f);
        player.rb.linearVelocity -= Vector2.up * player.currentGravity * Time.fixedDeltaTime;

        if (!player.isHoldingJump && player.rb.linearVelocity.y > 0)
        {
            //player.rb.linearVelocity = new Vector2(
            //    player.rb.linearVelocity.x,
            //    player.rb.linearVelocity.y * player.jumpCutMultiplier


            // );

            player.rb.linearVelocityY = Mathf.Min(player.rb.linearVelocity.y, 10f);
            player.anim.Play("JumpEnd");



        }

        //player.rb.linearVelocity += Vector2.down *
        //player.jumpGravityUnit *
        //Time.fixedDeltaTime;

        if (player.rb.linearVelocity.y <= 0)
            player.SwitchState(player.fallState);
        

        else if(player.CheckWallL() || player.CheckWallR())
            player.SwitchState(player.wallSlideState);
       
       


    }

    public override void Initialize(Player playerRef)
    {
        base.Initialize(playerRef);



    }



    

    public override float Gravity => player.jumpGravityUnit;

}
