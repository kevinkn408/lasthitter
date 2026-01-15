using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.VFX;

public class MuzzleFlash : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] public GameObject vfx = null;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnVFX()
    {
        Instantiate(vfx, transform.position, Quaternion.identity);
    }
}
