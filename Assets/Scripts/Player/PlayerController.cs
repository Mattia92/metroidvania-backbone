using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {
    // Singleton to reference in other scripts
    public static PlayerController Instance;
    [HideInInspector] public PlayerStateList playerStateList;
    private Animator anim;
    private Rigidbody2D rigidbody2D;
    private SpriteRenderer spriteRenderer;
    private float xAxis;
    private float yAxis;
    private float gravity;

    [Header("Health Settings")]
    [SerializeField] public int health;
    [SerializeField] public int maxHealth;
    [SerializeField] private float timeToHeal;
    private float healTimer;
    // Delegate function that handles any update needed for health
    public delegate void OnHealthChangedDelegate();
    [HideInInspector] public OnHealthChangedDelegate onHealthChangedCallback;

    [Space(5)]
    [Header("Damage Settings")]
    [SerializeField] private GameObject damagePE;
    [SerializeField] private float damageFlashSpeed;
    [SerializeField] private float damagePETime = 1.5f;
    [SerializeField] private float invincibilityFrameTime = 1f;
    // Stop time when player damaged
    private bool restoreTime;
    private float restoreTimeSpeed;

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
    // The middle of the side attack area
    [SerializeField] private Transform sideAttackTransform;
    // How large the area of side attack is
    [SerializeField] private Vector2 sideAttackArea;
    // The middle of the up attack area
    [SerializeField] private Transform upAttackTransform;
    // How large the area of up attack is
    [SerializeField] private Vector2 upAttackArea;
    // The middle of the down attack area
    [SerializeField] private Transform downAttackTransform;
    // How large the area of down attack is
    [SerializeField] private Vector2 downAttackArea;
    // The layer the player can attack and knockback off
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

    [Space(5)]
    [Header("Mana Settings")]
    [SerializeField] private Image manaStorage;
    [SerializeField] private float mana;
    [SerializeField] private float maxMana;
    [SerializeField] private float manaDrainSpeed;
    [SerializeField] private float manaGain;

    [Space(5)]
    [Header("Spell Settings")]
    [SerializeField] private GameObject sideSpellFireball;
    [SerializeField] private GameObject upSpellExplosion;
    [SerializeField] private GameObject downSpellFireball;
    [SerializeField] private float spellDamage;
    [SerializeField] private float manaSpellCost = 0.3f;
    [SerializeField] private float castCooldown = 0.5f;
    [SerializeField] private float downSpellForce;
    private float castTime;

    // Awake is called when the script instance is being loaded
    void Awake() {
        // Destroy any duplicate to keep Singleton
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Keep singleton between scenes
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start() {
        anim = GetComponent<Animator>();
        playerStateList = GetComponent<PlayerStateList>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        Health = maxHealth;
        Mana = maxMana;
        manaStorage.fillAmount = Mana;
        gravity = rigidbody2D.gravityScale;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(sideAttackTransform.position, sideAttackArea);
        Gizmos.DrawWireCube(upAttackTransform.position, upAttackArea);
        Gizmos.DrawWireCube(downAttackTransform.position, downAttackArea);
    }

    private void FixedUpdate() {
        // Dash cannot be interrupted by movements
        if (playerStateList.dashing) return;

        Knockback();
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
        RestoreTimeScale();
        FlashWhileInvincible();
        Heal();
        CastSpell();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.GetComponent<EnemyController>() != null && playerStateList.casting) {
            collision.GetComponent<EnemyController>().EnemyHit(spellDamage, (collision.transform.position - transform.position).normalized, -knockbackYSpeed);
        }
    }

    public bool Grounded() {
        return isOnGround() || isOnGroundEdge();
    }

    public int Health {
        get { return health; }
        set {
            if (health != value) {
                health = Mathf.Clamp(value, 0, maxHealth);
                if (onHealthChangedCallback != null) {
                    onHealthChangedCallback.Invoke();
                }
            }
        }
    }

    public float Mana {
        get { return mana; }
        set {
            if (mana != value) {
                mana = Mathf.Clamp(value, 0, maxMana);
                manaStorage.fillAmount = Mana;
            }
        }
    }

    public void TakeDamage(float damageAmount) {
        Health -= Mathf.RoundToInt(damageAmount);
        StartCoroutine(HandleInvincibilityFrames());
    }

    public void HitStopTime(float newTimeScale, int restoreSpeed, float delay) {
        restoreTimeSpeed = restoreSpeed;
        Time.timeScale = newTimeScale;

        // Player has taken damage
        if (delay > 0) {
            StopCoroutine(StartTimeAgain(delay));
            StartCoroutine(StartTimeAgain(delay));
        } else {
            restoreTime = true;
        }
    }

    private void Heal() {
        if (Input.GetButton("Heal") && Health < maxHealth && Mana > 0 && !playerStateList.jumping && !playerStateList.dashing) {
            playerStateList.healing = true;
            anim.SetBool("Healing", true);

            // Healing
            healTimer += Time.deltaTime;
            if (healTimer >= timeToHeal) {
                Health++;
                healTimer = 0;
            }

            // Drain mana for healing cost
            Mana -= Time.deltaTime * manaDrainSpeed;
        } else {
            playerStateList.healing = false;
            anim.SetBool("Healing", false);
            healTimer = 0;
        }
    }

    private IEnumerator HandleInvincibilityFrames() {
        playerStateList.invincible = true;
        GameObject damageParticleEffect = Instantiate(damagePE, transform.position, Quaternion.identity);
        Destroy(damageParticleEffect, damagePETime);
        anim.SetTrigger("TakingDamage");
        yield return new WaitForSeconds(invincibilityFrameTime);
        playerStateList.invincible = false;
    }

    private void FlashWhileInvincible() {
        // Change from white to black and back to white and keep diong so so long as the parameters are fulfilled
        spriteRenderer.material.color = playerStateList.invincible ? Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * damageFlashSpeed, 1.0f)) : Color.white;
    }

    private IEnumerator StartTimeAgain(float delay) {
        restoreTime = true;
        yield return new WaitForSeconds(delay);
    }

    private void RestoreTimeScale() {
        if (restoreTime) {
            if (Time.timeScale < 1) {
                Time.timeScale += Time.deltaTime * restoreTimeSpeed;
            } else {
                Time.timeScale = 1;
                restoreTime = false;
            }
        }
    }

    private void GetInputs() {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetButtonDown("Attack");
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
        // Condition to enable jump
        if (!playerStateList.jumping && jumpBufferCounter > 0 && coyoteTimeCounter > 0) {
            playerStateList.jumping = true;
            rigidbody2D.velocity = new Vector3(rigidbody2D.velocity.x, jumpForce);
        }

        if (!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump")) {
            playerStateList.jumping = true;
            airJumpCounter++;
            rigidbody2D.velocity = new Vector3(rigidbody2D.velocity.x, jumpForce);
        }

        // Condition to handle jump height
        if (Input.GetButtonUp("Jump") && rigidbody2D.velocity.y > 3) {
            playerStateList.jumping = false;
            if (enableJumpHeight) rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, 0);
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
        rigidbody2D.velocity = new Vector2(dashSpeed * (playerStateList.lookingRight ? 1 : -1), 0);
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

    private void CastSpell() {
        if (Input.GetButton("CastSpell") && castTime >= castCooldown && Mana > manaSpellCost) {
            playerStateList.casting = true;
            castTime = 0;
            StartCoroutine(CastCoroutine());
        } else {
            castTime += Time.deltaTime;
        }

        if (Grounded()) downSpellFireball.SetActive(false);

        // If down spell is active, force player down unil grounded
        if (downSpellFireball.activeInHierarchy) rigidbody2D.velocity += downSpellForce * Vector2.down;
    }

    private void Hit(Transform attackTransform, Vector2 attackArea, ref bool knockbackDir, float knockbackStrenght) {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(attackTransform.position, attackArea, 0, whatIsAttackable);

        if (objectsToHit.Length > 0) {
            knockbackDir = true;
        }

        for (int i = 0; i < objectsToHit.Length; i++) {
            if (objectsToHit[i].GetComponent<EnemyController>() != null) {
                objectsToHit[i].GetComponent<EnemyController>().EnemyHit(attackDamage, (transform.position - objectsToHit[i].transform.position).normalized, knockbackStrenght);
                if (objectsToHit[i].CompareTag("Enemy")) Mana += manaGain;
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

    private IEnumerator CastCoroutine() {
        anim.SetBool("Casting", true);
        yield return new WaitForSeconds(0.15f);

        // Side spell cast
        if (yAxis == 0 || (yAxis < 0 && Grounded())) {
            // Cast on side if not pressing up / down or pressing down but on ground
            GameObject spell = Instantiate(sideSpellFireball, sideAttackTransform.position, Quaternion.identity);
            spell.transform.eulerAngles = playerStateList.lookingRight ? Vector3.zero : new Vector2(spell.transform.eulerAngles.x, 180);
            playerStateList.knockbackingX = true;
        } else if (yAxis > 0) {
            // Cast up if pressing up
            Instantiate(upSpellExplosion, transform);
            // Freeze player in place when casting upwards
            rigidbody2D.velocity = Vector2.zero;
        } else if (yAxis < 0 && !Grounded()) {
            // Cast down if pressing down and not on ground
            downSpellFireball.SetActive(true);
        }

        // Drain mana
        Mana -= manaSpellCost;
        // Wait for the amount of time from the cast to the end of the animation
        yield return new WaitForSeconds(0.35f);

        anim.SetBool("Casting", false);
        playerStateList.casting = false;
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
