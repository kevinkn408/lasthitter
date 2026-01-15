using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Attributes;
using RPG.Combat;

public class AutoAttacker : MonoBehaviour
{
    Fighter fighter;
    CombatTarget combatTarget;
    GameObject currentTarget;

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
        AIAttackBehavior();
    }

    private void AIAttackBehavior()
    {
        if (combatTarget.PlayerAggro() == true)
        {
            fighter.Attack(currentTarget);
        }
        else
        {
            fighter.Cancel();
        }
    }
}
