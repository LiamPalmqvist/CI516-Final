using UnityEngine;
public class Bullet : MonoBehaviour
{
    public BaseUnit parent;
    // TODO: CREATE BULLET COLLISION WITH PLAYERS - I don't know if this works yet, haven't tested it.
    private void Update()
    {
        transform.position += transform.rotation.eulerAngles.normalized * Time.deltaTime;
        if (transform.position.x < 0 || transform.position.z < 0 || transform.position.x > 100 || transform.position.z > 100) Destroy(gameObject);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.TryGetComponent<BaseUnit>(out var unit)) return;
        if (parent == unit) return;
        
        unit.TakeDamage(10);
    }
}
