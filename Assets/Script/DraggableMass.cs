using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableMass : MonoBehaviour
{
    public float massValue = 10f; // Custom mass value per object
    private Vector3 startPosition;
    private Rigidbody2D rb;
    public ParallaxEffect parallaxEffect;

    void Start()
    {
        startPosition = transform.position;
        //parallaxEffect = FindObjectOfType<ParallaxEffect>();
        rb = GetComponent<Rigidbody2D>();

        // Ensure Rigidbody2D exists and is Kinematic for dragging
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnMouseDrag()
    {

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPoint.z = 0f; // Keep in 2D plane
        transform.position = worldPoint;
    }


    private void OnMouseDown()
    {
        rb.linearVelocity = Vector2.zero; // Stop movement while dragging

    }

    private void OnMouseUp()
    {
        // Convert mouse position to world position for physics check
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0f;

        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
        if (hit.collider != null && hit.collider.CompareTag("DropArea"))
        {
            // If dropped in a valid area, add mass and disable object
            parallaxEffect.AddMass(massValue);
            gameObject.SetActive(false);
        }
        else
        {
            // Reset position if dropped outside
            transform.position = startPosition;
        }
    }
}
