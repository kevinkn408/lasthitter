using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Attributes;

public class Spell_FireStrike : MonoBehaviour
{
    SphereCollider sphereRadius;
    LayerMask enemyLayerMask;
    GameObject instigator = null;
    [SerializeField] float damage = 10f;
    [SerializeField] int delay = 5;
    // Start is called before the first frame update
    void Start()
    {
        sphereRadius = GetComponent<SphereCollider>();
        Invoke("DamageEnemies", delay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void DamageEnemies()
    {
        print("Spell has casted.....");
        // Get all colliders within the sphere's bounds
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1, enemyLayerMask);

        // Loop through all colliders found
        foreach (Collider collider in colliders)
        {
            // Check if the collider has the Health component and is tagged as "Enemy"
            if (collider.CompareTag("Enemy"))
            {
                // Get the Health component and call the Damage method
                Health enemyHealth = collider.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(instigator, damage); // Call the Damage method on the Health component
                }
            }
        }
    }

}
