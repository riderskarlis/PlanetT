using UnityEngine;

public class SunLightDriver : MonoBehaviour
{
    public Transform sun;

    void Update()
    {
        Vector3 dirToSun = (sun.position - transform.position).normalized;

        GetComponent<Renderer>().material.SetVector("_SunDirection", new Vector4(dirToSun.x, dirToSun.y, dirToSun.z, 0f));
    }
}