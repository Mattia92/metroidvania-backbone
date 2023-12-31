using UnityEngine;

// Inerith from EnemyController
public class EnemyTemplate : EnemyController {

    // Start is called before the first frame update
    void Start() {
        base.Start();
        rigidbody2D.gravityScale = 12f;
    }

    // Update is called once per frame
    protected override void Update() {
        base.Update();
        // Move towards the player
        if (!isKnockbacking) transform.position = Vector2.MoveTowards(transform.position, new Vector2(PlayerController.Instance.transform.position.x, transform.position.y), speed * Time.deltaTime);
    }

    public override void EnemyHit(float damageAmount, Vector2 hitDirection, float hitForce) {
        base.EnemyHit(damageAmount, hitDirection, hitForce);
    }
}
