using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RPG.Combat;
namespace RPG.Combat
{
    public class Weapon : MonoBehaviour
    {
        // Start is called before the first frame update
        [SerializeField] UnityEvent onHit;
        [SerializeField] GameObject impactEffect = null;
        [SerializeField] AnimationClip attackAnimation = null;
        public AnimationClip AttackAnimation { get {return attackAnimation;}  }


        public void OnHit()
        {
            onHit?.Invoke();
            UnityEngine.Debug.Log("fuck");
            if (impactEffect != null)
            {
                //GetComponent<Fighter> might be a hard dependency, look for a new way to do this down the light
                //Weapon & Fighter are always present
                Instantiate(impactEffect, GetComponentInParent<Fighter>().GetTarget.transform.position + new Vector3(0,1,0), Quaternion.identity);
            }
        }
    }

}