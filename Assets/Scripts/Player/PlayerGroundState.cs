using System;
using UnityEngine;

public class PlayerGroundState : PlayerState
{
    float absSpeed;
    float speedRatio;

    // Spin Dash
    private bool spinCharging;
    private int spinPresses;

    public override void Enter()
    {
 

        //anim.Play("Idle"); // Wenn du später Animationen hast
        player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, 0f);
        spinCharging = false;
        spinPresses = 0;

    }

    public override void Exit()
    {
        base.Exit();
        if (spinCharging)
            CancelSpinDash();
    }

    public override void FixedDo()
    {
        if (spinCharging)
        {
            // Beim Aufladen steht die Figur still, Bewegungseingaben wirken nicht
            player.currentSpeed = 0f;
            player.rb.linearVelocity = new Vector2(0f, player.rb.linearVelocity.y);
            return;
        }

        HandleHorizontalMovement(player.walkAccelerationUnit, player.walkDecelerationUnit, player.turnDecelerationUnit, allowActiveBrake: player.activeBrakingEnabled);
    }

    public override void Do()
    {
        // Debug.Log($"[Ground.Do] isGrounded={player.isGrounded}, buffer={player.jumpBuffer.IsActive}");
        player.isGrounded = player.CheckGround();
        HandleAnimationState();

        if (HandleSpinDash())
            return;

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
            player.coyoteTimer.Start(player.CoyoteFrames);
        }


      
        
    }

    /// <summary>
    /// Spin Dash: Im Stand nach unten halten startet das Aufladen. Jeder Druck der Sprungtaste zählt als eine Ladung.
    /// Loslassen von unten schießt die Figur in Blickrichtung los (Geschwindigkeit über der Höchstgeschwindigkeit,
    /// die der normale Code über das passive Auslaufen langsam abbaut). Gibt true zurück, solange aufgeladen wird
    /// (dann wird in diesem Frame nicht gesprungen).
    /// </summary>
    private bool HandleSpinDash()
    {
        if (!player.SpinDashAvailable || !player.isGrounded)
        {
            if (spinCharging)
                CancelSpinDash();
            return false;
        }

        MovementProfile p = player.ActiveProfile;
        bool downHeld = player.yInput < (spinCharging ? -0.3f : -0.5f);

        if (!spinCharging)
        {
            // Start: nur im Stand (Geschwindigkeit unter 10 % der Höchstgeschwindigkeit) und mit Taste nach unten
            if (!downHeld || Mathf.Abs(player.currentSpeed) > 0.1f * player.runMaxSpeedUnit)
                return false;

            spinCharging = true;
            spinPresses = 0;
            player.currentSpeed = 0f;
            player.jumpBuffer.Consume();   // ein Druck vor dem Aufladen zählt nicht als Ladung
        }

        // Aufladen: jeder neue Druck der Sprungtaste
        if (player.jumpBuffer.IsActive)
        {
            spinPresses++;
            player.jumpBuffer.Consume();
        }

        if (!downHeld)
        {
            // Loslassen: mit mindestens einer Ladung abschießen, sonst nur abbrechen
            if (spinPresses > 0)
                LaunchSpinDash(p);
            CancelSpinDash();
            return false;
        }

        // Anzeige: Farbe wird mit der Ladung kräftiger, bei voller Ladung blinkt die Figur
        int full = Mathf.Max(1, p.spinDashPressesForFull);
        player.spinDashCharging = true;
        player.spinDashPresses = spinPresses;
        if (spinPresses >= full)
            player.SetSpriteTint(Mathf.Repeat(Time.time * 12f, 2f) < 1f ? Color.white : player.spinDashReadyColor);
        else
            player.SetSpriteTint(Color.Lerp(Color.white, player.spinDashChargeColor, (float)spinPresses / full));
        return true;
    }

    private void LaunchSpinDash(MovementProfile p)
    {
        int full = Mathf.Max(1, p.spinDashPressesForFull);
        float t = Mathf.Clamp01((float)spinPresses / full);
        float factor = Mathf.Lerp(p.spinDashMinSpeedFactor, p.spinDashMaxSpeedFactor, t);
        float direction = player.isFacingRight ? 1f : -1f;

        player.currentSpeed = direction * player.runMaxSpeedUnit * factor;
        player.rb.linearVelocity = new Vector2(player.currentSpeed, player.rb.linearVelocity.y);
    }

    private void CancelSpinDash()
    {
        spinCharging = false;
        spinPresses = 0;
        player.spinDashCharging = false;
        player.spinDashPresses = 0;
        player.SetSpriteTint(Color.white);
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
