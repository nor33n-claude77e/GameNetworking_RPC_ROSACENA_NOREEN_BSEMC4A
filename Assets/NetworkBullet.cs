using UnityEngine;
using Unity.Netcode;

public class NetworkBullet : NetworkBehaviour
{
    [SerializeField] private float speed = 25f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 25;

    private Rigidbody rb;
    private ulong shooterClientId;
    private bool hasInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void InitializeBullet(ulong shooterId)
    {
        shooterClientId = shooterId;
        hasInitialized = true;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearVelocity = transform.forward * speed;
            }
            Invoke(nameof(DespawnBullet), lifeTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (collision.gameObject.TryGetComponent(out PlayerHealthAndScore targetPlayer))
        {
            if (hasInitialized && targetPlayer.OwnerClientId == shooterClientId)
            {
                return;
            }
            targetPlayer.TakeDamage(damage, shooterClientId);
        }

        Debug.Log($"({NetworkObjectId}) BULLET: Hit: {collision.gameObject.name}");
        DespawnBullet();
    }

    private void DespawnBullet()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}