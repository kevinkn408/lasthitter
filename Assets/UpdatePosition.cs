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
            print("no tracking");
            vfx.gameObject.SetActive(false);
        }
        else
        {
            print("tracking");
            vfx.gameObject.SetActive(true);

            targetTransform = fighter.GetTarget.transform.position;
            this.gameObject.transform.position = targetTransform;
        }
    }
}
