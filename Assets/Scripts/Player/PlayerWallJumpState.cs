using UnityEngine;

public class PlayerWallJumpState : PlayerAirState
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    float curveValue;
    float elapsedRatio;
    private float endGravity;

    private float firstWallSite;

    private float lastWallSite;

    override public void Enter()
    {
        base.Enter();


        

        endGravity = player.currentGravity;
        player.currentGravity = Gravity;
        Debug.Log("WallJumpState Enter, currentGravity: " + player.currentGravity);
        Debug.Log("wallJumpSpeedXUnit: " + player.wallJumpSpeedXUnit);
        firstWallSite = player.CheckWallSite();
        player.rb.linearVelocity = new Vector2(player.wallJumpSpeedXUnit * firstWallSite, player.wallJumpSpeedYUnit);
        player.isGrounded = false;


        player.currentTurnDeceleration = TurnDeceleration;
        player.currentAcceleration = Acceleration;
        player.currentDeceleration = Deceleration;
        player.currentSpeed = player.wallJumpSpeedXUnit * firstWallSite;
    }

    override public void Do()
    {
        base.Do();
        
      
    }

    public override void Exit() { }

    override public void FixedDo()
    {
        lastWallSite = player.CheckWallSite();

        if(lastWallSite * -1 == firstWallSite)
        {
            player.SwitchState(player.wallSlideState);
            return;
        }




        elapsedRatio = Mathf.Clamp01(time / 2f);
        curveValue = player.wallJumpGravityCurve.Evaluate(elapsedRatio);
        float gravityToUse = player.isHoldingJump ? Gravity * player.wallJumpHoldGravityMod : Gravity;
        player.currentAcceleration = Mathf.Lerp(player.wallJumpAccelerationUnit, player.airAccelerationUnit, curveValue);


        HandleHorizontalMovement(player.currentAcceleration, player.currentDeceleration, player.currentTurnDeceleration);
        //HandleHorizontalMovement(player.airAccelerationUnit, player.airDecelerationUnit, player.airTurnDecelerationUnit);

        player.currentGravity = Mathf.Lerp(  gravityToUse, player.jumpGravityUnit, curveValue);
        Debug.Log("currentGravity: " + player.currentGravity);
        player.rb.linearVelocity -= Vector2.up * player.currentGravity * Time.fixedDeltaTime;
        player.currentDeceleration = Mathf.Lerp(player.currentDeceleration, player.airDecelerationUnit, 0.1f);
    }



    override public void Initialize(Player playerRef)
    {
        base.Initialize(playerRef);
    }

    override public float Gravity => player.wallJumpGravityUnit;
    override public float Acceleration => player.wallJumpAccelerationUnit;
    override public float Deceleration => player.wallJumpDecelerationUnit;
    override public float TurnDeceleration => player.wallJumpTurnDecelerationUnit;

}
