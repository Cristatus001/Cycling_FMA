using UnityEngine;
using System.Collections;
using TMPro;

public class CyclistManager : MonoBehaviour
{
    [Header("Prefabs & Points")]
    public GameObject cyclistPrefab;      // Cyclist prefab
    public Transform spawnPoint;          // Off-screen behind the player
    public Transform exitPoint;           // Off-screen ahead of the player
    public Transform playerTransform;     // The player's constant position

    [Header("UI")]
    public TextMeshProUGUI tauntText;       // UI element to display taunt messages

    [Header("Player Reference")]
    public ParallaxEffect playerParallax; // Reference to the ParallaxEffect script that exposes velocity and acceleration

    [Header("Stop Settings")]
    public float stopDistance = 5f;                 // Normal stop distance when player’s acceleration is not full
    public float nonOvertakeStopDistance = 0f;        // Stop distance when player's acceleration is full (cyclist won't overtake)
    public float fullAccelerationThreshold = 10f;     // Acceleration threshold at or above which player's acceleration is considered "full"

    // Threshold for detecting significant change (for exit decision)
    private float threshold = 0.5f;

    // Taunt messages (editable later)
    private string[] taunts = {
        "Too slow! Catch me if you can!",
        "Is that all you got?",
        "Maybe you should remove some weight!",
        "Pedal faster, slowpoke!",
        "You’ll never catch up!"
    };

  

    // Internal state
    private GameObject currentCyclist;
    private bool cyclistActive = false;
    private float recordedPlayerVelocity = 0f;       // Velocity recorded when cyclist stops
    private float recordedPlayerAcceleration = 0f;   // Acceleration recorded when cyclist stops

    private void Start()
    {
        // Spawn a cyclist every 15 seconds (first spawn after 8 seconds)
        InvokeRepeating("SpawnCyclist", 8f, 15f);
    }

    void SpawnCyclist()
    {
        if (cyclistActive) return; // Only one cyclist at a time
        currentCyclist = Instantiate(cyclistPrefab, spawnPoint.position, Quaternion.identity);
        cyclistActive = true;
        StartCoroutine(CyclistRoutine(currentCyclist));
    }

    IEnumerator CyclistRoutine(GameObject cyclist)
    {
        float extraSpeed = 2f; // Extra speed boost for overtaking
        bool reachedStopPoint = false;

        // --- Phase 1: Approach ---
        // Determine target stop distance based on player's acceleration.
        // If player's acceleration is full (>= fullAccelerationThreshold), use nonOvertakeStopDistance.
        // Otherwise, use the normal stopDistance.
        float targetStopDistance = (playerParallax.acceleration >= fullAccelerationThreshold)
                                    ? nonOvertakeStopDistance
                                    : stopDistance;

        while (!reachedStopPoint)
        {
            float targetX = playerTransform.position.x + targetStopDistance;
            float currentSpeed = playerParallax.velocity + extraSpeed;
            cyclist.transform.position += Vector3.right * currentSpeed * Time.deltaTime;

            if (cyclist.transform.position.x >= targetX)
            {
                // Clamp exactly to targetX
                cyclist.transform.position = new Vector3(targetX, cyclist.transform.position.y, cyclist.transform.position.z);
                reachedStopPoint = true;
            }
            yield return null;
        }

        // --- Phase 2: Taunt & Wait ---
        // Only display a taunt if the cyclist actually overtakes (targetStopDistance > nonOvertakeStopDistance)
        if (tauntText != null && targetStopDistance > nonOvertakeStopDistance)
        {
            tauntText.text = taunts[Random.Range(0, taunts.Length)];
            tauntText.gameObject.SetActive(true);

        }

        // Record the player's velocity and acceleration at the moment the cyclist stops
        recordedPlayerVelocity = playerParallax.velocity;
        recordedPlayerAcceleration = playerParallax.acceleration;

        // Wait for 20 seconds while monitoring for significant changes
        float waitTime = 20f;
        float elapsed = 0f;
        while (elapsed < waitTime)
        {
            // If the player's acceleration is full, check for changes in acceleration.
            if (playerParallax.acceleration >= fullAccelerationThreshold)
            {
                float deltaA = playerParallax.acceleration - recordedPlayerAcceleration;
                if (Mathf.Abs(deltaA) > threshold)
                {
                    // If player accelerates further, exit backward; if decelerates, exit forward.
                    bool exitBackward = deltaA > 0;
                    StartCoroutine(ExitCyclist(cyclist, exitBackward));
                    yield break;
                }
            }
            else // Otherwise, check velocity change
            {
                float deltaV = playerParallax.velocity - recordedPlayerVelocity;
                if (Mathf.Abs(deltaV) > threshold)
                {
                    bool exitBackward = deltaV > 0;
                    StartCoroutine(ExitCyclist(cyclist, exitBackward));
                    yield break;
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- Phase 3: Decide Exit Direction ---
        // When player's acceleration is full, if no significant change, force the cyclist to exit backward (go back)
        bool finalExitBackward = (playerParallax.acceleration >= fullAccelerationThreshold) ? true : false;
        StartCoroutine(ExitCyclist(cyclist, finalExitBackward));
    }

    IEnumerator ExitCyclist(GameObject cyclist, bool exitBackward)
    {
        // Determine exit direction and target based on exitBackward flag:
        // exitBackward: move left (back toward spawnPoint)
        // otherwise: move right (toward exitPoint)
        Vector3 direction = exitBackward ? Vector3.left : Vector3.right;
        Transform targetPoint = exitBackward ? spawnPoint : exitPoint;

        float exitSpeed = playerParallax.velocity + 2f; // Use current player's velocity plus boost
        if (tauntText != null)
        {
            tauntText.gameObject.SetActive(false);
        }
        while (cyclist != null &&
              (exitBackward
                  ? (cyclist.transform.position.x > targetPoint.position.x)
                  : (cyclist.transform.position.x < targetPoint.position.x)))
        {
            cyclist.transform.position += direction * exitSpeed * Time.deltaTime;
            yield return null;
        }

        

        if (cyclist != null)
        {
            Destroy(cyclist);
        }
        cyclistActive = false;
    }
}
