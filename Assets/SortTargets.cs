using System.Collections;
using System.Collections.Generic;
using RPG.Attributes;
using UnityEngine;

public class SortTargets : MonoBehaviour
{
    [SerializeField] float chaseDistance = 10f;
    [SerializeField] public GameObject closestTarget;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    public GameObject ClosestTarget()
    {
        return closestTarget;
    }

    // Update is called once per frame
    void Update()
    {
       closestTarget = DoSortTargets(FindTargets());
    }


    private GameObject DoSortTargets(List<GameObject> targets)
    {
        Transform closestTargetTransform = null;
        float minDistance = Mathf.Infinity;

        Vector3 currentPosition = transform.position;

        foreach (GameObject target in targets.ToArray())
        {
            Vector3 directionToTarget = target.transform.position - currentPosition;

            float distance = Mathf.Sqrt(directionToTarget.sqrMagnitude);

            if (target.GetComponent<Health>().IsDead() || distance > chaseDistance)
            {
                targets.Remove(target);
                continue;
            }
            if (distance < minDistance)
            {
                minDistance = distance;
                closestTargetTransform = target.transform;
            }
        }

        if (targets.Count >= 1)
        {
            return closestTargetTransform.gameObject;
        }
        return null;
    }

    private List<GameObject> FindTargets()
    {
        List<GameObject> allTargets = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
        return allTargets;
    }

}
