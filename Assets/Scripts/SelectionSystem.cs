using System.Collections.Generic;
using UnityEngine;

public class SelectionSystem : MonoBehaviour
{
    public Color lassoFill = new Color(0.3f, 0.6f, 1f, 0.15f);
    public Color lassoBorder = new Color(0.3f, 0.6f, 1f, 0.9f);

    const float DragThreshold = 6f;

    readonly HashSet<SpaceshipController> selected = new HashSet<SpaceshipController>();

    Vector2 dragStart;
    bool dragging;
    bool lassoActive;
    Texture2D whiteTex;

    public IReadOnlyCollection<SpaceshipController> Selected => selected;

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
        if (!Input.GetMouseButtonDown(1) || selected.Count == 0)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!MovementTargeting.TryGetMoveTarget(ray, out Vector3 target, out LowPolyPlanet planet))
            return;

        foreach (SpaceshipController ship in selected)
            ship.SetMoveTarget(target, planet);
    }

    void ClickSelect(Vector2 screenPos, bool shift)
    {
        SpaceshipController hitShip = RaycastShip(screenPos);

        if (hitShip != null)
        {
            if (shift)
                ToggleShip(hitShip);
            else
                SetSingleSelection(hitShip);
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

        foreach (SpaceshipController ship in SpaceshipController.All)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(ship.transform.position);
            if (screen.z < 0f)
                continue;

            if (!rect.Contains(new Vector2(screen.x, screen.y)))
                continue;

            if (shift)
            {
                if (!selected.Contains(ship))
                    AddShip(ship);
            }
            else
            {
                AddShip(ship);
            }
        }
    }

    SpaceshipController RaycastShip(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return null;

        return hit.collider.GetComponentInParent<SpaceshipController>();
    }

    void SetSingleSelection(SpaceshipController ship)
    {
        ClearSelection();
        AddShip(ship);
    }

    void ToggleShip(SpaceshipController ship)
    {
        if (selected.Contains(ship))
            RemoveShip(ship);
        else
            AddShip(ship);
    }

    void AddShip(SpaceshipController ship)
    {
        if (selected.Add(ship))
            ship.SetSelected(true);
    }

    void RemoveShip(SpaceshipController ship)
    {
        if (selected.Remove(ship))
            ship.SetSelected(false);
    }

    public void ClearSelection()
    {
        foreach (SpaceshipController ship in selected)
            ship.SetSelected(false);
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
