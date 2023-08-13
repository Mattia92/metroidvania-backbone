using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour {
    [SerializeField] private float damage;
    [SerializeField] private float hitForce;
    [SerializeField] private float lifeTime = 1;
    [SerializeField] private int speed;

    // Start is called before the first frame update
    void Start() {
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate() {
        transform.position += speed * transform.right;
    }

    // Detect hit
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Enemy")) collision.GetComponent<EnemyController>().EnemyHit(damage, (collision.transform.position - transform.position).normalized, -hitForce);
    }
}
