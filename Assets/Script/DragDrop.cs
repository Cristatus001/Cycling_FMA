using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class DragDropUI : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // --- existing fields ---
    private Vector2 startPosition;
    private bool isDragging = false;
    private bool wasInsideDropArea = false;
    private bool isTap = false;

    public RectTransform DropArea;
    public float objectMass = 10f; // mass value (20 => must always be top)
    private bool isInsideDropArea = false;

    private ParallaxEffect parallaxEffect;

    private static List<GameObject> stackedObjects = new List<GameObject>();
    private static float stackOffset = 172f;
    private static float moveDuration = 0.3f;

    private RectTransform rectTransform;
    private Canvas canvas;

    public Sprite idleSprite;
    public Sprite burdenedSprite;
    private Image imageComponent;

    // minimal required field
    public Transform playerTransform;

    // optional overlay image (if you still use it)
    public Image overlayImage;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        startPosition = rectTransform.anchoredPosition;
        parallaxEffect = FindObjectOfType<ParallaxEffect>();
        imageComponent = GetComponent<Image>();

        SetBurdened(false);

        if (canvas == null) Debug.LogError("Canvas not found in parents of " + name);
        if (DropArea == null) Debug.LogWarning("DropArea not assigned on " + name);
        if (playerTransform == null) Debug.LogWarning("playerTransform not assigned on " + name);

        // Make DropArea predictable: center pivot & center anchors
        if (DropArea != null)
        {
            DropArea.pivot = new Vector2(0.5f, 0.5f);
            DropArea.anchorMin = DropArea.anchorMax = new Vector2(0.5f, 0.5f);
        }

        // Reparent overlayImage to canvas root if provided
        if (overlayImage != null && canvas != null)
        {
            if (overlayImage.transform.parent != canvas.transform)
                overlayImage.transform.SetParent(canvas.transform, false);

            RectTransform overlayRT = overlayImage.GetComponent<RectTransform>();
            if (overlayRT != null)
            {
                overlayRT.pivot = new Vector2(0.5f, 0.5f);
                overlayRT.anchorMin = overlayRT.anchorMax = new Vector2(0.5f, 0.5f);
            }
        }
    }

    void LateUpdate()
    {
        if (DropArea == null || playerTransform == null || canvas == null) return;

        // If the canvas is WorldSpace, tie DropArea directly to the player's world position.
        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            DropArea.position = playerTransform.position;
            DropArea.rotation = canvas.transform.rotation;

            if (overlayImage != null)
            {
                RectTransform overlayRT = overlayImage.GetComponent<RectTransform>();
                overlayRT.position = playerTransform.position;
                overlayRT.rotation = canvas.transform.rotation;
                overlayImage.transform.SetAsLastSibling();
            }
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;

        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;
        if (cam == null) cam = Camera.main;

        Vector3 screenPoint = (cam != null) ? cam.WorldToScreenPoint(playerTransform.position) : RectTransformUtility.WorldToScreenPoint(null, playerTransform.position);

        if (screenPoint.z < 0f)
        {
            if (DropArea.gameObject.activeSelf) DropArea.gameObject.SetActive(false);
            if (overlayImage != null && overlayImage.gameObject.activeSelf) overlayImage.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (!DropArea.gameObject.activeSelf) DropArea.gameObject.SetActive(true);
            if (overlayImage != null && !overlayImage.gameObject.activeSelf) overlayImage.gameObject.SetActive(true);
        }

        Vector3 worldPoint;
        Camera camForRect = (canvas.renderMode == RenderMode.ScreenSpaceCamera) ? cam : null;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPoint, camForRect, out worldPoint))
        {
            DropArea.position = worldPoint;
            DropArea.rotation = canvasRect.rotation;

            if (overlayImage != null)
            {
                RectTransform overlayRT = overlayImage.GetComponent<RectTransform>();
                overlayRT.position = worldPoint;
                overlayRT.rotation = canvasRect.rotation;
                overlayImage.transform.SetAsLastSibling();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isTap = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isTap = false;
        isDragging = true;
        wasInsideDropArea = isInsideDropArea;

        if (isInsideDropArea)
        {
            RemoveFromStack();
            isInsideDropArea = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        if (imageComponent != null && idleSprite != null) imageComponent.sprite = idleSprite;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        if (isTap)
        {
            StartCoroutine(ReturnToOriginalPosition());
        }
        else if (IsOverDropArea(eventData))
        {
            PlaceInDropArea();
        }
        else
        {
            StartCoroutine(ReturnToOriginalPosition());
        }
    }

    void PlaceInDropArea()
    {
        // If already inside, do nothing (or reorder to top if mass==20)
        if (isInsideDropArea)
        {
            // if this object is already inside but mass==20, push it to top
            if (objectMass == 20f)
            {
                if (stackedObjects.Contains(gameObject))
                {
                    stackedObjects.Remove(gameObject);
                    stackedObjects.Add(gameObject);
                    EnsureMassTop(); // keep mass 20 at top (this will put it at end)
                    StartCoroutine(UpdateStackPositions());
                    UpdateBurdenedStates();
                }
            }
            return;
        }

        // Add to stack
        stackedObjects.Add(gameObject);
        parallaxEffect?.AddMass(objectMass);
        isInsideDropArea = true;

        // Ensure item with mass==20 is always at the top (end of list)
        EnsureMassTop();

        // Update burden states and animate stack positions
        UpdateBurdenedStates();
        StartCoroutine(UpdateStackPositions());
    }

    IEnumerator ReturnToOriginalPosition()
    {
        yield return StartCoroutine(SmoothMove(rectTransform, startPosition, moveDuration));
        isInsideDropArea = false;
    }

    bool IsOverDropArea(PointerEventData eventData)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(DropArea, eventData.position, eventData.pressEventCamera);
    }

    void RemoveFromStack()
    {
        if (stackedObjects.Contains(gameObject))
        {
            int removedIndex = stackedObjects.IndexOf(gameObject);
            stackedObjects.Remove(gameObject);
            parallaxEffect?.RemoveMass(objectMass);

            // After removal, ensure the special-mass object remains top and animate
            EnsureMassTop();
            UpdateBurdenedStates();
            StartCoroutine(UpdateStackPositions());
        }
    }

    // Ensure any object(s) with objectMass == 20 are placed at the end of the stackedObjects list.
    // If there are multiple, their relative order is preserved and all moved to the end in the order found.
    void EnsureMassTop()
    {
        if (stackedObjects.Count <= 1) return;

        List<GameObject> mass20 = new List<GameObject>();
        List<GameObject> others = new List<GameObject>();

        foreach (var obj in stackedObjects)
        {
            DragDropUI s = obj.GetComponent<DragDropUI>();
            if (s != null && Mathf.Approximately(s.objectMass, 20f))
                mass20.Add(obj);
            else
                others.Add(obj);
        }

        if (mass20.Count == 0) return;

        // Rebuild stackedObjects: others first, then mass20 objects (so mass20 are visually on top)
        stackedObjects.Clear();
        stackedObjects.AddRange(others);
        stackedObjects.AddRange(mass20);
    }

    // Animate all stacked objects to their correct positions based on current order in stackedObjects.
    IEnumerator UpdateStackPositions()
    {
        // If DropArea uses world positioning we still assign anchored positions relative to DropArea's anchoredPosition
        // Make sure we have a base anchored position to use
        Vector2 baseAnchored = DropArea.anchoredPosition;

        // Start all moves simultaneously
        for (int i = 0; i < stackedObjects.Count; i++)
        {
            RectTransform objRect = stackedObjects[i].GetComponent<RectTransform>();
            if (objRect == null) continue;

            Vector2 target = new Vector2(baseAnchored.x, baseAnchored.y + (i * stackOffset));
            // Start a SmoothMove for each object (they will run in parallel)
            StartCoroutine(SmoothMove(objRect, target, moveDuration));
        }

        // wait for the longest move to complete before finishing this coroutine
        yield return new WaitForSeconds(moveDuration);
    }

    IEnumerator SmoothMove(RectTransform obj, Vector2 targetPosition, float duration)
    {
        Vector2 start = obj.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            obj.anchoredPosition = Vector2.Lerp(start, targetPosition, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        obj.anchoredPosition = targetPosition;
    }

    void UpdateBurdenedStates()
    {
        for (int i = 0; i < stackedObjects.Count; i++)
        {
            DragDropUI script = stackedObjects[i].GetComponent<DragDropUI>();
            if (script != null)
            {
                // Burdened if there is something stacked above it (i < last index)
                bool isBurdened = i < stackedObjects.Count - 1;
                script.SetBurdened(isBurdened);
            }
        }
    }

    public void SetBurdened(bool isBurdened)
    {
        if (imageComponent == null)
            imageComponent = GetComponent<Image>();

        if (imageComponent != null)
        {
            imageComponent.sprite = isBurdened ? burdenedSprite : idleSprite;
        }
    }
}
