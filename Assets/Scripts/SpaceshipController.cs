using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpaceshipController : MonoBehaviour
{
    static readonly List<SpaceshipController> all = new List<SpaceshipController>();
    static int nextId = 1;

    public float moveSpeed = 25f;
    public string shipTag = "Spaceship";

    public int ShipId { get; private set; }
    public bool HasMoveTarget => hasMoveTarget;
    public Vector3 MoveTarget => moveTarget;

    Vector3 moveTarget;
    bool hasMoveTarget;
    bool flatMove;
    RenderSelection selectionRing;

    public static IReadOnlyList<SpaceshipController> All => all;

    void Awake()
    {
        ShipId = nextId++;
        if (string.IsNullOrEmpty(shipTag))
            shipTag = "Ship_" + ShipId;
        else if (shipTag == "Spaceship")
            shipTag = "Ship_" + ShipId;

        selectionRing = GetComponent<RenderSelection>();
        if (selectionRing == null)
            selectionRing = gameObject.AddComponent<RenderSelection>();
    }

    void OnEnable()
    {
        all.Add(this);
    }

    void OnDisable()
    {
        all.Remove(this);
    }

    public void SetSelected(bool value)
    {
        selectionRing.SetSelected(value);
    }

    public void SetMoveTarget(Vector3 target, LowPolyPlanet planet)
    {
        moveTarget = target;
        flatMove = planet == null;
        hasMoveTarget = true;
    }

    public void StopMoving()
    {
        hasMoveTarget = false;
    }

    void Update()
    {
        if (hasMoveTarget)
        {
            transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed * Time.deltaTime);

            if (flatMove)
            {
                Vector3 flat = transform.position;
                flat.y = 0f;
                transform.position = flat;
            }

            if (Vector3.Distance(transform.position, moveTarget) < 0.25f)
                hasMoveTarget = false;
        }

        ApplyOrientation();
    }

    void ApplyOrientation()
    {
        Quaternion modelOffset = Quaternion.Euler(-90f, 0f, 0f);

        if (MovementTargeting.IsNearPlanet(transform.position, out LowPolyPlanet planet))
        {
            Vector3 up = (transform.position - planet.transform.position).normalized;
            Vector3 forward = hasMoveTarget
                ? moveTarget - transform.position
                : transform.forward;

            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(transform.forward, up);

            if (forward.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(forward.normalized, up) * modelOffset;
            return;
        }

        if (hasMoveTarget)
        {
            Vector3 forward = moveTarget - transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up) * modelOffset;
        }
    }
}
