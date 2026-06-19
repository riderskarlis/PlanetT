using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpaceshipController : MonoBehaviour, ISelectable
{
    static readonly List<SpaceshipController> all = new List<SpaceshipController>();
    static int nextId = 1;

    [Header("Ownership")]
    public string owner = "Player";

    public string Name => shipTag;
    public string Description => $"Owner: {owner}\nSpaceship ID: {ShipId}\nSpeed: {moveSpeed}";

    public float moveSpeed = 35f;
    public float rotationSpeed = 8f;
    public float surfaceOffset = 50f;
    public string shipTag = "Spaceship";

    public int ShipId { get; private set; }
    public bool HasMoveTarget => hasMoveTarget;

    [System.NonSerialized] public bool isPlayerCommandedMove = false;
    [System.NonSerialized] public bool isOnPlanet = false;

    Vector3 moveTargetWorld;
    Vector3 moveTargetLocal;
    bool hasMoveTarget;
    LowPolyPlanet targetPlanet;
    LowPolyPlanet lastTargetedPlanet;
    RenderSelection selectionRing;

    [Header("Color Customization")]
    public Renderer spaceshipRenderer;
    public Material spaceshipMaterial;
    public float emissionIntensity = 2f;

    [Header("Combat & Faction Setup")]
    public bool isEnemy = false;
    public float health = 100f;
    public float maxHealth = 100f;
    public bool aggressive = false;
    public float attackRange = 35f;
    public float detectionRange = 60f;
    public float attackDamage = 20f;
    public float fireRate = 0.5f;
    public AudioClip laserSound;
    public GameObject laserProjectilePrefab;
    public SpaceshipController attackTarget;

    private float lastFireTime;
    private LineRenderer laserRenderer;

    public static IReadOnlyList<SpaceshipController> All => all;

    void Awake()
    {
        ShipId = nextId++;
        if (string.IsNullOrEmpty(shipTag) || shipTag == "Spaceship")
            shipTag = "Ship_" + ShipId;

        // Initialize owner name from setup profile name (if player ship)
        if (!isEnemy)
        {
            owner = GameSetupData.profileName;
        }
        else
        {
            owner = "Enemy Faction";
        }

        selectionRing = GetComponent<RenderSelection>();
        if (selectionRing == null)
            selectionRing = gameObject.AddComponent<RenderSelection>();

        // Create a child GameObject for the laser LineRenderer to avoid conflict with selection ring
        GameObject laserObj = new GameObject("LaserBeam");
        laserObj.transform.SetParent(transform, false);
        laserObj.transform.localPosition = Vector3.zero;

        laserRenderer = laserObj.AddComponent<LineRenderer>();
        laserRenderer.startWidth = 0.25f;
        laserRenderer.endWidth = 0.25f;
        laserRenderer.useWorldSpace = true;
        laserRenderer.positionCount = 2;
        laserRenderer.material = new Material(Shader.Find("Sprites/Default"));
        laserRenderer.enabled = false;
    }

    void Start()
    {
        ApplySetupColor();
    }

    void ApplySetupColor()
    {
        int index = GameSetupData.colorIndex;
        Color[] colors = new Color[]
        {
            new Color(1f, 0f, 0f),       // Red
            new Color(1f, 0.5f, 0f),     // Orange
            new Color(1f, 1f, 0f),       // Yellow
            new Color(0f, 1f, 0f),       // Green
            new Color(0f, 0f, 1f),       // Blue
            new Color(0.29f, 0f, 0.51f), // Indigo
            new Color(0.5f, 0f, 0.5f)    // Violet
        };

        Color chosenColor;
        if (isEnemy)
        {
            chosenColor = Color.blue; // Enemies are always Blue
        }
        else
        {
            if (index >= 0 && index < colors.Length)
            {
                chosenColor = colors[index];
            }
            else
            {
                chosenColor = new Color(0.2f, 0.9f, 1f); // Default cyan
            }
        }

        // If a specific material asset is assigned, update its emission color directly
        if (spaceshipMaterial != null)
        {
            spaceshipMaterial.EnableKeyword("_EMISSION");
            spaceshipMaterial.SetColor("_EmissionColor", chosenColor * emissionIntensity);
        }

        // Apply color to the renderer's material emission at index 1 (fallback to 0 if only 1 material exists)
        if (spaceshipRenderer != null)
        {
            Material[] mats = spaceshipRenderer.materials;
            int targetIndex = (mats.Length > 1) ? 1 : 0;
            if (mats.Length > targetIndex)
            {
                mats[targetIndex].EnableKeyword("_EMISSION");
                mats[targetIndex].SetColor("_EmissionColor", chosenColor * emissionIntensity);
                spaceshipRenderer.materials = mats;
            }
        }
        else
        {
            // Fallback to any child renderer if none is assigned
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                Material[] mats = r.materials;
                int targetIndex = (mats.Length > 1) ? 1 : 0;
                if (mats.Length > targetIndex)
                {
                    mats[targetIndex].EnableKeyword("_EMISSION");
                    mats[targetIndex].SetColor("_EmissionColor", chosenColor * emissionIntensity);
                    r.materials = mats;
                }
            }
        }
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    public void SetSelected(bool value)
    {
        if (this == null || selectionRing == null) return;
        selectionRing.SetSelected(value);
    }

    public void SetMoveTarget(Vector3 worldTarget, LowPolyPlanet planet, Vector3 formationOffset = default)
    {
        hasMoveTarget = true;
        
        // Update targeting indicators
        if (lastTargetedPlanet != planet)
        {
            if (lastTargetedPlanet != null) lastTargetedPlanet.SetTargeted(false);
            if (planet != null && isPlayerCommandedMove) planet.SetTargeted(true);
            lastTargetedPlanet = planet;
        }

        targetPlanet = planet;
        moveTargetWorld = worldTarget + formationOffset;

        if (isOnPlanet)
        {
            // If we are on a planet but target is elsewhere, detach
            if (planet == null || transform.parent != planet.transform)
            {
                DetachFromPlanet();
            }
            else
            {
                // Target is on the same planet, update local target with tangent formation offset
                Vector3 localDir = transform.parent.InverseTransformPoint(worldTarget).normalized;
                Vector3 localOffset = transform.parent.InverseTransformVector(formationOffset) * 0.05f;
                moveTargetLocal = (localDir + localOffset).normalized * (planet.radius + surfaceOffset);
            }
        }
    }

    public void StopMoving()
    {
        hasMoveTarget = false;
        if (lastTargetedPlanet != null)
        {
            lastTargetedPlanet.SetTargeted(false);
            lastTargetedPlanet = null;
        }
    }

    void Update()
    {
        if (health <= 0f)
        {
            Debug.Log($"[SpaceshipController] Ship {ShipId} destroyed!");
            Destroy(gameObject);
            return;
        }

        UpdateCombat();

        if (hasMoveTarget)
        {
            Move();
        }

        ApplyOrientation();
    }

    void LateUpdate()
    {
        // Disable the laser renderer at the end of the frame to create a flashing fire effect
        if (laserRenderer != null)
        {
            laserRenderer.enabled = false;
        }
    }

    public void SetAttackTarget(SpaceshipController target)
    {
        attackTarget = target;
        if (target != null)
        {
            hasMoveTarget = false; // Override manual move targets to pursue
            isPlayerCommandedMove = false; // Override commanded move
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        health = Mathf.Max(0f, health);
        if (attackTarget == null && health > 0f && !isPlayerCommandedMove)
        {
            // Fight back if attacked (unless fleeing/moving)
            ScanForTargets();
        }
    }

    void UpdateCombat()
    {
        // 1. If executing a player move command, suspend combat targeting to allow retreat
        if (isPlayerCommandedMove)
        {
            attackTarget = null;
            return;
        }

        // 2. Scan for targets if we don't have one or if the target died
        if (attackTarget == null || attackTarget.health <= 0f)
        {
            attackTarget = null;
            ScanForTargets();
        }

        // 3. Pursue and attack target if active
        if (attackTarget != null)
        {
            float dist = Vector3.Distance(transform.position, attackTarget.transform.position);

            // Face the target (Euler rotation offset to match model alignment)
            Vector3 targetDir = (attackTarget.transform.position - transform.position).normalized;
            if (targetDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetDir, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            if (dist > attackRange)
            {
                // Pursue using SetMoveTarget so we inherit orbital/curved path mechanics
                LowPolyPlanet targetPlanetObj = null;
                if (attackTarget.isOnPlanet && attackTarget.transform.parent != null)
                {
                    targetPlanetObj = attackTarget.transform.parent.GetComponent<LowPolyPlanet>();
                }
                SetMoveTarget(attackTarget.transform.position, targetPlanetObj);
            }
            else
            {
                // Stop move command and fire laser
                StopMoving();
                FireLaserAtTarget();
            }
        }
    }

    void ScanForTargets()
    {
        // Enemies target players
        if (isEnemy)
        {
            SpaceshipController closest = null;
            float minDist = aggressive ? float.MaxValue : detectionRange;
            foreach (var other in all)
            {
                if (!other.isEnemy && other.health > 0f)
                {
                    float d = Vector3.Distance(transform.position, other.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = other;
                    }
                }
            }
            if (closest != null) attackTarget = closest;
        }
        // Players scan for nearby enemies in self-defense
        else
        {
            SpaceshipController closest = null;
            float minDist = detectionRange;
            foreach (var other in all)
            {
                if (other.isEnemy && other.health > 0f)
                {
                    float d = Vector3.Distance(transform.position, other.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = other;
                    }
                }
            }
            if (closest != null) attackTarget = closest;
        }
    }

    void FireLaserAtTarget()
    {
        if (Time.time - lastFireTime < fireRate)
            return;

        lastFireTime = Time.time;

        Vector3 targetDir = (attackTarget.transform.position - transform.position).normalized;
        Color laserColor = isEnemy ? Color.blue : GetPlayerLaserColor();

        // 1. If a projectile prefab is assigned, spawn it
        if (laserProjectilePrefab != null)
        {
            Vector3 spawnPos = transform.position + targetDir * 2f;
            GameObject proj = Instantiate(laserProjectilePrefab, spawnPos, Quaternion.LookRotation(targetDir));
            
            LaserProjectile projectileScript = proj.GetComponent<LaserProjectile>();
            if (projectileScript == null)
            {
                projectileScript = proj.AddComponent<LaserProjectile>();
            }

            projectileScript.target = attackTarget;
            projectileScript.damage = attackDamage * fireRate; // Scale damage DPS by fire rate
            
            // Color the projectile's material to match faction colors
            Renderer projRenderer = proj.GetComponentInChildren<Renderer>();
            if (projRenderer != null)
            {
                projRenderer.material.color = laserColor;
                projRenderer.material.EnableKeyword("_EMISSION");
                projRenderer.material.SetColor("_EmissionColor", laserColor * 3f);
            }
        }
        // 2. Otherwise, fallback to the instant LineRenderer laser beam
        else
        {
            // Apply damage instantly
            attackTarget.TakeDamage(attackDamage * fireRate);

            if (laserRenderer != null)
            {
                laserRenderer.startColor = laserColor;
                laserRenderer.endColor = laserColor;
                
                laserRenderer.SetPosition(0, transform.position);
                laserRenderer.SetPosition(1, attackTarget.transform.position);
                laserRenderer.enabled = true;
            }
        }

        // Play SFX
        if (SoundManager.Instance != null && laserSound != null)
        {
            SoundManager.Instance.PlaySFX(laserSound, 0.1f);
        }
    }

    Color GetPlayerLaserColor()
    {
        int index = GameSetupData.colorIndex;
        Color[] colors = new Color[]
        {
            new Color(1f, 0f, 0f),       // Red
            new Color(1f, 0.5f, 0f),     // Orange
            new Color(1f, 1f, 0f),       // Yellow
            new Color(0f, 1f, 0f),       // Green
            new Color(0f, 0f, 1f),       // Blue
            new Color(0.29f, 0f, 0.51f), // Indigo
            new Color(0.5f, 0f, 0.5f)    // Violet
        };
        if (index >= 0 && index < colors.Length)
        {
            return colors[index];
        }
        return new Color(0.2f, 0.9f, 1f); // Default cyan
    }

    void Move()
    {
        if (isOnPlanet)
        {
            // Curved orbital movement around the sphere instead of cutting straight through the planet
            float radius = transform.localPosition.magnitude;
            if (radius > 0.001f)
            {
                Vector3 currentDir = transform.localPosition / radius;
                Vector3 targetDir = moveTargetLocal.normalized;

                // Calculate angular speed (radians/sec) = linear speed / radius
                float angularSpeed = moveSpeed / radius;
                
                // Rotate position vector around the planet's center
                Vector3 nextDir = Vector3.RotateTowards(currentDir, targetDir, angularSpeed * Time.deltaTime, 0f);
                
                transform.localPosition = nextDir * radius;
            }

            if (Vector3.Distance(transform.localPosition, moveTargetLocal) < 0.5f)
            {
                hasMoveTarget = false;
                isPlayerCommandedMove = false;
                if (lastTargetedPlanet != null)
                {
                    lastTargetedPlanet.SetTargeted(false);
                    lastTargetedPlanet = null;
                }
            }
        }
        else
        {
            // World space approach
            Vector3 currentTargetPos = moveTargetWorld;
            if (targetPlanet != null)
            {
                // Update target based on moving planet
                Vector3 dir = (moveTargetWorld - targetPlanet.transform.position).normalized;
                currentTargetPos = targetPlanet.transform.position + dir * (targetPlanet.radius + surfaceOffset);

                // CHECK PROXIMITY FOR PARENTING
                float distToTarget = Vector3.Distance(transform.position, currentTargetPos);
                if (distToTarget < 10f) // Within 10 units of final altitude
                {
                    AttachToPlanet(targetPlanet);
                    moveTargetLocal = transform.parent.InverseTransformPoint(currentTargetPos);
                    return; // Continue in local space next frame
                }
            }
            else
            {
                currentTargetPos.y = 0; // Space plane
            }

            transform.position = Vector3.MoveTowards(transform.position, currentTargetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, currentTargetPos) < 0.1f)
            {
                hasMoveTarget = false;
                isPlayerCommandedMove = false;
                if (lastTargetedPlanet != null)
                {
                    lastTargetedPlanet.SetTargeted(false);
                    lastTargetedPlanet = null;
                }
            }
        }
    }

    void AttachToPlanet(LowPolyPlanet planet)
    {
        Vector3 worldPos = transform.position;
        Quaternion worldRot = transform.rotation;
        transform.SetParent(planet.transform);
        transform.position = worldPos;
        transform.rotation = worldRot;
        isOnPlanet = true;

        if (lastTargetedPlanet != null)
        {
            lastTargetedPlanet.SetTargeted(false);
            lastTargetedPlanet = null;
        }

        Debug.Log($"[SpaceshipController] Ship {ShipId} parented to {planet.name} (Close approach)");
    }

    void DetachFromPlanet()
    {
        Vector3 worldPos = transform.position;
        Quaternion worldRot = transform.rotation;
        transform.SetParent(null);
        transform.position = worldPos;
        transform.rotation = worldRot;
        isOnPlanet = false;
        Debug.Log($"[SpaceshipController] Ship {ShipId} detached from planet");
    }

    void ApplyOrientation()
    {
        if (!hasMoveTarget && !isOnPlanet)
            return;

        Quaternion modelOffset = Quaternion.Euler(-90f, 0f, 0f);
        Vector3 up = Vector3.up;
        Vector3 forward = transform.forward;

        LowPolyPlanet activePlanet = isOnPlanet ? transform.parent.GetComponent<LowPolyPlanet>() : targetPlanet;

        if (activePlanet != null)
        {
            float distToCenter = Vector3.Distance(transform.position, activePlanet.transform.position);
            float surfaceDistance = distToCenter - activePlanet.radius;

            // Start tilting bottom to planet at 120 units away, fully tilted at 60 units
            float tiltFactor = Mathf.InverseLerp(120f, 60f, surfaceDistance);
            Vector3 surfaceNormal = (transform.position - activePlanet.transform.position).normalized;
            up = Vector3.Slerp(Vector3.up, surfaceNormal, tiltFactor);
        }

        if (isOnPlanet)
        {
            // Use local calculations to maintain stability while parented
            Vector3 localUp = transform.localPosition.normalized;
            
            // Extract local forward from current rotation to avoid snaps
            Vector3 localForward = (transform.localRotation * Quaternion.Inverse(modelOffset)) * Vector3.forward;

            if (hasMoveTarget)
            {
                Vector3 targetDirLocal = (moveTargetLocal - transform.localPosition);
                targetDirLocal = Vector3.ProjectOnPlane(targetDirLocal, localUp);
                if (targetDirLocal.sqrMagnitude > 0.001f)
                    localForward = targetDirLocal.normalized;
            }

            if (localForward.sqrMagnitude > 0.001f)
            {
                Quaternion targetLocalRot = Quaternion.LookRotation(localForward, localUp) * modelOffset;
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRot, rotationSpeed * Time.deltaTime);
            }
        }
        else if (hasMoveTarget) // Only rotate in space if we have a target
        {
            Vector3 currentTargetPos = moveTargetWorld;
            if (targetPlanet != null)
            {
                Vector3 dir = (moveTargetWorld - targetPlanet.transform.position).normalized;
                currentTargetPos = targetPlanet.transform.position + dir * (targetPlanet.radius + surfaceOffset);
            }
            else
            {
                currentTargetPos.y = 0;
            }

            Vector3 targetDirWorld = (currentTargetPos - transform.position);
            targetDirWorld = Vector3.ProjectOnPlane(targetDirWorld, up);
            
            if (targetDirWorld.sqrMagnitude > 0.001f)
                forward = targetDirWorld.normalized;

            if (forward.sqrMagnitude > 0.001f)
            {
                Quaternion targetWorldRot = Quaternion.LookRotation(forward, up) * modelOffset;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetWorldRot, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
