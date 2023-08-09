using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviour {
    // Singleton to reference in other scripts
    public static PlayerController Instance;
    private Animator anim;
    private PlayerStateList playerStateList;
    private Rigidbody2D rigidbody2D;
    private float xAxis;
    private float gravity;
    private bool canDash = true;
    private bool hasDashed = false;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 1;

    [Space(5)]
    [Header("Jump Settings")]
    [SerializeField] private bool enableJumpHeight = false;
    [SerializeField] private float jumpForce = 1;
    [SerializeField] private int jumpBufferFrames;
    [SerializeField] private int maxAirJumps;
    [SerializeField] private float coyoteTime;
    private int jumpBufferCounter = 0;
    private int airJumpCounter = 0;
    private float coyoteTimeCounter = 0;

    [Space(5)]
    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private LayerMask whatIsGround;

    [Space(5)]
    [Header("Dash Settings")]
    [SerializeField] private GameObject dashEffect;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;

    // Awake is called when the script instance is being loaded
    void Awake() {
        // Destroy any duplicate to keep Singleton
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // Start is called before the first frame update
    void Start() {
        anim = GetComponent<Animator>();
        playerStateList = GetComponent<PlayerStateList>();
        rigidbody2D = GetComponent<Rigidbody2D>();

        gravity = rigidbody2D.gravityScale;
    }

    // Update is called once per frame
    void Update() {
        GetInputs();
        UpdateJumpVariables();

        // Dash cannot be interrupted by movements
        if (playerStateList.dashing) return;

        Flip();
        Move();
        Jump();
        StartDash();
    }

    public bool Grounded() {
        return isOnGround() || isOnGroundEdge();
    }

    private void GetInputs() {
        xAxis = Input.GetAxisRaw("Horizontal");
    }

    private void UpdateJumpVariables() {
        if (Grounded()) {
            playerStateList.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        } else {
            coyoteTimeCounter -= Time.deltaTime;
        }

        jumpBufferCounter = Input.GetButtonDown("Jump") ? jumpBufferFrames : jumpBufferCounter - 1;
    }

    private void Flip() {
        transform.localScale = new Vector2(xAxis < 0 ? -1 : (xAxis == 0 ? transform.localScale.x : 1), transform.localScale.y);
    }

    private void Move() {
        rigidbody2D.velocity = new Vector2(walkSpeed * xAxis, rigidbody2D.velocity.y);
        // Start Walking animation
        anim.SetBool("Walking", rigidbody2D.velocity.x != 0 && Grounded());
    }

    private void Jump() {
        // Condition to handle jump height
        if (Input.GetButtonUp("Jump") && rigidbody2D.velocity.y > 0) {
            if (enableJumpHeight) rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, 0);
            playerStateList.jumping = false;
        }

        if (!playerStateList.jumping) {
            // Condition to enable jump
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0) {
                rigidbody2D.velocity = new Vector3(rigidbody2D.velocity.x, jumpForce);
                playerStateList.jumping = true;
            } else if (!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump")) {
                rigidbody2D.velocity = new Vector3(rigidbody2D.velocity.x, jumpForce);
                playerStateList.jumping = true;
                airJumpCounter++;
            }
        }
        // Start Jumping animation
        anim.SetBool("Jumping", !Grounded());
    }

    private void StartDash() {
        // Condition to dash only once in air
        if (Input.GetButtonDown("Dash") && canDash && !hasDashed) {
            StartCoroutine(Dash());
            hasDashed = true;
        }

        // Can dash if hit ground
        if (Grounded()) hasDashed = false;
    }

    private IEnumerator Dash() {
        canDash = false;
        playerStateList.dashing = true;
        anim.SetTrigger("Dashing");
        rigidbody2D.gravityScale = 0;
        rigidbody2D.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        if (Grounded()) Instantiate(dashEffect, transform);
        yield return new WaitForSeconds(dashTime);
        rigidbody2D.gravityScale = gravity;
        playerStateList.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private bool isOnGround() {
        return Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround);
    }

    private bool isOnGroundEdge() {
        return Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround) || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround);
    }
}
