using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Attributes;
using RPG.Combat;

public class AutoAttacker : MonoBehaviour
{
    Fighter fighter;
    CombatTarget combatTarget;
    GameObject currentTarget = null;

    // Start is called before the first frame update
    void Awake()
    {
        fighter = GetComponent<Fighter>();
        combatTarget = GetComponent<CombatTarget>();
    }

    // Update is called once per frame
    void Update()
    {
        currentTarget = GetComponent<SortTargets>().ClosestTarget();
        if (currentTarget == null) return;
        fighter.Attack(currentTarget);
    }
}
