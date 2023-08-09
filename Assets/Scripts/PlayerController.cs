using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviour {
    // Singleton to reference in other scripts
    public static PlayerController Instance;
    private Rigidbody2D rigidbody2D;
    private float xAxis;
    Animator anim;

    [Header("Horizontal Movement Settings")]
    [SerializeField] private float walkSpeed = 1;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Jump Settings")]
    [SerializeField] private bool enableJumpHeight = false;
    [SerializeField] private float jumpForce = 1;

    // Awake is called when the script instance is being loaded
    void Awake() {
        // Destroy any duplicate to keep Singleton
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // Start is called before the first frame update
    void Start() {
        rigidbody2D = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update() {
        GetInputs();
        Move();
        Flip();
        Jump();
    }

    void GetInputs() {
        xAxis = Input.GetAxisRaw("Horizontal");
    }

    void Flip() {
        transform.localScale = new Vector2(xAxis < 0 ? -1 : (xAxis == 0 ? transform.localScale.x : 1), transform.localScale.y);
    }

    void Jump() {
        // Condition to handle jump height
        if (enableJumpHeight && Input.GetButtonUp("Jump") && rigidbody2D.velocity.y > 0) rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, 0);
        // Condition to enable jump
        if (Input.GetButtonDown("Jump") && Grounded()) rigidbody2D.velocity = new Vector3(rigidbody2D.velocity.x, jumpForce);
        // Start Jumping animation
        anim.SetBool("Jumping", !Grounded());
    }

    private void Move() {
        rigidbody2D.velocity = new Vector2(walkSpeed * xAxis, rigidbody2D.velocity.y);
        // Start Walking animation
        anim.SetBool("Walking", rigidbody2D.velocity.x != 0 && Grounded());
    }

    public bool Grounded() {
        return isOnGround() || isOnGroundEdge();
    }

    private bool isOnGround() {
        return Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround);
    }

    private bool isOnGroundEdge() {
        return Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround) || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround);
    }
}
