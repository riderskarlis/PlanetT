using System.Collections.Generic;
using UnityEngine;

public class SelectionSystem : MonoBehaviour
{
    public Color lassoFill = new Color(0.3f, 0.6f, 1f, 0.15f);
    public Color lassoBorder = new Color(0.3f, 0.6f, 1f, 0.9f);

    [Header("Audio Settings")]
    public AudioClip selectionClickSound;
    public AudioClip moveCommandSound;

    const float DragThreshold = 6f;

    readonly HashSet<ISelectable> selected = new HashSet<ISelectable>();

    Vector2 dragStart;
    bool dragging;
    bool lassoActive;
    Texture2D whiteTex;

    private SimpleCameraController freeCam;
    private OrbitalCameraController orbitCam;

    private bool drawingFormation = false;
    private readonly List<Vector3> formationPoints = new List<Vector3>();
    private LineRenderer formationLine;

    public IReadOnlyCollection<ISelectable> Selected => selected;

    void Awake()
    {
        whiteTex = Texture2D.whiteTexture;

        // Retrieve player's selected color from game setup
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
            Color chosenColor = colors[index];
            lassoFill = new Color(chosenColor.r, chosenColor.g, chosenColor.b, 0.15f);
            lassoBorder = new Color(chosenColor.r, chosenColor.g, chosenColor.b, 0.9f);
        }
    }

    void Update()
    {
        // Prune any destroyed selectables (e.g. ships destroyed in battle) from selection list
        selected.RemoveWhere(item => item == null || (item as MonoBehaviour) == null);

        HandleSelectionInput();
        HandleMoveInput();
        HandleSelectionShortcuts();
        HandleCameraJumpInput();
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
            {
                LassoSelect(dragStart, (Vector2)Input.mousePosition, shift);
                if (Selected.Count > 0 && SoundManager.Instance != null && selectionClickSound != null)
                {
                    SoundManager.Instance.PlaySFX(selectionClickSound);
                }
            }
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
        if (SimpleCameraController.IsCtrlHeld || drawingFormation)
        {
            HandleFormationDrawing();
            return;
        }

        if (!Input.GetMouseButtonDown(1))
            return;

        // Collect all selected player-owned ships (filter out enemy ships)
        List<SpaceshipController> selectedShips = new List<SpaceshipController>();
        foreach (var s in selected)
        {
            if (s is SpaceshipController ship && !ship.isEnemy)
                selectedShips.Add(ship);
        }

        if (selectedShips.Count == 0)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check if we right-clicked an enemy spaceship to attack it
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            SpaceshipController targetShip = hit.collider.GetComponentInParent<SpaceshipController>();
            if (targetShip != null && targetShip.isEnemy)
            {
                // Command all selected player ships to attack the target
                foreach (SpaceshipController ship in selectedShips)
                {
                    if (!ship.isEnemy)
                    {
                        ship.SetAttackTarget(targetShip);
                    }
                }

                // Play combat order sound
                if (SoundManager.Instance != null && moveCommandSound != null)
                {
                    SoundManager.Instance.PlaySFX(moveCommandSound);
                }
                return;
            }
        }

        // Standard movement logic otherwise
        if (!MovementTargeting.TryGetMoveTarget(ray, out Vector3 target, out LowPolyPlanet planet))
            return;

        // Play move command sound
        if (SoundManager.Instance != null && moveCommandSound != null)
        {
            SoundManager.Instance.PlaySFX(moveCommandSound);
        }

        int shipCount = selectedShips.Count;
        for (int i = 0; i < shipCount; i++)
        {
            SpaceshipController ship = selectedShips[i];
            ship.SetAttackTarget(null); // Clear any ongoing attack target
            ship.isPlayerCommandedMove = true; // Set player commanded move flag

            // Calculate circular formation offset so ships spread out instead of overlapping
            Vector3 formationOffset = Vector3.zero;
            if (shipCount > 1)
            {
                float angle = i * (2f * Mathf.PI / shipCount);
                float radius = 4f + Mathf.Sqrt(shipCount) * 1.5f; // Scale radius with ship density
                formationOffset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }

            ship.SetMoveTarget(target, planet, formationOffset);
        }
    }

    void ClickSelect(Vector2 screenPos, bool shift)
    {
        ISelectable hit = RaycastSelectable(screenPos);

        if (hit != null)
        {
            // Play click select sound
            if (SoundManager.Instance != null && selectionClickSound != null)
            {
                SoundManager.Instance.PlaySFX(selectionClickSound);
            }

            if (shift)
                ToggleSelectable(hit);
            else
                SetSingleSelection(hit);
            return;
        }

        if (!shift)
        {
            // Play click sound on deselect too
            if (Selected.Count > 0 && SoundManager.Instance != null && selectionClickSound != null)
            {
                SoundManager.Instance.PlaySFX(selectionClickSound);
            }
            ClearSelection();
        }
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
        {
            if (item != null && (item as MonoBehaviour) != null)
            {
                item.SetSelected(false);
            }
        }
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

    void HandleSelectionShortcuts()
    {
        // Z: Split selection in half
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (selected.Count > 1)
            {
                List<ISelectable> list = new List<ISelectable>(selected);
                int halfCount = list.Count / 2;
                for (int i = halfCount; i < list.Count; i++)
                {
                    RemoveSelectable(list[i]);
                }
                
                if (SoundManager.Instance != null && selectionClickSound != null)
                {
                    SoundManager.Instance.PlaySFX(selectionClickSound);
                }
                Debug.Log($"[SelectionSystem] Split selection: kept {halfCount} ships.");
            }
        }

        // X: Select all player ships
        if (Input.GetKeyDown(KeyCode.X))
        {
            ClearSelection();
            foreach (var ship in SpaceshipController.All)
            {
                if (ship != null && !ship.isEnemy)
                {
                    AddSelectable(ship);
                }
            }
            if (selected.Count > 0 && SoundManager.Instance != null && selectionClickSound != null)
            {
                SoundManager.Instance.PlaySFX(selectionClickSound);
            }
            Debug.Log($"[SelectionSystem] Selected all player ships ({selected.Count} ships).");
        }

        // C: Clear selection
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (selected.Count > 0)
            {
                ClearSelection();
                if (SoundManager.Instance != null && selectionClickSound != null)
                {
                    SoundManager.Instance.PlaySFX(selectionClickSound);
                }
                Debug.Log("[SelectionSystem] Selection cleared.");
            }
        }

        // H: Stop selected ships
        if (Input.GetKeyDown(KeyCode.H))
        {
            int stopCount = 0;
            foreach (var s in selected)
            {
                if (s is SpaceshipController ship && !ship.isEnemy)
                {
                    ship.StopMoving();
                    ship.SetAttackTarget(null);
                    ship.isPlayerCommandedMove = false;
                    stopCount++;
                }
            }
            if (stopCount > 0 && SoundManager.Instance != null && moveCommandSound != null)
            {
                SoundManager.Instance.PlaySFX(moveCommandSound);
            }
            Debug.Log($"[SelectionSystem] Stopped {stopCount} selected player ships.");
        }
    }

    void HandleCameraJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            JumpToSelected();
        }
    }

    void JumpToSelected()
    {
        if (selected.Count == 0) return;

        ISelectable firstSel = null;
        foreach (var item in selected)
        {
            if (item != null && (item as MonoBehaviour) != null)
            {
                firstSel = item;
                break;
            }
        }

        if (firstSel == null) return;

        Transform targetT = firstSel.transform;
        
        if (freeCam == null) freeCam = FindObjectOfType<SimpleCameraController>();
        if (orbitCam == null) orbitCam = FindObjectOfType<OrbitalCameraController>();

        Transform camT = null;
        if (freeCam != null) camT = freeCam.transform;
        else if (orbitCam != null) camT = orbitCam.transform;
        else
        {
            Camera cam = Camera.main;
            if (cam != null) camT = cam.transform;
        }

        if (camT == null) return;

        // 1. Detach camera parent if orbiting and ensure free look camera is active
        if (orbitCam != null && orbitCam.enabled)
        {
            camT.SetParent(null);
            orbitCam.enabled = false;
        }

        if (freeCam != null)
        {
            freeCam.enabled = true;
        }

        // 2. Determine positioning variables
        float offsetDistance = 20f;
        Vector3 targetDirection;

        LowPolyPlanet planet = firstSel as LowPolyPlanet;
        if (planet != null)
        {
            // Position camera close to planet boundary (inside the atmosphere trigger to trigger orbit naturally)
            offsetDistance = planet.radius + 40f; 
            targetDirection = camT.position - targetT.position;
            if (targetDirection.sqrMagnitude > 0.001f)
            {
                targetDirection.Normalize();
            }
            else
            {
                targetDirection = Vector3.up;
            }
        }
        else
        {
            // Position camera slightly behind and above the ship
            targetDirection = -targetT.forward * 0.8f + targetT.up * 0.4f;
            if (targetDirection.sqrMagnitude > 0.001f)
            {
                targetDirection.Normalize();
            }
            else
            {
                targetDirection = Vector3.up;
            }
        }

        // 3. Move and rotate camera
        camT.position = targetT.position + targetDirection * offsetDistance;
        camT.LookAt(targetT.position);

        // 4. Sync free look camera variables
        if (freeCam != null && freeCam.enabled)
        {
            freeCam.SyncRotationFromTransform();
        }

        Debug.Log($"[SelectionSystem] Camera physically moved close to: {firstSel.Name}");
    }

    void EnsureFormationLineRenderer()
    {
        if (formationLine == null)
        {
            GameObject obj = new GameObject("FormationDrawLine");
            obj.transform.SetParent(transform, false);
            formationLine = obj.AddComponent<LineRenderer>();
            formationLine.startWidth = 0.35f;
            formationLine.endWidth = 0.35f;
            formationLine.useWorldSpace = true;
            
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            formationLine.material = new Material(shader);
            
            formationLine.startColor = lassoBorder;
            formationLine.endColor = lassoBorder;
            formationLine.positionCount = 0;
        }
    }

    bool GetDrawPoint(Ray ray, out Vector3 point)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            point = hit.point;
            return true;
        }

        Plane yZeroPlane = new Plane(Vector3.up, Vector3.zero);
        if (yZeroPlane.Raycast(ray, out float dist))
        {
            point = ray.GetPoint(dist);
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    LowPolyPlanet GetPlanetAtPosition(Vector3 pos)
    {
        LowPolyPlanet[] planets = Object.FindObjectsOfType<LowPolyPlanet>();
        foreach (var p in planets)
        {
            if (p != null && Vector3.Distance(pos, p.transform.position) < p.radius + 60f)
            {
                return p;
            }
        }
        return null;
    }

    List<Vector3> SamplePointsAlongPath(List<Vector3> path, int sampleCount)
    {
        List<Vector3> sampled = new List<Vector3>();
        if (path == null || path.Count == 0) return sampled;

        if (sampleCount == 1)
        {
            sampled.Add(path[path.Count - 1]);
            return sampled;
        }

        float[] cumulativeLength = new float[path.Count];
        cumulativeLength[0] = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            cumulativeLength[i] = cumulativeLength[i - 1] + Vector3.Distance(path[i], path[i - 1]);
        }
        float totalLength = cumulativeLength[path.Count - 1];

        for (int i = 0; i < sampleCount; i++)
        {
            float targetDist = (i / (float)(sampleCount - 1)) * totalLength;
            
            int segmentIdx = 0;
            while (segmentIdx < path.Count - 1 && cumulativeLength[segmentIdx + 1] < targetDist)
            {
                segmentIdx++;
            }

            if (segmentIdx >= path.Count - 1)
            {
                sampled.Add(path[path.Count - 1]);
            }
            else
            {
                float segStartDist = cumulativeLength[segmentIdx];
                float segEndDist = cumulativeLength[segmentIdx + 1];
                float segLength = segEndDist - segStartDist;
                
                float t = 0f;
                if (segLength > 0.0001f)
                {
                    t = (targetDist - segStartDist) / segLength;
                }
                
                sampled.Add(Vector3.Lerp(path[segmentIdx], path[segmentIdx + 1], t));
            }
        }

        return sampled;
    }

    void HandleFormationDrawing()
    {
        EnsureFormationLineRenderer();

        if (Input.GetMouseButtonDown(1))
        {
            bool hasPlayerShips = false;
            foreach (var s in selected)
            {
                if (s is SpaceshipController ship && !ship.isEnemy)
                {
                    hasPlayerShips = true;
                    break;
                }
            }

            if (hasPlayerShips)
            {
                drawingFormation = true;
                formationPoints.Clear();
                SimpleCameraController.BlockLook = true;
                
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (GetDrawPoint(ray, out Vector3 firstPoint))
                {
                    formationPoints.Add(firstPoint);
                    formationLine.positionCount = 1;
                    formationLine.SetPosition(0, firstPoint);
                }
            }
        }

        if (drawingFormation && Input.GetMouseButton(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (GetDrawPoint(ray, out Vector3 newPoint))
            {
                if (formationPoints.Count == 0)
                {
                    formationPoints.Add(newPoint);
                    formationLine.positionCount = 1;
                    formationLine.SetPosition(0, newPoint);
                }
                else
                {
                    float dist = Vector3.Distance(newPoint, formationPoints[formationPoints.Count - 1]);
                    if (dist > 1.5f)
                    {
                        formationPoints.Add(newPoint);
                        formationLine.positionCount = formationPoints.Count;
                        formationLine.SetPosition(formationPoints.Count - 1, newPoint);
                    }
                }
            }
        }

        if (drawingFormation && Input.GetMouseButtonUp(1))
        {
            drawingFormation = false;
            SimpleCameraController.BlockLook = false;
            
            if (formationLine != null)
            {
                formationLine.positionCount = 0;
            }

            List<SpaceshipController> selectedShips = new List<SpaceshipController>();
            foreach (var s in selected)
            {
                if (s is SpaceshipController ship && !ship.isEnemy)
                    selectedShips.Add(ship);
            }

            if (selectedShips.Count > 0 && formationPoints.Count >= 2)
            {
                float pathLength = 0f;
                for (int i = 1; i < formationPoints.Count; i++)
                {
                    pathLength += Vector3.Distance(formationPoints[i], formationPoints[i - 1]);
                }

                if (SoundManager.Instance != null && moveCommandSound != null)
                {
                    SoundManager.Instance.PlaySFX(moveCommandSound);
                }

                if (pathLength > 3.0f)
                {
                    List<Vector3> targets = SamplePointsAlongPath(formationPoints, selectedShips.Count);
                    for (int i = 0; i < selectedShips.Count; i++)
                    {
                        SpaceshipController ship = selectedShips[i];
                        ship.SetAttackTarget(null);
                        ship.isPlayerCommandedMove = true;

                        Vector3 targetPos = targets[i];
                        LowPolyPlanet targetPlanet = GetPlanetAtPosition(targetPos);
                        
                        ship.SetMoveTarget(targetPos, targetPlanet, Vector3.zero);
                    }
                    Debug.Log($"[SelectionSystem] Ordered {selectedShips.Count} ships to custom drawn formation shape.");
                }
                else
                {
                    Vector3 clickTarget = formationPoints[formationPoints.Count - 1];
                    LowPolyPlanet targetPlanet = GetPlanetAtPosition(clickTarget);

                    int shipCount = selectedShips.Count;
                    for (int i = 0; i < shipCount; i++)
                    {
                        SpaceshipController ship = selectedShips[i];
                        ship.SetAttackTarget(null);
                        ship.isPlayerCommandedMove = true;

                        Vector3 formationOffset = Vector3.zero;
                        if (shipCount > 1)
                        {
                            float angle = i * (2f * Mathf.PI / shipCount);
                            float radius = 4f + Mathf.Sqrt(shipCount) * 1.5f;
                            formationOffset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                        }

                        ship.SetMoveTarget(clickTarget, targetPlanet, formationOffset);
                    }
                }
            }
        }
    }
}
