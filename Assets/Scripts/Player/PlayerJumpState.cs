using UnityEngine;
using UnityEngine.Rendering;

public class PlayerJumpState : PlayerAirState
{
    // Für den Zwei-Stufen-Sprung (Profil Momentum)
    private float takeoffY;
    private int framesSinceTakeoff;
    private bool shortJumpApplied;


    public override void Do()
    {
        //base.Do();
        
    }

    public override void Enter()
    {
        player.anim.Play("Jump"); 
        // Debug.Log("JumpState Enter");

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

        takeoffY = player.rb.position.y;
        framesSinceTakeoff = 0;
        shortJumpApplied = false;

        // Debug.Log($"Speed: {player.currentSpeed:F2}, Height: {heightBasedOnSpeed:F1}px");
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
        if (player.cornerCorrectionEnabled)
            ResolveCornerCollision();
        player.TryLedgeCorrection();
        bool isAtApex = Mathf.Abs(player.rb.linearVelocity.y) < player.apexHangRange
                && time > 0.15f
                && player.isHoldingJump;  // Nur wenn Button gehalten!

        
        
 
        float gravityToUse;
        if (isAtApex)
        {
            
            gravityToUse = player.jumpGravityUnit * player.apexHangMultiplier;
            player.anim.Play("JumpEnd");
        }
        else
        {

            gravityToUse = player.jumpGravityUnit;
        }

        player.currentGravity = Mathf.Lerp(player.currentGravity, gravityToUse, 0.2f);
        player.rb.linearVelocity -= Vector2.up * player.currentGravity * Time.fixedDeltaTime;

        framesSinceTakeoff++;
        MovementProfile profile = player.ActiveProfile;

        if (profile.twoStageJump)
        {
            // Zwei feste Höhen: Nur ein Loslassen innerhalb des Fensters zählt (kurzer Sprung), danach hat die Taste keinen Einfluss mehr.
            if (!shortJumpApplied && framesSinceTakeoff <= profile.twoStageWindowFrames
                && !player.isHoldingJump && player.rb.linearVelocity.y > 0)
            {
                // Aufstiegsgeschwindigkeit so setzen, dass die Figur genau bis zur Höhe des kurzen Sprungs steigt,
                // egal in welchem Frame des Fensters losgelassen wurde: v = Wurzel(2 * g * Resthöhe)
                float gained = player.rb.position.y - takeoffY;
                float shortHeight = player.PixelsToUnits(profile.shortJumpHeightFigures * player.figureHeightPx);
                float remaining = Mathf.Max(0f, shortHeight - gained);
                float shortSpeed = Mathf.Sqrt(2f * player.jumpGravityUnit * remaining);

                player.rb.linearVelocityY = Mathf.Min(player.rb.linearVelocity.y, shortSpeed);
                shortJumpApplied = true;
                player.anim.Play("JumpEnd");
            }
        }
        else if (profile.jumpCut && !player.isHoldingJump && player.rb.linearVelocity.y > 0)
          
        {
            // Debug.Log("JUMP CUT ausgelöst! isHoldingJump=" + player.isHoldingJump);
         
            player.rb.linearVelocityY = Mathf.Min(player.rb.linearVelocity.y, player.jumpVelocity * player.ActiveProfile.jumpCutSpeedFraction);
            player.anim.Play("JumpEnd");



        }

   
        if (player.rb.linearVelocity.y <= 0)
            player.SwitchState(player.fallState);


        else if (player.wallMechanicsEnabled && (player.CheckWallL() || player.CheckWallR()))
            player.SwitchState(player.wallSlideState);





    }

    public override void Initialize(Player playerRef)
    {
        base.Initialize(playerRef);



    }

    private void ResolveCornerCollision()
    {
        int maxIterations = 20; // Sicherheitsnetz gegen Endlosschleifen
        int iterations = 0;
        float pushStep = 0.05f; // kleine Schrittweite pro Iteration

        while (player.CheckCeiling() != 0 && iterations < maxIterations)
        {
            int direction = player.CheckCeiling();
            // direction = -1 bedeutet "links blockiert" -> nach RECHTS schieben
            // direction = 1 bedeutet "rechts blockiert" -> nach LINKS schieben
            player.transform.position += new Vector3(-direction * pushStep, 0, 0);
            iterations++;
        }
    }




    public override float Gravity => player.jumpGravityUnit;



}
