using UnityEngine;
using Unity.Cinemachine;
using System.Runtime.ExceptionServices;
using System;
using System.Runtime.CompilerServices;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] public GameObject standardXPosition;
    [SerializeField] public GameObject standardYPosition;

    [SerializeField] public GameObject runningRightXPosition;
    [SerializeField] public GameObject runningLeftXPosition;

    [SerializeField] public GameObject upYPosition;
    [SerializeField] public GameObject downYPosition;

    [SerializeField] private Player player;



    private float currentOffsetX;

    private float standardOffsetX;
    private float rightOffsetX;
    private float leftOffsetX;

    private float standardOffsetY;
    private float upOffsetY;
    private float downOffsetY;

    private float targetX;
    private float targetY;

    private PlayerState currentState;

    private PlayerState previousState;

    void Awake()
    {
        standardOffsetX = transform.localPosition.x;
        rightOffsetX = runningRightXPosition.transform.localPosition.x;
        leftOffsetX = runningLeftXPosition.transform.localPosition.x;

        standardOffsetY = standardYPosition.transform.localPosition.y;
        upOffsetY = upYPosition.transform.localPosition.y;
        downOffsetY = downYPosition.transform.localPosition.y;

        currentState = player.currentState;
        previousState = currentState;
    }
    void Start()
    {
        


    }


    // Update is called once per frame
    void Update()
    {
        
    }


    private void LateUpdate()
    {
        CalculateCameraPosition();
       
    }




    private void CalculateCameraPosition()
    {
        HandleStateChange();
        UpdateTargetX();
        UpdateTargetY();







    }




    private void HandleStateChange()
    {
        currentState = player.currentState;
        if (currentState != previousState)
        {
            previousState = currentState;


        }


    }


    private void UpdateTargetX()
    {
        
        bool isNormalGroundedOrAir = currentState == player.groundState
                                    || currentState == player.jumpState
                                    || currentState == player.fallState;

        if (isNormalGroundedOrAir)
            targetX = player.isFacingRight ? rightOffsetX : leftOffsetX;
        else if (currentState == player.wallSlideState || currentState == player.wallSlideFallState)
            targetX = 0f; 
        else if (currentState == player.wallJumpState)
            targetX = player.isFacingRight ? rightOffsetX : leftOffsetX;
    }


    private void UpdateTargetY()
    {
        

        bool isFallingOrWallFalling = currentState == player.fallState || currentState == player.wallSlideFallState;
        if (isFallingOrWallFalling)
        {
            targetY = downOffsetY;
        }
        else if(currentState == player.groundState)
        {
            targetY = standardOffsetY;
        }

    }


}
