using UnityEngine;
using RPG.Movement;
using RPG.Core;
using RPG.Saving;
using RPG.Attributes;
using RPG.Stats;
using System.Collections.Generic;
using GameDevTV.Utils;

namespace RPG.Combat
{
    public class Fighter : MonoBehaviour, IAction, ISaveable, IModifierProvider
    {

        [SerializeField] Health currentTarget;
        [SerializeField] WeaponConfig defaultWeapon = null;
        [SerializeField] Transform rightHandTransform = null;
        [SerializeField] Transform leftHandTransform = null;
        [SerializeField] float speedModifier = 1f;
        AnimatorClipInfo[] currentAnimatorClipInfo;
        public Health GetTarget
        {
            get { return currentTarget; }
            set { currentTarget = value; }
        }
        [SerializeField] Avatar avatar = null;

        //private float attackSpeed = 0f;
        WeaponConfig currentWeaponConfig;
        LazyValue<Weapon> currentWeapon;
        public float timeSinceLastAttack = Mathf.Infinity;
        [SerializeField] float attackAnimationLength = 1f;
        [SerializeField] float attackRecovery = 0f;
        [SerializeField] float timeSinceLastHit = 0f;
        public float AttackRecovery { get { return attackRecovery; } set { attackRecovery = value; } }


        float currentClipLength;

        //so player can attack immediately


        //special skills

        void Awake()
        {
            //For playable characters

            if (GetComponentInChildren<CharacterManager>() != null)
            {
                CharacterManager characterManager = GetComponentInChildren<CharacterManager>();
                rightHandTransform = characterManager.RightHandTransform;
                leftHandTransform = characterManager.LeftHandTransform;
                avatar = characterManager.Avatar;
                defaultWeapon = characterManager.WeaponCFG;
            }

            //For playable characters

            GetComponent<Animator>().avatar = avatar;
            currentWeaponConfig = defaultWeapon;
            currentWeapon = new LazyValue<Weapon>(SetupDefaultWeapon);


        }

        private Weapon SetupDefaultWeapon()
        {
            return AttachWeapon(defaultWeapon);
        }

        // Start is called before the first frame update
        void Start()
        {
            currentWeapon.ForceInit();
        }

        public void EquipWeapon(WeaponConfig weapon)
        {
            currentWeaponConfig = weapon;
            currentWeapon.value = AttachWeapon(weapon);
        }

        private Weapon AttachWeapon(WeaponConfig weapon)
        {
            Animator animator = GetComponent<Animator>();
            return weapon.Spawn(rightHandTransform, leftHandTransform, animator);
        }

        private void Update()
        {
            //while notAttacking == true
            currentAnimatorClipInfo = this.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0);
            currentClipLength = currentAnimatorClipInfo[0].clip.length;

            //print(currentAnimatorClipInfo[0].clip.name);
            //CalculateAttackSpeed();
            CalculateTime();
            HandleAttacking();
        }

        public void HandleAttacking()
        {
            if (currentTarget == null || currentTarget.IsDead())
            {
                currentTarget = null;
                return;
            }

            if (!GetIsInRange(currentTarget.transform))
            {
                GetComponent<Mover>().MoveTo(currentTarget.transform.position, 1f);
                print("not in range");
            }
            else
            {
                GetComponent<Mover>().Cancel();
                AttackBehavior(CalculateAttackSpeed());
            }
        }

        private float CalculateAttackSpeed()
        {
            if (defaultWeapon.equippedPrefab.GetComponent<Animation>().clip != null)
            {

                float attackLength = defaultWeapon.equippedPrefab.GetComponent<Animation>().clip.length;
                float combinedSpeed = 1 / speedModifier; // Inverse the modifier for correct timing
                attackLength = 1 / combinedSpeed;
                GetComponent<Animator>().SetFloat("attackSpeed", combinedSpeed);
                return attackLength;
            }
            return 0;
        }

        private void CalculateTime()
        {
            attackRecovery -= Time.deltaTime;
            if (attackRecovery < 0) attackRecovery = 0;

            // time since character called Hit()
            // used for calculating "cooldown" after character has commited to a hit
            // player can cancel attack animation up until Hit() is called
            timeSinceLastHit += Time.deltaTime;

            // time since character initiated attacking
            // used for calculating auto-attacking
            timeSinceLastAttack += Time.deltaTime;
        }

        public bool CanAttack(GameObject combatTarget)
        {
            if (combatTarget == null) { return false; }
            if (!GetComponent<Mover>().CanMoveTo(combatTarget.transform.position) && !GetIsInRange(currentTarget.transform))
            {
                return false;
            }
            return true;
        }

        public void Attack(GameObject combatTarget)
        {
            GetComponent<ActionScheduler>().StartAction(this);
            currentTarget = combatTarget.GetComponent<Health>();
        }


        private void AttackBehavior(float attackLength)
        {
            transform.LookAt(currentTarget.transform);
            UnityEngine.Debug.Log("Current Target is " + currentTarget);

            if (!this.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            {

                TriggerAttack();

            }

            //if (timeSinceLastAttack > attackLength)
            //{
            //    print("pre attacking");

            //    TriggerAttack();
            //    //this will trigger an animation that will call "Hit()" on a certain frame (check animation clip);

            //}
        }

        public void TriggerAttack()
        {
            if (GetIsInRange(currentTarget.transform))
            {
                timeSinceLastHit = 0f;
                timeSinceLastAttack = 0f;
                GetComponent<Animator>().ResetTrigger("stopAttack");
                GetComponent<Animator>().SetTrigger("attack");
                print("attacking");

            }

        }

        void AnimationStart()
        {

        }

        void AnimationStop()
        {

        }

        void Hit()
        //THIS GETS CALLED ON Animation Event
        //THIS GETS CALLED ON Animation Event
        //THIS GETS CALLED ON Animation Event
        {

            attackRecovery = attackAnimationLength - timeSinceLastHit;

            if (currentTarget == null) return;

            float damage = GetComponent<BaseStats>().GetStat(Stat.Damage);

            if (currentWeapon.value != null)
            {
                currentWeapon.value.OnHit();
            }

            if (currentWeaponConfig.HasProjectile())
            {
                currentWeaponConfig.LaunchProjectile(rightHandTransform, leftHandTransform, currentTarget, this.gameObject, damage);
            }
            else
            {
                currentTarget.TakeDamage(gameObject, damage);
            }

        }

        //I think this was used because animation for bow uses Shoot() and can't be changed (read only)
        // THIS IS RETARDED DONT DO THIS
        // JUST MAKE A COPY OF THE ANIMATION AND ADD HIT()
        // JUST LEAVING AS REMINDER
        void Shoot()
        {
            Hit();
        }

        public bool GetIsInRange(Transform target)
        {
            return Vector3.Distance(transform.position, target.transform.position) < currentWeaponConfig.WeaponRange();
        }

        public void Cancel()
        {
            StopAttack();
            currentTarget = null;
            GetComponent<Mover>().Cancel();
        }

        public void StopAttack()
        {
            GetComponent<Animator>().ResetTrigger("attack");
            GetComponent<Animator>().SetTrigger("stopAttack");
        }

        public object CaptureState()
        {
            //if (currentWeapon.value == null)
            //{
            //    Debug.Log($"{name} does not have a weapon equipped in CaptureState()");
            //}
            return currentWeaponConfig.name;
        }

        public void RestoreState(object state)
        {
            string weaponName = (string)state;
            WeaponConfig weapon = Resources.Load<WeaponConfig>(weaponName);
            EquipWeapon(weapon);
        }

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (stat == Stat.Damage)
            {
                yield return currentWeaponConfig.GetDamage();
            }
        }

        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            if (stat == Stat.Damage)
            {
                yield return currentWeaponConfig.GetPercentageBonus();
            }
        }
    }

}