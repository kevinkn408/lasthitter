using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class particleAttractorLinear : MonoBehaviour 
{
	private ParticleSystem ps;
	ParticleSystem.Particle[] m_Particles;
	public Transform target;
	public float speed = 5f;
    public float delay = 0f;
    private float time;
	int numParticlesAlive;

    private void Awake()
    {
        target = GameObject.FindWithTag("Player").transform;
    }

    void Start () 
    {
		ps = GetComponent<ParticleSystem>();
	}

	void Update ()
    {
        time += Time.deltaTime;
        if (time < delay) return;
        FlyToPlayer();
    }

    private void OnEnable()
    {
        time = 0;
    }

    private void FlyToPlayer()
    {
        m_Particles = new ParticleSystem.Particle[ps.main.maxParticles];
        numParticlesAlive = ps.GetParticles(m_Particles);

        for (int i = 0; i < numParticlesAlive; i++)
        {
            float step = Mathf.Pow(time, 2) * speed * Time.deltaTime;  // Use time^2 for a quadratic increase
            m_Particles[i].position = Vector3.SlerpUnclamped(m_Particles[i].position, target.position, step);
        }

        ps.SetParticles(m_Particles, numParticlesAlive);
    }
}
