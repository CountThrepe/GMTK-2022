using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody2D body;
    private PlayerGround ground;
    private PlayerJump jump;
    private PlayerJuice juice;
    private Animator animator;
    private PlayerTransform diceTransform;

    [Header("Movement Stats")]
    [SerializeField, Range(0f, 50f)][Tooltip("Dash speed")] public float dashDistance = 5f;
    [SerializeField, Range(0f, 2f)][Tooltip("Dash time")] public float dashTime = 0.75f;
    [SerializeField, Range(0f, 2f)][Tooltip("Dash slow multiplier")] public float dashEndSlow = 0.5f;
    [SerializeField, Range(0f, 20f)][Tooltip("Maximum movement speed")] public float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to reach max speed")] public float maxAcceleration = 52f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop after letting go")] public float maxDecceleration = 52f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop when changing direction")] public float maxTurnSpeed = 80f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to reach max speed when in mid-air")] public float maxAirAcceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop in mid-air when no direction is used")] public float maxAirDeceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop when changing direction when in mid-air")] public float maxAirTurnSpeed = 80f;
    [SerializeField][Tooltip("Friction to apply against movement on stick")] private float friction;

    [Header("Options")]
    [Tooltip("When false, the charcter will skip acceleration and deceleration and instantly move and stop")] public bool useAcceleration;

    [Header("Calculations")]
    private float directionX;
    private Vector2 desiredVelocity;
    private Vector2 velocity;
    private float maxSpeedChange;
    private float acceleration;
    private float deceleration;
    private float turnSpeed;
    private float dashStart;
    private float dashSpeed;

    [Header("Current State")]
    public bool onGround;
    public bool pressingKey;
    public bool frozen;


    private void Awake() {
        //Find the character's Rigidbody and ground detection script
        body = GetComponent<Rigidbody2D>();
        ground = GetComponent<PlayerGround>();
        jump = GetComponent<PlayerJump>();
        juice = GetComponent<PlayerJuice>();
        animator = GetComponentInChildren<Animator>();
        diceTransform = GetComponent<PlayerTransform>();
    }

    public void OnMove(InputValue value) {
        Vector2 movement = value.Get<Vector2>();
        directionX = movement.x;

        jump.SetJumpPressed(movement.y > 0);
    }

    // void OnShift() {
    //     Dash();
    // }

    // Update is called once per frame
    void Update() {
        if (directionX != 0) {
            transform.localScale = new Vector3(directionX > 0 ? 1 : -1, 1, 1);
            pressingKey = true;
        } else {
            pressingKey = false;
        }

        desiredVelocity = new Vector2(directionX, 0f) * Mathf.Max(maxSpeed - friction, 0f);

        
    }

    private void FixedUpdate() {
        // Perform movement calculations
        //Get Kit's current ground status from her ground script
        onGround = ground.GetOnGround();

        //Get the Rigidbody's current velocity
        velocity = body.velocity;

        if(!frozen) {
            if(dashStart > 0) {
                if(Time.time - dashStart > dashTime) { // If we're done dashing
                    dashStart = -1;
                    diceTransform.Personify();
                    velocity.x *= dashEndSlow;
                } else { // If we're still dashing
                    velocity.y = 0;
                    velocity.x = dashSpeed;
                }
                body.velocity = velocity;
            } else {
                //Calculate movement, depending on whether "Instant Movement" has been checked
                if (useAcceleration || !onGround)  runWithAcceleration();
                else runWithoutAcceleration();
            }
        }        

        // Update animator
        animator.SetBool("Grounded", onGround);
        animator.SetFloat("Speed", Mathf.Abs(body.velocity.x));
        animator.SetFloat("vSpeed", body.velocity.y);
    }

    void OnCheat() {
        Dash();
    }

    public void JumpFreeze(float length) {
        StartCoroutine(TempFreeze(length));
    }

    private IEnumerator TempFreeze(float length) {
        frozen = true;
        yield return new WaitForSeconds(length);
        frozen = false;
    }

    public void Dash() {
        if(frozen) return;

        diceTransform.Dieify();
        juice.DashEffects();
        dashStart = Time.time;
        dashSpeed = dashDistance / dashTime;
        if(directionX != 0) dashSpeed *= directionX;
        else dashSpeed *= transform.localScale.x;
    }

    public void CancelDash() {
        dashStart = -1;
        velocity = body.velocity;
        velocity.x = 0;
        body.velocity = velocity;
    }


    private void runWithAcceleration() {
        //Set our acceleration, deceleration, and turn speed stats, based on whether we're on the ground on in the air

        acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        deceleration = onGround ? maxDecceleration : maxAirDeceleration;
        turnSpeed = onGround ? maxTurnSpeed : maxAirTurnSpeed;

        if (pressingKey) {
            //If the sign (i.e. positive or negative) of our input direction doesn't match our movement, it means we're turning around and so should use the turn speed stat.
            if (Mathf.Sign(directionX) != Mathf.Sign(velocity.x)) maxSpeedChange = turnSpeed * Time.deltaTime;
            //If they match, it means we're simply running along and so should use the acceleration stat
            else maxSpeedChange = acceleration * Time.deltaTime;
        } else {
            //And if we're not pressing a direction at all, use the deceleration stat
            maxSpeedChange = deceleration * Time.deltaTime;
        }

        //Move our velocity towards the desired velocity, at the rate of the number calculated above
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);

        //Update the Rigidbody with this new velocity
        body.velocity = velocity;

    }

    private void runWithoutAcceleration() {
        //If we're not using acceleration and deceleration, just send our desired velocity (direction * max speed) to the Rigidbody
        velocity.x = desiredVelocity.x;

        body.velocity = velocity;
    }

    public Vector2 GetVelocity() { return velocity; }

    public void SetFrozen(bool val) { frozen = val; }
}
