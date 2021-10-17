using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class PlayerMotor : MonoBehaviour {

    private const float LANE_DISTANCE = 2.0f;
    private const float TURN_SPEED = 0.5f;

	public bool isRunning = false;

    // Animation
    private Animator anim;

    // Movement
    private CharacterController contorller;
    private float jumpForce = 4.0f;
    private float gravity = 12.0f;
    private float verticalVelocity;
    private int desiredLane = 1; // 0 = Left, 1 = Middle, 2 = Right

	// Speed Modifier
	private float originalSpeed = 7.0f;
	private float speed;
	private float speedIncreaseLastTick;
	private float speedIncreaseTime = 2.5f;
	private float speedIncreaseAmount = 0.1f;


    private void Start()
    {
        contorller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
		speed = originalSpeed;
         
    }

    private void Update()
    {
		if (!isRunning)
			return;

		// speed modifier
		if ((Time.time - speedIncreaseLastTick) > speedIncreaseTime) {
			speedIncreaseLastTick = Time.time;
			speed += speedIncreaseAmount;
			GameManager.Instance.UpdateModifier (speed - originalSpeed);
		}

        // Gather input on which lane we should be
		if(MobileInput.Instance.SwipeLeft)
        {
            MoveLane(false);
        }
		if (MobileInput.Instance.SwipeRight)
        {
            MoveLane(true);
        }

        // Calculate where should we be
        Vector3 targetPosition = transform.position.z * Vector3.forward;
        switch (desiredLane)
        {
            case 0:
                targetPosition += Vector3.left * LANE_DISTANCE;
                break;  
            case 2:
                targetPosition += Vector3.right * LANE_DISTANCE;
                break;
        }

        // Calculate our move delta
        Vector3 moveVector = Vector3.zero;
        moveVector.x = (targetPosition - transform.position).normalized.x * speed;

        bool isGrounded = IsGrounded();
        anim.SetBool("Grounded", isGrounded);

        // Calculate Y
        if (IsGrounded()) // is grounded
        {
            verticalVelocity = -0.1f;

			if (MobileInput.Instance.SwipeUp) {
				verticalVelocity = jumpForce;
				anim.SetTrigger ("Jump");
			} else if (MobileInput.Instance.SwipeDown) {
				StartSliding ();
			}
        }
        else
        {
            verticalVelocity -= (gravity * Time.deltaTime);

			if (MobileInput.Instance.SwipeDown)
            {
                verticalVelocity = -jumpForce;
            }
        }

        moveVector.y = verticalVelocity;
        moveVector.z = speed;

        // Move the pengu
        contorller.Move(moveVector * Time.deltaTime);

        // Rotate the pengu to where he is going
        Vector3 dir = contorller.velocity;

        if (dir != Vector3.zero)
        {
            dir.y = 0f;
            transform.forward = Vector3.Lerp(transform.forward, dir, TURN_SPEED);
        }
    }

    private void MoveLane(bool goingRight)
    {
        desiredLane += (goingRight) ? 1 : -1;
        desiredLane = Mathf.Clamp(desiredLane, 0, 2);
    }

	private void StartSliding ()
	{
		anim.SetBool ("Sliding", true);
		contorller.height *= 0.5f;
		contorller.center *= 0.5f;
		Invoke ("StopSliding", 1f);
	}

	private void StopSliding ()
	{
		contorller.height *= 2f;
		contorller.center *= 2f;
		anim.SetBool ("Sliding", false);
	}

    private bool IsGrounded()
    {
        Ray groundRay = new Ray(
            new Vector3(
                contorller.bounds.center.x,
                contorller.bounds.center.y - contorller.bounds.extents.y + 0.2f,
                contorller.bounds.center.z),
            Vector3.down);
        Debug.DrawRay(groundRay.origin, groundRay.direction, Color.cyan, 1f);

        return Physics.Raycast(groundRay, 0.3f);
    }

	public void StartRunning()
	{
		isRunning = true;
		anim.SetTrigger ("StartRunnig");
	}

	private void Crash()
	{
		anim.SetTrigger ("Death");
		isRunning = false;
        GameManager.Instance.OnDeath();
       // Debug.Log("Dead");
    
        }

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		switch (hit.gameObject.tag) {
		case "Obstacle":
			Crash ();
			break;
		}
	}
}

