using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyController : MonoBehaviour {
    protected Rigidbody2D rigidbody2D;

    [Header("Parameters Settings")]
    [SerializeField] protected float health;
    [SerializeField] protected float knockbackLength;
    [SerializeField] protected float knockbackFactor;
    [SerializeField] protected bool isKnockbacking = false;
    protected float knockbackTimer;

    [Space(5)]
    [Header("AI Settings")]
    [SerializeField] protected PlayerController playerController;
    [SerializeField] protected float speed;

    // Awake is called when the script instance is being loaded
    public virtual void Awake() {
        rigidbody2D = GetComponent<Rigidbody2D>();
        playerController = PlayerController.Instance;
    }

    // Start is called before the first frame update
    public virtual void Start() {

    }

    // Update is called once per frame
    public virtual void Update() {
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

    public virtual void EnemyHit(float damageAmount, Vector2 hitDirection, float hitForce) {
        health -= damageAmount;
        if (!isKnockbacking) rigidbody2D.AddForce(-hitForce * knockbackFactor * hitDirection);
    }
}
