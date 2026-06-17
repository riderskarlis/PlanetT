using UnityEngine;

public static class MovementTargeting
{
    static readonly Plane yZeroPlane = new Plane(Vector3.up, Vector3.zero);

    public static bool TryGetMoveTarget(Ray ray, out Vector3 target, out LowPolyPlanet planet)
    {
        planet = null;
        target = Vector3.zero;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            planet = hit.collider.GetComponentInParent<LowPolyPlanet>();
            if (planet != null)
            {
                Vector3 center = planet.transform.position;
                Vector3 outward = (hit.point - center).normalized;
                target = hit.point + outward * 50f;
                return true;
            }

            SpaceshipController ship = hit.collider.GetComponentInParent<SpaceshipController>();
            if (ship != null)
            {
                if (!yZeroPlane.Raycast(ray, out float dist))
                    return false;

                target = ray.GetPoint(dist);
                target.y = 0f;
                return TryPlanetOverride(ref target, ref planet);
            }
        }

        if (!yZeroPlane.Raycast(ray, out float groundDist))
            return false;

        target = ray.GetPoint(groundDist);
        target.y = 0f;
        TryPlanetOverride(ref target, ref planet);
        return true;
    }

    static bool TryPlanetOverride(ref Vector3 target, ref LowPolyPlanet planet)
    {
        planet = GetNearestPlanet(target);
        if (planet == null)
            return false;

        float influence = planet.radius + 60f;
        if (Vector3.Distance(target, planet.transform.position) >= influence)
        {
            planet = null;
            return false;
        }

        Vector3 center = planet.transform.position;
        Vector3 outward = (target - center).normalized;
        if (outward.sqrMagnitude < 0.0001f)
            outward = Vector3.up;

        Ray surfaceRay = new Ray(center, outward);
        if (Physics.Raycast(surfaceRay, out RaycastHit surfHit, planet.radius * 4f))
        {
            LowPolyPlanet hitPlanet = surfHit.collider.GetComponentInParent<LowPolyPlanet>();
            if (hitPlanet == planet)
            {
                outward = (surfHit.point - center).normalized;
                target = surfHit.point + outward * 50f;
                return true;
            }
        }

        target = center + outward * (planet.radius + 50f);
        return true;
    }

    public static LowPolyPlanet GetNearestPlanet(Vector3 point)
    {
        LowPolyPlanet[] planets = Object.FindObjectsOfType<LowPolyPlanet>();
        LowPolyPlanet best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < planets.Length; i++)
        {
            float dist = Vector3.Distance(point, planets[i].transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = planets[i];
            }
        }

        return best;
    }

    public static bool IsNearPlanet(Vector3 point, out LowPolyPlanet planet, float extraRange = 60f)
    {
        planet = GetNearestPlanet(point);
        if (planet == null)
            return false;

        return Vector3.Distance(point, planet.transform.position) < planet.radius + extraRange;
    }
}
