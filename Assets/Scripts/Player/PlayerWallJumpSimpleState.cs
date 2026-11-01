using UnityEngine;

/// <summary>
/// Einfacher Wandsprung (Taste V schaltet zwischen diesem und dem alten, kurvenbasierten Wandsprung um).
/// Orientiert an Celeste und New Super Mario Bros.: Beim Absprung wird die Geschwindigkeit fest gesetzt, die Richtung ergibt sich aus
/// dem Winkel im Profil (0 Grad = senkrecht an der Wand hoch, 90 Grad = waagerecht von der Wand weg), die Länge aus
/// der Absprunggeschwindigkeit im Profil (Spielpixel pro Sekunde, wie alle Profilwerte auf die Figur umgerechnet).
/// Danach gibt es für eine einstellbare Zahl von Frames keine Steuerung: Die waagerechte Geschwindigkeit bleibt konstant,
/// damit man nicht sofort wieder zur selben Wand zurückläuft oder an ihr erneut abspringt. Erst nach der Sperre lenkt die
/// normale Luftsteuerung. Die Sprunghöhe lässt sich wie beim normalen Sprung durch Loslassen der Taste verkürzen.
/// </summary>
public class PlayerWallJumpSimpleState : PlayerAirState
{
    private int lockFramesLeft;
    private float lockedSpeedX;
    private float takeoffSpeedY;

    public override void Enter()
    {
        base.Enter();
        MovementProfile profile = player.ActiveProfile;

        // CheckWallSite liefert die Richtung von der Wand weg (+1 = Wand links, -1 = Wand rechts)
        float away = player.CheckWallSite();
        if (away == 0f)
            away = player.isFacingRight ? -1f : 1f;

        float speed = player.PixelsPerSecondToUnits(profile.wallJumpSpeed * player.profileScale);
        float angle = Mathf.Clamp(profile.wallJumpAngle, 0f, 90f) * Mathf.Deg2Rad;
        lockedSpeedX = away * speed * Mathf.Sin(angle);
        takeoffSpeedY = speed * Mathf.Cos(angle);

        player.rb.linearVelocity = new Vector2(lockedSpeedX, takeoffSpeedY);
        player.currentSpeed = lockedSpeedX;
        player.isGrounded = false;
        player.isTurningAround = false;
        player.turningLocked = false;
        player.currentGravity = player.jumpGravityUnit;
        lockFramesLeft = Mathf.Max(0, profile.wallJumpLockFrames);

        // Die Figur schaut von der Wand weg
        player.Turn(away > 0f);
        player.anim.Play("WallJump");
    }

    public override void FixedDo()
    {
        // Nach der Sperre ist an einer anderen Wand direkt der nächste Wandsprung möglich
        if (lockFramesLeft <= 0 && player.TryWallJump())
            return;

        if (lockFramesLeft > 0)
        {
            lockFramesLeft--;
            player.currentSpeed = lockedSpeedX;
            rb.linearVelocity = new Vector2(lockedSpeedX, rb.linearVelocity.y);
        }
        else
        {
            // normale Luftsteuerung
            base.FixedDo();
        }

        // Gravitation und Sprungabbruch wie beim normalen Sprung
        rb.linearVelocity -= Vector2.up * player.jumpGravityUnit * Time.fixedDeltaTime;

        MovementProfile profile = player.ActiveProfile;
        if (profile.jumpCut && !player.isHoldingJump && rb.linearVelocity.y > 0f)
            rb.linearVelocityY = Mathf.Min(rb.linearVelocity.y, takeoffSpeedY * profile.jumpCutSpeedFraction);

        if (rb.linearVelocity.y <= 0f)
            player.SwitchState(player.fallState);
    }

    public override float Gravity => player.jumpGravityUnit;
}
