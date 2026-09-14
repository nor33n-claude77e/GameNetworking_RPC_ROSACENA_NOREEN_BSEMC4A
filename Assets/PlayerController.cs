using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    private Animator animator;
    private bool isDead = false;

    private void Awake()
    {
        // Finds Animator on this object OR on the child Stickman model
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            transform.position = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
        }
    }

    public void SetDeadState(bool dead)
    {
        isDead = dead;
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    private void Update()
    {
        if (!IsOwner || isDead) return;

        HandleMovement();
        HandleShooting();
    }

    private void HandleMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;

        // Read Input from New Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
        }
        else
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }

        Vector3 moveInput = new Vector3(horizontal, 0f, vertical).normalized;
        float currentSpeed = moveInput.magnitude;

        // Send to Server
        MoveServerRpc(moveInput, Time.deltaTime);

        // Update local animation
        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed);
        }
    }

    [ServerRpc]
    private void MoveServerRpc(Vector3 moveDir, float deltaTime)
    {
        if (isDead) return;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            // Smoothly rotate to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);

            // Move in that direction
            transform.position += moveDir * moveSpeed * deltaTime;
        }

        // Sync Speed parameter on Server for NetworkAnimator
        if (animator != null)
        {
            animator.SetFloat("Speed", moveDir.magnitude);
        }
    }

    private void HandleShooting()
    {
        bool shootPressed = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) shootPressed = true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) shootPressed = true;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) shootPressed = true;

        if (shootPressed)
        {
            Transform spawn = firePoint != null ? firePoint : transform;
            FireServerRpc(spawn.position, spawn.rotation);
        }
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (isDead || bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, spawnRotation);

        if (bullet.TryGetComponent(out NetworkBullet netBullet))
        {
            netBullet.InitializeBullet(OwnerClientId);
        }

        NetworkObject netObj = bullet.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
        }
    }
}