using UnityEngine;

public class SunLightDriver : MonoBehaviour
{
    public Transform sun;

    void Start()
    {
        if (sun == null)
        {
            GameObject sunGo = GameObject.Find("Sun");
            if (sunGo != null)
            {
                sun = sunGo.transform;
            }
        }
    }

    void Update()
    {
        if (sun == null) return;

        Vector3 dirToSun = (sun.position - transform.position).normalized;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null && rend.material != null)
        {
            rend.material.SetVector("_SunDirection", new Vector4(dirToSun.x, dirToSun.y, dirToSun.z, 0f));
        }
    }
}