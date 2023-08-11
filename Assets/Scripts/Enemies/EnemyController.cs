using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyController : MonoBehaviour {
    protected Rigidbody2D rigidbody2D;

    [Header("Health Settings")]
    [SerializeField] protected float health;

    [Space(5)]
    [Header("Movement Settings")]
    [SerializeField] protected float speed;

    [Space(5)]
    [Header("Attack Settings")]
    [SerializeField] protected float attackDamage;

    [Space(5)]
    [Header("Knockback Settings")]
    [SerializeField] protected float knockbackLength;
    [SerializeField] protected float knockbackFactor;
    [SerializeField] protected bool isKnockbacking = false;
    protected float knockbackTimer;

    [Space(5)]
    [Header("AI Settings")]
    [SerializeField] protected PlayerController playerController;

    // Awake is called when the script instance is being loaded
    protected virtual void Awake() {
        rigidbody2D = GetComponent<Rigidbody2D>();
        playerController = PlayerController.Instance;
    }

    // Start is called before the first frame update
    protected virtual void Start() {

    }

    // Update is called once per frame
    protected virtual void Update() {
        if (health <= 0) Destroy(gameObject);
        if (isKnockbacking) {
            if (knockbackTimer < knockbackLength) {
                knockbackTimer += Time.deltaTime;
            } else {
                isKnockbacking = false;
                knockbackTimer = 0;
            }
        }
    }

    protected void OnTriggerStay2D(Collider2D collision) {
        if (collision.CompareTag("Player") && !PlayerController.Instance.playerStateList.invincible) {
            Attack();
            PlayerController.Instance.HitStopTime(0, 5, 0.5f);
        }
    }

    public virtual void Attack() {
        PlayerController.Instance.TakeDamage(attackDamage);
    }

    public virtual void EnemyHit(float damageAmount, Vector2 hitDirection, float hitForce) {
        health -= damageAmount;
        if (!isKnockbacking) rigidbody2D.AddForce(-hitForce * knockbackFactor * hitDirection);
    }
}
