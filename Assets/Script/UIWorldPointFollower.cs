using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIWorldPointFollower : MonoBehaviour
{
    [Tooltip("World transform to follow (e.g. a point on the player).")]
    public Transform worldTarget;

    [Tooltip("Canvas that contains this UI element.")]
    public Canvas canvas;

    [Tooltip("Optional world offset from the target.")]
    public Vector3 worldOffset = Vector3.zero;

    [Tooltip("Smooth follow (0 = instant).")]
    public float smoothTime = 0f;

    [Tooltip("If true, keeps the UI inside canvas rect bounds (useful for overlay/camera canvases).")]
    public bool clampToCanvas = true;

    private RectTransform rectTransform;
    private RectTransform canvasRect;
    // Separate velocity fields for Vector3 and Vector2 SmoothDamp calls to avoid type mismatch
    private Vector3 velocity3;
    private Vector2 velocity2;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }
    }

    void LateUpdate()
    {
        if (worldTarget == null || canvas == null) return;

        Vector3 worldPos = worldTarget.position + worldOffset;

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            // For World Space canvas, convert world position into canvas local space directly:
            Vector3 localPos = canvasRect.InverseTransformPoint(worldPos);
            if (smoothTime > 0f)
            {
                rectTransform.localPosition = Vector3.SmoothDamp(rectTransform.localPosition, localPos, ref velocity3, smoothTime);
            }
            else
            {
                rectTransform.localPosition = localPos;
            }
        }
        else
        {
            // Screen space (Overlay or Camera): convert world -> screen -> canvas local
            Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? Camera.main : (canvas.worldCamera != null ? canvas.worldCamera : Camera.main);
            // If we still don't have camera (rare), try Camera.main; if null, bail.
            if (cam == null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) return;

            // WorldToScreenPoint via RectTransformUtility guarantees correct result with given camera
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam, out localPoint);

            Vector2 targetAnchoredPos = localPoint;

            if (clampToCanvas)
            {
                Vector2 halfSize = canvasRect.rect.size * 0.5f;
                // clamp with a tiny padding to avoid exact edge problems
                targetAnchoredPos.x = Mathf.Clamp(targetAnchoredPos.x, -halfSize.x + 1, halfSize.x - 1);
                targetAnchoredPos.y = Mathf.Clamp(targetAnchoredPos.y, -halfSize.y + 1, halfSize.y - 1);
            }

            if (smoothTime > 0f)
            {
                rectTransform.anchoredPosition = Vector2.SmoothDamp(rectTransform.anchoredPosition, targetAnchoredPos, ref velocity2, smoothTime);
            }
            else
            {
                rectTransform.anchoredPosition = targetAnchoredPos;
            }
        }
    }
}
