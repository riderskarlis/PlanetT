using UnityEngine;

public static class MovementTargeting
{
    static readonly Plane yZeroPlane = new Plane(Vector3.up, Vector3.zero);

    public static bool TryGetMoveTarget(Ray ray, out Vector3 target, out LowPolyPlanet planet)
    {
        planet = null;
        target = Vector3.zero;

        // 1. Check for planet hit
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            planet = hit.collider.GetComponentInParent<LowPolyPlanet>();
            if (planet != null)
            {
                target = hit.point; // Target the exact surface point
                return true;
            }
        }

        // 2. Check for ground plane hit
        if (yZeroPlane.Raycast(ray, out float dist))
        {
            target = ray.GetPoint(dist);
            
            // Check if we clicked near a planet on the plane
            planet = GetNearestPlanet(target);
            if (planet != null)
            {
                float distToPlanet = Vector3.Distance(target, planet.transform.position);
                if (distToPlanet < planet.radius + 60f)
                {
                    // Snap to planet surface if close enough
                    Vector3 dir = (target - planet.transform.position).normalized;
                    target = planet.transform.position + dir * planet.radius;
                }
                else
                {
                    planet = null; // Too far, treat as space move
                }
            }
            return true;
        }

        return false;
    }

    public static LowPolyPlanet GetNearestPlanet(Vector3 point)
    {
        LowPolyPlanet[] planets = Object.FindObjectsOfType<LowPolyPlanet>();
        LowPolyPlanet best = null;
        float bestDist = float.MaxValue;

        foreach (var p in planets)
        {
            float dist = Vector3.Distance(point, p.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = p;
            }
        }
        return best;
    }
}
