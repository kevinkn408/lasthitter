using UnityEngine;
using RPG.Attributes;
using RPG.Control;
using RPG.Attributes;

namespace RPG.Combat
{
    [RequireComponent(typeof(Health))]
    public class CombatTarget : MonoBehaviour, IRaycastable
    {

        bool playerAggro = false;
        Health currentTarget;


        public CursorType GetCursorType()
        {
            return CursorType.Combat;
        }

        public void Update()
        {
            print(playerAggro);
        }

        public bool PlayerAggro()
        {
            return playerAggro;
        }

        public bool HandleRaycast(PlayerController callingController)
        {
            if (callingController.GetComponent<Fighter>().CanAttack(gameObject) == false)
            {
                return false; 
            }

            if (Input.GetMouseButtonDown(0))
            {
                playerAggro = false;
                //callingController.GetComponent<Fighter>().StopAttack();
                if (!GetComponent<Health>().IsDead())
                {
                    callingController.GetComponent<Fighter>().Attack(gameObject);
                    playerAggro = true;

                    if (callingController.GetComponent<Fighter>().AttackRecovery == 0)
                    {
                        callingController.GetComponent<Fighter>().TriggerAttack();
                    }
                    
                }
            }
            return true;
        }
    }
}