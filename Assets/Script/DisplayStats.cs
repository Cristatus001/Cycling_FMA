using UnityEngine;
using TMPro;

public class DisplayStats : MonoBehaviour
{
    // Reference to the script that holds your simulation values.
    public ParallaxEffect parallaxEffect;

    // UI text fields (assign these in the Inspector)
    public TextMeshProUGUI accelerationText;
    public TextMeshProUGUI forceText;
    public TextMeshProUGUI massText;
    //public TextMeshProUGUI velocityText; // If needed

    void Update()
    {
        if (parallaxEffect != null)
        {
            accelerationText.text = "Acceleration: " + parallaxEffect.acceleration.ToString("F4");
            forceText.text = "Force: " + parallaxEffect.force.ToString("F2");
            massText.text = "Mass: " + parallaxEffect.totalMass.ToString("F2");
            //velocityText.text = "Velocity: " + parallaxEffect.velocity.ToString("F2");
        }
    }
}
