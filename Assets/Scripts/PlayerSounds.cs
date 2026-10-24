using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerSounds : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] AudioClip[] steps;
    [SerializeField] AudioClip[] grunts;
    [SerializeField] float stepVolume = 1f;
    [SerializeField] float gruntVolume = 0.8f;

    AudioSource source;
    PlayerController player;
    Transform leftFoot, rightFoot;
    readonly bool[] planted = { true, true };
    float floorL, floorR, lastStep, lastGrunt;
    Vector3 lastPos;
    float speed, airTime;
    bool wasGrounded = true;
    int lastStepClip = -1, lastGruntClip = -1;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        player = GetComponent<PlayerController>();
        if (player != null)
        {
            player.Jumped += Grunt;
            player.Dashed += Grunt;
        }
        lastPos = transform.position;
    }

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }
        floorL = floorR = 1e6f;
    }

    bool Grounded => player == null ? true : player.IsGrounded;

    void LateUpdate()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        var delta = transform.position - lastPos;
        lastPos = transform.position;
        float ground = new Vector2(delta.x, delta.z).magnitude / dt;
        if (ground > 200f) ground = 0f;
        speed = Mathf.Lerp(speed, ground, 1f - Mathf.Exp(-10f * dt));

        bool grounded = Grounded;
        if (grounded && !wasGrounded && player != null && airTime > 0.3f) Step(1.25f);
        airTime = grounded ? 0f : airTime + dt;
        wasGrounded = grounded;

        if (leftFoot == null || rightFoot == null) return;
        Foot(0, leftFoot, ref floorL, grounded);
        Foot(1, rightFoot, ref floorR, grounded);
    }

    void Foot(int i, Transform foot, ref float floor, bool grounded)
    {
        float height = foot.position.y - transform.position.y;
        floor = Mathf.Min(floor + 0.3f * Time.deltaTime, height);
        if (planted[i])
        {
            if (height > floor + 0.1f) planted[i] = false;
        }
        else if (height < floor + 0.04f)
        {
            planted[i] = true;
            if (grounded && speed > 1.5f && Time.time - lastStep > 0.12f) Step(1f);
        }
    }

    void Step(float loudness)
    {
        if (steps == null || steps.Length == 0 || Time.time - lastStep < 0.08f) return;
        lastStep = Time.time;
        int k = Pick(steps.Length, ref lastStepClip);
        source.pitch = Random.Range(0.92f, 1.1f);
        float v = stepVolume * loudness * Mathf.Lerp(0.85f, 1f, Mathf.InverseLerp(4f, 22f, speed));
        source.PlayOneShot(steps[k], v * GameSettings.Sfx);
    }

    public void Grunt()
    {
        if (grunts == null || grunts.Length == 0 || Time.time - lastGrunt < 0.25f) return;
        lastGrunt = Time.time;
        int k = Pick(grunts.Length, ref lastGruntClip);
        source.pitch = Random.Range(1.0f, 1.12f);
        source.PlayOneShot(grunts[k], gruntVolume * GameSettings.Sfx);
    }

    static int Pick(int count, ref int last)
    {
        int k = Random.Range(0, count);
        if (count > 1 && k == last) k = (k + 1) % count;
        return last = k;
    }

    public void Setup(Animator anim, AudioClip[] stepClips, AudioClip[] gruntClips)
    {
        animator = anim;
        steps = stepClips;
        grunts = gruntClips;
    }
}
