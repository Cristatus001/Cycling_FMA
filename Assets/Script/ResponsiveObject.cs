using UnityEngine;
using UnityEngine.EventSystems;

public class ResponsiveObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float offsetX = 0f;      // Adjust as needed
    public float offsetY = 0f;      // Adjust as needed
    public float cameraDistance = 10f;  // Distance from camera to sprite (for perspective cam)

    private Camera mainCam;
    public bool isDragging = false; // Flag to control snapping

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (!isDragging)
        {
            // Get the bottom-right screen coordinate
            Vector3 screenCorner = new Vector3(Screen.width, 0, cameraDistance);
            // Convert to world space
            Vector3 bottomRightWorld = mainCam.ScreenToWorldPoint(screenCorner);

            // Apply offset if needed (adjust sign based on pivot)
            Vector3 finalPosition = bottomRightWorld + new Vector3(-offsetX, offsetY, 0);

            // Set the object's position
            transform.position = finalPosition;
        }
    }

    // These methods use Unity's EventSystems for pointer interactions.
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }
}
