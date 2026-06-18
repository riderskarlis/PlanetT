using System.Collections.Generic;
using UnityEngine;

public class SelectionSystem : MonoBehaviour
{
    public Color lassoFill = new Color(0.3f, 0.6f, 1f, 0.15f);
    public Color lassoBorder = new Color(0.3f, 0.6f, 1f, 0.9f);

    const float DragThreshold = 6f;

    readonly HashSet<ISelectable> selected = new HashSet<ISelectable>();

    Vector2 dragStart;
    bool dragging;
    bool lassoActive;
    Texture2D whiteTex;

    public IReadOnlyCollection<ISelectable> Selected => selected;

    void Awake()
    {
        whiteTex = Texture2D.whiteTexture;
    }

    void Update()
    {
        HandleSelectionInput();
        HandleMoveInput();
    }

    void HandleSelectionInput()
    {
        if (Input.GetMouseButtonDown(0) && SimpleCameraController.IsCtrlHeld)
        {
            dragStart = Input.mousePosition;
            dragging = true;
            lassoActive = false;
            SimpleCameraController.BlockLook = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (dragging && Input.GetMouseButton(0))
        {
            if (!lassoActive && Vector2.Distance(dragStart, Input.mousePosition) >= DragThreshold)
                lassoActive = true;
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (lassoActive)
                LassoSelect(dragStart, (Vector2)Input.mousePosition, shift);
            else
                ClickSelect((Vector2)Input.mousePosition, shift);

            dragging = false;
            lassoActive = false;
            SimpleCameraController.BlockLook = false;

            if (!SimpleCameraController.IsCtrlHeld)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void HandleMoveInput()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        // Collect all selected ships
        List<SpaceshipController> selectedShips = new List<SpaceshipController>();
        foreach (var s in selected)
        {
            if (s is SpaceshipController ship)
                selectedShips.Add(ship);
        }

        if (selectedShips.Count == 0)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!MovementTargeting.TryGetMoveTarget(ray, out Vector3 target, out LowPolyPlanet planet))
            return;

        foreach (SpaceshipController ship in selectedShips)
            ship.SetMoveTarget(target, planet);
    }

    void ClickSelect(Vector2 screenPos, bool shift)
    {
        ISelectable hit = RaycastSelectable(screenPos);

        if (hit != null)
        {
            if (shift)
                ToggleSelectable(hit);
            else
                SetSingleSelection(hit);
            return;
        }

        if (!shift)
            ClearSelection();
    }

    void LassoSelect(Vector2 start, Vector2 end, bool shift)
    {
        Rect rect = ScreenRect(start, end);
        if (!shift)
            ClearSelection();

        // Check Ships
        foreach (SpaceshipController ship in SpaceshipController.All)
        {
            if (IsInLasso(ship.transform.position, rect))
            {
                if (shift) ToggleSelectable(ship);
                else AddSelectable(ship);
            }
        }

        // Check Planets
        LowPolyPlanet[] planets = Object.FindObjectsOfType<LowPolyPlanet>();
        foreach (LowPolyPlanet planet in planets)
        {
            if (IsInLasso(planet.transform.position, rect))
            {
                if (shift) ToggleSelectable(planet);
                else AddSelectable(planet);
            }
        }
    }

    bool IsInLasso(Vector3 worldPos, Rect rect)
    {
        Vector3 screen = Camera.main.WorldToScreenPoint(worldPos);
        if (screen.z < 0f) return false;
        return rect.Contains(new Vector2(screen.x, screen.y));
    }

    ISelectable RaycastSelectable(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return null;

        return hit.collider.GetComponentInParent<ISelectable>();
    }

    void SetSingleSelection(ISelectable item)
    {
        ClearSelection();
        AddSelectable(item);
    }

    void ToggleSelectable(ISelectable item)
    {
        if (selected.Contains(item))
            RemoveSelectable(item);
        else
            AddSelectable(item);
    }

    void AddSelectable(ISelectable item)
    {
        if (selected.Add(item))
            item.SetSelected(true);
    }

    void RemoveSelectable(ISelectable item)
    {
        if (selected.Remove(item))
            item.SetSelected(false);
    }

    public void ClearSelection()
    {
        foreach (ISelectable item in selected)
            item.SetSelected(false);
        selected.Clear();
    }

    static Rect ScreenRect(Vector2 a, Vector2 b)
    {
        float xMin = Mathf.Min(a.x, b.x);
        float xMax = Mathf.Max(a.x, b.x);
        float yMin = Mathf.Min(a.y, b.y);
        float yMax = Mathf.Max(a.y, b.y);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    void OnGUI()
    {
        if (!dragging || !lassoActive)
            return;

        Rect rect = ScreenRect(dragStart, Input.mousePosition);
        rect.y = Screen.height - rect.y - rect.height;

        Color prev = GUI.color;
        GUI.color = lassoFill;
        GUI.DrawTexture(rect, whiteTex);
        GUI.color = lassoBorder;
        DrawRectBorder(rect, 2f);
        GUI.color = prev;
    }

    static void DrawRectBorder(Rect rect, float thickness)
    {
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
    }
}
