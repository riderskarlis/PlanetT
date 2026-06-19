using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    public float speed = 80f;
    public float damage = 10f;
    public SpaceshipController target;

    void Update()
    {
        // Destroy projectile if target is lost or destroyed
        if (target == null || target.health <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Move towards the target spaceship
        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);

        // Orient projectile to face target
        Vector3 targetDir = (target.transform.position - transform.position).normalized;
        if (targetDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(targetDir);
        }

        // Check for impact
        if (Vector3.Distance(transform.position, target.transform.position) < 1.5f)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
