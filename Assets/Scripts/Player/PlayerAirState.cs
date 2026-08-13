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
        player.currentTurnDeceleration = player.airTurnDecelerationUnit; // Standardwert, überschreibbar von spezielleren States



    }

    public override void Exit()
    {
        base.Exit();
        
        
    }

    public override void FixedDo()
    {
        base.FixedDo();
        bool ignoreInput = Time.time < player.inputLockedUntil;
        HandleHorizontalMovement(player.airAccelerationUnit, player.airDecelerationUnit, player.currentTurnDeceleration, ignoreInput);

    }

    public override void Initialize(Player playerRef)
    {
        base.Initialize(playerRef);
    }

    protected float LerpTowardsHoldTarget(float currentValue, float heldTarget, float releasedTarget, float lerpSpeed)
    {
        float target = player.isHoldingJump ? heldTarget : releasedTarget;
        return Mathf.Lerp(currentValue, target, lerpSpeed);
    }

    protected void ClampFallSpeed(float maxFallSpeed)
    {
        float clampedY = Mathf.Max(player.rb.linearVelocity.y, -maxFallSpeed);
        player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, clampedY);
    }

    public virtual float MaxFallSpeed => player.fallMaxSpeedUnit;
    public virtual float HoldMaxFallSpeed => player.fallHoldJumpMaxSpeedUnit;


}
