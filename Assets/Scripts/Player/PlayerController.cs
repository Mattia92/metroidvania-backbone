using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviour {
    // Singleton to reference in other scripts
    public static PlayerController Instance;
    [HideInInspector] public PlayerStateList playerStateList;
    private Animator anim;
    private Rigidbody2D rigidbody2D;
    private float xAxis;
    private float yAxis;
    private float gravity;

    [Header("Health Settings")]
    [SerializeField] public int health;
    [SerializeField] public int maxHealth;
    [SerializeField] private float invincibilityFrameTime = 1f;

    [Space(5)]
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
    private bool canDash = true;
    private bool hasDashed = false;

    [Space(5)]
    [Header("Attack Settings")]
    [SerializeField] private GameObject slashEffect;
    [SerializeField] private Transform sideAttackTransform;
    [SerializeField] private Transform upAttackTransform;
    [SerializeField] private Transform downAttackTransform;
    [SerializeField] private Vector2 sideAttackArea;
    [SerializeField] private Vector2 upAttackArea;
    [SerializeField] private Vector2 downAttackArea;
    [SerializeField] private LayerMask whatIsAttackable;
    [SerializeField] private float attackDamage;
    private float attackTime;
    private float attackCooldown;
    private bool attack = false;

    [Space(5)]
    [Header("Knockback Settings")]
    [SerializeField] private int knockbackXSteps = 0;
    [SerializeField] private int knockbackYSteps = 0;
    [SerializeField] private float knockbackXSpeed = 0f;
    [SerializeField] private float knockbackYSpeed = 0f;
    private int stepsXKnockbacked;
    private int stepsYKnockbacked;

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

        health = maxHealth;
        gravity = rigidbody2D.gravityScale;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(sideAttackTransform.position, sideAttackArea);
        Gizmos.DrawWireCube(upAttackTransform.position, upAttackArea);
        Gizmos.DrawWireCube(downAttackTransform.position, downAttackArea);
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
        Attack();
        Knockback();
    }

    public bool Grounded() {
        return isOnGround() || isOnGroundEdge();
    }

    public void TakeDamage(float damageAmount) {
        health -= Mathf.RoundToInt(damageAmount);
        StartCoroutine(HandleInvincibilityFrames());
    }

    private IEnumerator HandleInvincibilityFrames() {
        playerStateList.invincible = true;
        anim.SetTrigger("TakingDamage");
        ClampHealth();
        yield return new WaitForSeconds(invincibilityFrameTime);
        playerStateList.invincible = false;
    }

    private void GetInputs() {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetMouseButtonDown(0);
    }

    private void ClampHealth() {
        health = Mathf.Clamp(health, 0, maxHealth);
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
        playerStateList.lookingRight = xAxis < 0 ? false : (xAxis == 0 ? playerStateList.lookingRight : true);
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

    private void Attack() {
        attackTime += Time.deltaTime;
        if (attack && attackTime >= attackCooldown) {
            attackTime = 0;
            anim.SetTrigger("Attacking");

            if (yAxis == 0 || (yAxis < 0 && Grounded())) {
                // Attack on side if not moving up / down or moving down but on ground
                Hit(sideAttackTransform, sideAttackArea, ref playerStateList.knockbackingX, knockbackXSpeed);
                Instantiate(slashEffect, sideAttackTransform);
            } else if (yAxis > 0) {
                // Attack up if moving up
                Hit(upAttackTransform, upAttackArea, ref playerStateList.knockbackingY, knockbackYSpeed);
                SlashEffectAngle(slashEffect, 90, upAttackTransform);
            } else if (yAxis < 0 && !Grounded()) {
                // Attack down if moving down and not on ground
                Hit(downAttackTransform, downAttackArea, ref playerStateList.knockbackingY, knockbackYSpeed);
                SlashEffectAngle(slashEffect, -90, downAttackTransform);
            }
        }
    }

    private void Hit(Transform attackTransform, Vector2 attackArea, ref bool knockbackDir, float knockbackStrenght) {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(attackTransform.position, attackArea, 0, whatIsAttackable);

        if (objectsToHit.Length > 0) {
            knockbackDir = true;
        }

        for (int i = 0; i < objectsToHit.Length; i++) {
            if (objectsToHit[i].GetComponent<EnemyController>() != null) {
                objectsToHit[i].GetComponent<EnemyController>().EnemyHit(attackDamage, (transform.position - objectsToHit[i].transform.position).normalized, knockbackStrenght);
            }
        }
    }

    private void SlashEffectAngle(GameObject slashEffect, int effectAngle, Transform attackTransform) {
        slashEffect = Instantiate(slashEffect, attackTransform);

        // Rotate slash effect
        slashEffect.transform.eulerAngles = new Vector3(0, 0, effectAngle);
        // Stretch slash effect to fit attack area
        slashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);

    }

    private void Knockback() {
        if (playerStateList.knockbackingX) {
            rigidbody2D.velocity = new Vector2(playerStateList.lookingRight ? -knockbackXSpeed : knockbackXSpeed, 0);
        }

        if (playerStateList.knockbackingY) {
            rigidbody2D.gravityScale = 0;
            rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, yAxis < 0 ? knockbackYSpeed : -knockbackYSpeed);
            airJumpCounter = 0;
        } else {
            rigidbody2D.gravityScale = gravity;
        }

        // Stop knockback
        if (playerStateList.knockbackingX && stepsXKnockbacked < knockbackXSteps) stepsXKnockbacked++;
        else StopKnockbackX();
        if (playerStateList.knockbackingY && stepsYKnockbacked < knockbackYSteps) stepsYKnockbacked++;
        else StopKnockbackY();
        if (Grounded()) StopKnockbackY();
    }

    private void StopKnockbackX() {
        stepsXKnockbacked = 0;
        playerStateList.knockbackingX = false;
    }

    private void StopKnockbackY() {
        stepsYKnockbacked = 0;
        playerStateList.knockbackingY = false;
    }

    private bool isOnGround() {
        return Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround);
    }

    private bool isOnGroundEdge() {
        return Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround) || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround);
    }
}
