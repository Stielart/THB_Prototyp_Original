using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWallSlideFallState : PlayerAirState
{
    public override void Enter()
    {
        base.Enter();
        player.playerInput.ingame.Jump.performed += OnJumpPressed;
        // kein Schub, keine Sticky-Logik – Charakter fällt einfach mit reduzierter Gravity weiter
        player.currentGravity = player.jumpGravityUnit * player.wallSlideGravityMod;
    }

    public override void Exit()
    {
        base.Exit();
        player.playerInput.ingame.Jump.performed -= OnJumpPressed;
    }

    public override void FixedDo()
    {
        player.currentGravity = Mathf.Lerp(player.currentGravity, Gravity, 0.1f);
        Debug.Log($"currentGravity: {player.currentGravity}, velocity.y: {player.rb.linearVelocity.y}, Gravity target: {Gravity}");
        player.rb.linearVelocity -= Vector2.up * player.currentGravity * Time.fixedDeltaTime;
  
    }

    public override void Do()
    {
        base.Do();
        // Check for wall contact to know when to slide/fall (no more wall contact)
    }

    private void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        if (player.CheckWallSite() != 0)
        {
            float boostX = player.wallJumpSpeedXUnit * player.CheckWallSite();
            player.currentSpeed = boostX;
            player.rb.linearVelocity = new Vector2(boostX, player.rb.linearVelocity.y);
            player.SwitchState(player.fallState);
        }
    }

    override public float Gravity => player.wallSlideGravityUnit;
}