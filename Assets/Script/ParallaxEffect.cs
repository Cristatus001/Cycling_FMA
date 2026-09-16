using UnityEngine;
using UnityEngine.UI;

public class ParallaxEffect : MonoBehaviour
{
    public float force = 10f;
    public float totalMass = 50f;
    public float acceleration;
    public float velocity = 0f;
    public float drag = 0.1f;

    public GameObject parallaxImagePrefab; // Prefab for the parallax image
    public float resetOffset = 0f; // Distance before resetting images

    public Slider forceSlider;
    public Slider accelerationSlider;

    public Animator playerAnim;

    private GameObject image1;
    private GameObject image2;

    private int massCount = 0;

    void Start()
    {
        image1 = Instantiate(parallaxImagePrefab, transform.position, Quaternion.identity);
        image2 = Instantiate(parallaxImagePrefab, transform.position + Vector3.right * image1.GetComponent<SpriteRenderer>().bounds.size.x, Quaternion.identity);

        if (forceSlider != null)
            forceSlider.value = force;

        accelerationSlider.onValueChanged.AddListener(UpdateAnimationSpeed);
    }

    void FixedUpdate()
    {
        force = forceSlider.value;

        if (totalMass <= 0) totalMass = 1f;

        acceleration = force / totalMass;

        if (accelerationSlider != null)
            accelerationSlider.value = acceleration;

        UpdateAnimationState();

        velocity += acceleration * Time.fixedDeltaTime;
        velocity = Mathf.Lerp(velocity, 0, drag * Time.fixedDeltaTime);

        Vector3 moveVector = Vector3.left * velocity * Time.fixedDeltaTime;

        // Move both images
        image1.transform.position += moveVector;
        image2.transform.position += moveVector;

        // Reset positions to loop seamlessly
        if (image1.transform.position.x < -image1.GetComponent<SpriteRenderer>().bounds.size.x)
        {
            image1.transform.position = image2.transform.position + Vector3.right * image1.GetComponent<SpriteRenderer>().bounds.size.x;
        }

        if (image2.transform.position.x < -image2.GetComponent<SpriteRenderer>().bounds.size.x)
        {
            image2.transform.position = image1.transform.position + Vector3.right * image2.GetComponent<SpriteRenderer>().bounds.size.x;
        }
    }

    void UpdateAnimationState()
    {
        if (massCount == 0)
        {
            playerAnim.Play(force < 350 ? "Player" : "Anim2");
        }
        else if (massCount == 1)
        {
            playerAnim.Play(force < 350 ? "Anim2" : "Anim3");
        }
        else if (massCount == 2)
        {
            playerAnim.Play(force < 350 ? "Anim3" : "Anim4");
        }
        else if (massCount >= 3)
        {
            playerAnim.Play(force < 350 ? "Anim4" : "Anim5");
        }
    }

    void UpdateAnimationSpeed(float value)
    {
        playerAnim.speed = Mathf.Clamp(value * 0.5f, 0.5f, 2f);
    }

    public void AddMass(float mass)
    {
        totalMass += mass;
        massCount++;
        Debug.Log("Mass Added: " + mass + " | Total Mass: " + totalMass);
    }

    public void RemoveMass(float mass)
    {
        totalMass -= mass;
        massCount = Mathf.Max(0, massCount - 1);
        if (totalMass < 1f) totalMass = 1f;
        Debug.Log("Mass Removed: " + mass + " | Total Mass: " + totalMass);
    }
}
