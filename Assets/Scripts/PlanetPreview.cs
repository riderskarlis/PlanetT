using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlanetPreview : MonoBehaviour
{
    [Header("Dependencies")]
    public SelectionSystem selectionSystem;
    
    [Header("UI Render Targets")]
    [Tooltip("Highly recommended for performance (avoids CPU readbacks)")]
    public RawImage rawImage;
    [Tooltip("Fallback image if RawImage is not used (less performant)")]
    public Image uiImage;

    [Header("Preview Setup")]
    public int renderTextureWidth = 512;
    public int renderTextureHeight = 512;
    public Color backgroundColor = new Color(0.1f, 0.11f, 0.13f, 1f);
    public string previewLayerName = "";
    
    [Header("Camera & Lighting")]
    public Vector3 cameraAngle = new Vector3(20f, 45f, 0f);
    public float rotationSpeed = 25f;

    [Header("Wireframe Settings")]
    public bool showWireframe = true;
    public Color wireframeColor = new Color(0f, 1f, 1f, 0.4f);

    [Header("Dynamic Padding")]
    [Tooltip("Padding used for small objects (e.g. spaceships)")]
    public float smallObjectPadding = 1.6f;
    [Tooltip("Padding used for large objects (e.g. planets)")]
    public float largeObjectPadding = 1.15f;
    [Tooltip("Object bounds magnitude at or below which smallObjectPadding is applied")]
    public float minSizeThreshold = 5f;
    [Tooltip("Object bounds magnitude at or above which largeObjectPadding is applied")]
    public float maxSizeThreshold = 120f;

    private RenderTexture renderTexture;
    private Texture2D previewTexture2D;
    private Sprite previewSprite;
    
    private Transform previewAnchor;
    private Camera previewCamera;
    private Camera wireframeCamera;
    private Light keyLight;
    private Light fillLight;
    
    private GameObject activeClone;
    private ISelectable lastSelectedItem;

    void Start()
    {
        if (selectionSystem == null)
        {
            selectionSystem = FindObjectOfType<SelectionSystem>();
        }

        // Initialize preview scene off-screen
        InitializePreviewScene();
        
        // Hide preview initially
        UpdatePreview(null);
    }

    void InitializePreviewScene()
    {
        // Create an isolated anchor far away in space
        GameObject anchorObj = new GameObject("PlanetPreview_Anchor");
        anchorObj.transform.position = new Vector3(8888f, 8888f, 8888f);
        previewAnchor = anchorObj.transform;

        // Create RenderTexture
        renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 4;
        renderTexture.Create();

        if (rawImage != null)
        {
            rawImage.texture = renderTexture;
        }

        // Setup camera
        GameObject camObj = new GameObject("PreviewCamera");
        camObj.transform.SetParent(previewAnchor);
        camObj.transform.localPosition = new Vector3(0f, 0f, -10f); // Default position, adjusted dynamically
        camObj.transform.localRotation = Quaternion.Euler(cameraAngle);
        
        previewCamera = camObj.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = backgroundColor;
        previewCamera.fieldOfView = 35f;
        previewCamera.targetTexture = renderTexture;

        // Setup overlay wireframe camera
        GameObject wireframeCamObj = new GameObject("WireframeCamera");
        wireframeCamObj.transform.SetParent(camObj.transform, false);
        
        wireframeCamera = wireframeCamObj.AddComponent<Camera>();
        wireframeCamera.clearFlags = CameraClearFlags.Nothing;
        wireframeCamera.depth = previewCamera.depth + 1;
        wireframeCamera.targetTexture = renderTexture;
        
        WireframeCameraHelper wireframeHelper = wireframeCamObj.AddComponent<WireframeCameraHelper>();
        wireframeHelper.parent = this;

        Shader overlayShader = Shader.Find("Custom/WireframeOverlay");
        if (overlayShader != null)
        {
            wireframeCamera.SetReplacementShader(overlayShader, "");
        }

        // Setup lighting
        GameObject lightObj = new GameObject("PreviewKeyLight");
        lightObj.transform.SetParent(camObj.transform);
        lightObj.transform.localPosition = new Vector3(4f, 4f, -2f);
        lightObj.transform.localRotation = Quaternion.identity;
        
        keyLight = lightObj.AddComponent<Light>();
        keyLight.type = LightType.Point;
        keyLight.range = 100f;
        keyLight.intensity = 2f;
        keyLight.color = Color.white;

        GameObject fillObj = new GameObject("PreviewFillLight");
        fillObj.transform.SetParent(camObj.transform);
        fillObj.transform.localPosition = new Vector3(-4f, -2f, -2f);
        fillObj.transform.localRotation = Quaternion.identity;
        
        fillLight = fillObj.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.range = 100f;
        fillLight.intensity = 0.8f;
        fillLight.color = new Color(0.8f, 0.85f, 1f);

        // Apply layer settings if specified
        if (!string.IsNullOrEmpty(previewLayerName))
        {
            int layer = LayerMask.NameToLayer(previewLayerName);
            if (layer != -1)
            {
                previewCamera.cullingMask = 1 << layer;
                wireframeCamera.cullingMask = 1 << layer;
                keyLight.cullingMask = 1 << layer;
                fillLight.cullingMask = 1 << layer;
            }
        }
    }

    void Update()
    {
        if (selectionSystem == null) return;

        var selected = selectionSystem.Selected;
        ISelectable currentItem = selected.Count == 1 ? selected.First() : null;

        if (currentItem != lastSelectedItem)
        {
            UpdatePreview(currentItem);
            lastSelectedItem = currentItem;
        }

        // Rotate preview object if it exists
        if (previewAnchor != null && activeClone != null)
        {
            previewAnchor.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }

        // Render target fallback copy if using standard Image component (only if RawImage is not used)
        if (rawImage == null && activeClone != null && uiImage != null && renderTexture != null)
        {
            UpdateUIImageFallback();
        }

        SyncWireframeCamera();
    }

    void UpdatePreview(ISelectable item)
    {
        // Reset anchor rotation to keep consistent starting orientation
        if (previewAnchor != null)
        {
            previewAnchor.rotation = Quaternion.identity;
        }

        // Clean up old preview object
        if (activeClone != null)
        {
            Destroy(activeClone);
            activeClone = null;
        }

        if (item == null)
        {
            if (previewCamera != null) previewCamera.enabled = false;
            if (wireframeCamera != null) wireframeCamera.enabled = false;
            if (rawImage != null) rawImage.gameObject.SetActive(false);
            if (uiImage != null) uiImage.gameObject.SetActive(false);
            return;
        }

        GameObject original = item.transform.gameObject;
        if (original == null) return;

        // Activate UI & Camera
        if (previewCamera != null) previewCamera.enabled = true;
        if (rawImage != null)
        {
            rawImage.gameObject.SetActive(true);
            if (uiImage != null) uiImage.gameObject.SetActive(false);
        }
        else if (uiImage != null)
        {
            uiImage.gameObject.SetActive(true);
        }

        // Create visual-only clone directly instead of instantiating and purging all behaviors
        activeClone = CreateVisualClone(original);
        activeClone.transform.SetParent(previewAnchor, false);

        // Setup layers if configured
        if (!string.IsNullOrEmpty(previewLayerName))
        {
            int layer = LayerMask.NameToLayer(previewLayerName);
            if (layer != -1)
            {
                SetLayerRecursively(activeClone, layer);
            }
        }

        activeClone.SetActive(true);

        // Calculate visual bounds to center and scale camera view
        Bounds bounds = new Bounds(activeClone.transform.position, Vector3.zero);
        Renderer[] renderers = activeClone.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        foreach (Renderer r in renderers)
        {
            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (hasBounds)
        {
            // Center the clone visual center on the anchor
            Vector3 offset = previewAnchor.position - bounds.center;
            activeClone.transform.position += offset;

            float size = bounds.size.magnitude;
            float fov = previewCamera.fieldOfView;
            float distance = (size / 2f) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

            // Dynamic padding based on size (small objects get more breathing room, large objects fit tight)
            float t = Mathf.InverseLerp(minSizeThreshold, maxSizeThreshold, size);
            float dynamicPadding = Mathf.Lerp(smallObjectPadding, largeObjectPadding, t);
            distance *= dynamicPadding;

            // Positioning camera backward along local forward vector
            previewCamera.transform.position = previewAnchor.position - previewCamera.transform.forward * distance;

            // Adjust lights range based on distance
            if (keyLight != null) keyLight.range = distance * 3f;
            if (fillLight != null) fillLight.range = distance * 3f;
        }

        SyncWireframeCamera();
    }

    private GameObject CreateVisualClone(GameObject original)
    {
        GameObject cloneRoot = new GameObject(original.name + "_Preview");
        
        cloneRoot.transform.localPosition = original.transform.localPosition;
        cloneRoot.transform.localRotation = original.transform.localRotation;
        cloneRoot.transform.localScale = original.transform.localScale;

        CopyVisualsRecursively(original.transform, cloneRoot.transform);

        return cloneRoot;
    }

    private void CopyVisualsRecursively(Transform source, Transform target)
    {
        // Copy MeshFilter / MeshRenderer if they exist
        MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        if (sourceFilter != null && sourceRenderer != null)
        {
            MeshFilter targetFilter = target.gameObject.AddComponent<MeshFilter>();
            targetFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer targetRenderer = target.gameObject.AddComponent<MeshRenderer>();
            CopyRendererProperties(sourceRenderer, targetRenderer);
        }

        // Copy SkinnedMeshRenderer if it exists
        SkinnedMeshRenderer sourceSkinned = source.GetComponent<SkinnedMeshRenderer>();
        if (sourceSkinned != null)
        {
            SkinnedMeshRenderer targetSkinned = target.gameObject.AddComponent<SkinnedMeshRenderer>();
            targetSkinned.sharedMesh = sourceSkinned.sharedMesh;
            CopyRendererProperties(sourceSkinned, targetSkinned);
        }

        // Recursively process children skip helper components (Selection rings, lines)
        foreach (Transform child in source)
        {
            if (child.name == "OrbitLine" || child.GetComponent<PlanetOrbit>() != null || child.name == "SelectionRing")
            {
                continue;
            }

            GameObject childClone = new GameObject(child.name);
            childClone.transform.SetParent(target, false);
            childClone.transform.localPosition = child.localPosition;
            childClone.transform.localRotation = child.localRotation;
            childClone.transform.localScale = child.localScale;

            CopyVisualsRecursively(child, childClone.transform);
        }
    }

    private void CopyRendererProperties(Renderer source, Renderer target)
    {
        target.sharedMaterials = source.sharedMaterials;
        
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        source.GetPropertyBlock(block);
        target.SetPropertyBlock(block);
    }

    void UpdateUIImageFallback()
    {
        if (previewTexture2D == null || previewTexture2D.width != renderTexture.width || previewTexture2D.height != renderTexture.height)
        {
            previewTexture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        }

        RenderTexture currentActive = RenderTexture.active;
        RenderTexture.active = renderTexture;
        previewTexture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        previewTexture2D.Apply();
        RenderTexture.active = currentActive;

        if (uiImage.sprite == null || uiImage.sprite.texture != previewTexture2D)
        {
            if (previewSprite != null)
            {
                Destroy(previewSprite);
            }
            previewSprite = Sprite.Create(previewTexture2D, new Rect(0, 0, previewTexture2D.width, previewTexture2D.height), new Vector2(0.5f, 0.5f));
            uiImage.sprite = previewSprite;
        }
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (previewTexture2D != null)
        {
            Destroy(previewTexture2D);
        }
        if (previewSprite != null)
        {
            Destroy(previewSprite);
        }
        if (previewAnchor != null)
        {
            Destroy(previewAnchor.gameObject);
        }
    }

    void SyncWireframeCamera()
    {
        if (wireframeCamera != null && previewCamera != null)
        {
            wireframeCamera.fieldOfView = previewCamera.fieldOfView;
            wireframeCamera.nearClipPlane = previewCamera.nearClipPlane;
            wireframeCamera.farClipPlane = previewCamera.farClipPlane;
            wireframeCamera.orthographic = previewCamera.orthographic;
            wireframeCamera.orthographicSize = previewCamera.orthographicSize;
            wireframeCamera.cullingMask = previewCamera.cullingMask;
            wireframeCamera.enabled = previewCamera.enabled && showWireframe;
        }
    }
}

public class WireframeCameraHelper : MonoBehaviour
{
    [HideInInspector]
    public PlanetPreview parent;

    void OnPreRender()
    {
        GL.wireframe = true;
        if (parent != null)
        {
            Shader.SetGlobalColor("_WireframeColor", parent.wireframeColor);
        }
    }

    void OnPostRender()
    {
        GL.wireframe = false;
    }
}
