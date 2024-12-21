using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 25;
    private Health hp;

    private void OnEnable()
    {
        hp = GetComponent<Health>();

        hp.damaged += () => transform.forward = Vector3.up;
        hp.killed  += () => Destroy(gameObject);
    }

    void Update()
    {
        transform.forward = Vector3.MoveTowards(transform.forward, (Player.Instance.rb.position - transform.position).normalized, Time.deltaTime * rotateSpeed);
    }
}
