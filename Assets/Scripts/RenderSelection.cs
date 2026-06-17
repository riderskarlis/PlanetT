using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RenderSelection : MonoBehaviour
{
    static readonly List<RenderSelection> all = new List<RenderSelection>();
    static RenderSelection hovered;

    public float radiusPadding = 1.15f;
    public int segments = 48;
    public Color color = new Color(1f, 0.85f, 0.2f);
    public Color selectedColor = new Color(0.2f, 0.9f, 1f);
    public Color hoverColor = Color.white;

    LineRenderer line;
    Vector3 center;
    float radius;
    bool selected;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.enabled = false;
        ApplyColor();
    }

    void OnEnable()
    {
        all.Add(this);
    }

    void OnDisable()
    {
        all.Remove(this);
        if (hovered == this)
            hovered = null;
    }

    void LateUpdate()
    {
        if (!line.enabled)
            return;

        RefreshCircle();
    }

    void RefreshCircle()
    {
        Renderer r = GetComponentInChildren<Renderer>();
        center = r != null ? r.bounds.center : transform.position;
        float y = r != null ? r.bounds.min.y : center.y;
        radius = r != null ? Mathf.Max(r.bounds.extents.x, r.bounds.extents.z) * radiusPadding : 2f;

        line.widthMultiplier = radius * 0.04f;
        line.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius, y, center.z + Mathf.Sin(a) * radius));
        }
    }

    bool ContainsPoint(Vector3 point)
    {
        float dx = point.x - center.x;
        float dz = point.z - center.z;
        return dx * dx + dz * dz <= radius * radius;
    }

    void ApplyColor()
    {
        Color c = selected ? selectedColor : color;
        if (hovered == this)
            c = hoverColor;

        line.startColor = c;
        line.endColor = c;
    }

    public void SetSelected(bool value)
    {
        selected = value;
        line.enabled = selected;
        if (selected)
            RefreshCircle();
        ApplyColor();
    }

    void SetHovered()
    {
        ApplyColor();
    }

    public static void SetHoverPoint(Vector3 point, bool active)
    {
        RenderSelection next = null;
        if (active)
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].line.enabled && all[i].ContainsPoint(point))
                    next = all[i];
            }
        }

        if (hovered == next)
            return;

        if (hovered != null)
            hovered.SetHovered();
        hovered = next;
        if (hovered != null)
            hovered.SetHovered();
    }
}
