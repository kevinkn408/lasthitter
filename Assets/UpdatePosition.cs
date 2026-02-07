using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Combat;
using RPG.Attributes;



public class UpdatePosition : MonoBehaviour
{
    [SerializeField] Fighter fighter;
    private Vector3 targetTransform;
    private Health target;
    [SerializeField] GameObject vfx;

    void Update()
    {
        if (fighter.GetTarget == null)
        {
            vfx.gameObject.SetActive(false);
        }
        else
        {
            vfx.gameObject.SetActive(true);
            targetTransform = fighter.GetTarget.transform.position;
            this.gameObject.transform.position = targetTransform;
        }
    }
}
