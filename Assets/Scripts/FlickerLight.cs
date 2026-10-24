using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    [SerializeField] Light lamp;
    [SerializeField] Renderer fixture;
    [SerializeField] Color emission = Color.white;
    [SerializeField] bool flickery;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    float baseIntensity;
    float burstEnd, nextBurst;
    MaterialPropertyBlock block;

    void Start()
    {
        baseIntensity = lamp.intensity;
        block = new MaterialPropertyBlock();
        nextBurst = Time.time + Random.Range(1f, 8f);
    }

    void Update()
    {
        float level = 0.96f + 0.04f * Mathf.PerlinNoise(Time.time * 8f, transform.position.x);

        if (flickery)
        {
            if (Time.time >= nextBurst)
            {
                burstEnd = Time.time + Random.Range(0.15f, 0.8f);
                nextBurst = burstEnd + Random.Range(2f, 10f);
            }
            if (Time.time < burstEnd)
                level = Random.value < 0.5f ? Random.Range(0.05f, 0.3f) : 1f;
        }

        lamp.intensity = baseIntensity * level;
        if (fixture != null)
        {
            if (block == null) block = new MaterialPropertyBlock();
            block.SetColor(EmissionId, emission * level);
            fixture.SetPropertyBlock(block);
        }
    }

    public void Setup(Light light, Renderer renderer, Color emissionColor, bool flicker)
    {
        lamp = light;
        fixture = renderer;
        emission = emissionColor;
        flickery = flicker;
    }
}
