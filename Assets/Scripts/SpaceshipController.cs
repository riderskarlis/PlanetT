using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpaceshipController : MonoBehaviour, ISelectable
{
    static readonly List<SpaceshipController> all = new List<SpaceshipController>();
    static int nextId = 1;

    public string Name => shipTag;
    public string Description => $"Spaceship ID: {ShipId}\nSpeed: {moveSpeed}";

    public float moveSpeed = 35f;
    public float rotationSpeed = 8f;
    public float surfaceOffset = 50f;
    public string shipTag = "Spaceship";

    public int ShipId { get; private set; }
    public bool HasMoveTarget => hasMoveTarget;

    Vector3 moveTargetWorld;
    Vector3 moveTargetLocal;
    bool hasMoveTarget;
    bool isOnPlanet = false;
    LowPolyPlanet targetPlanet;
    LowPolyPlanet lastTargetedPlanet;
    RenderSelection selectionRing;

    public static IReadOnlyList<SpaceshipController> All => all;

    void Awake()
    {
        ShipId = nextId++;
        if (string.IsNullOrEmpty(shipTag) || shipTag == "Spaceship")
            shipTag = "Ship_" + ShipId;

        selectionRing = GetComponent<RenderSelection>();
        if (selectionRing == null)
            selectionRing = gameObject.AddComponent<RenderSelection>();
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    public void SetSelected(bool value) => selectionRing.SetSelected(value);

    public void SetMoveTarget(Vector3 worldTarget, LowPolyPlanet planet)
    {
        hasMoveTarget = true;
        
        // Update targeting indicators
        if (lastTargetedPlanet != planet)
        {
            if (lastTargetedPlanet != null) lastTargetedPlanet.SetTargeted(false);
            if (planet != null) planet.SetTargeted(true);
            lastTargetedPlanet = planet;
        }

        targetPlanet = planet;
        moveTargetWorld = worldTarget;

        if (isOnPlanet)
        {
            // If we are on a planet but target is elsewhere, detach
            if (planet == null || transform.parent != planet.transform)
            {
                DetachFromPlanet();
            }
            else
            {
                // Target is on the same planet, update local target
                moveTargetLocal = transform.parent.InverseTransformPoint(worldTarget).normalized * (planet.radius + surfaceOffset);
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
        if (hasMoveTarget)
        {
            Move();
        }

        ApplyOrientation();
    }

    void Move()
    {
        if (isOnPlanet)
        {
            // Stable movement in local space
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, moveTargetLocal, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.localPosition, moveTargetLocal) < 0.1f)
            {
                hasMoveTarget = false;
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
