using UnityEngine;
using UnityEngine.AI;
using RPG.Core;
using RPG.Saving;
using RPG.Attributes;
using RPG.Combat;
using System.Collections;

namespace RPG.Movement
{
    public class Mover : MonoBehaviour, IAction, ISaveable
    {
        [SerializeField] float maxSpeed = 6f;
        [SerializeField] float maxNavPathLength = 40f;
        [SerializeField] private float dashAmount = 5f;
        [SerializeField] private float dashDuration = 1f;
        NavMeshAgent navAgent;
        Health health;
        Vector3 lastMoveDir = Vector3.forward;
        public DriftingJoystickMouse joystick;
        private bool isDashing = false;

        public bool cameraRelative = true;
        Vector3 prevPos;
        Camera cam;




        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            if (!navAgent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    navAgent.Warp(hit.position);
                }
            }
        }

        void Update()
        {
            navAgent.enabled = !health.IsDead();
            UpdateAnimation();

        }

        void OnEnable()
        {
            if (joystick != null)
                joystick.DragSpeedTriggered += AdjustSpeed;
        }

        void OnDisable()
        {
            if (joystick != null)
                joystick.DragSpeedTriggered -= AdjustSpeed;
        }

        // 0–1: how much of the dash stays at full speed
        [SerializeField, Range(0f, 1f)]
        private float holdPercent = 0.7f;

        // Higher = sharper drop at the end
        [SerializeField, Range(1f, 6f)]
        private float falloffSharpness = 3f;

        public void AdjustSpeed()
        {
            if (isDashing) return;
            GetComponent<Animator>().SetTrigger("dash");
            StartCoroutine(DashCoroutine());
        }

        private IEnumerator DashCoroutine()
        {
            isDashing = true;

            float originalSpeed = maxSpeed;
            float boostedSpeed = originalSpeed + dashAmount;

            maxSpeed = boostedSpeed;

            float elapsed = 0f;

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / dashDuration;

                float t;

                if (normalizedTime < holdPercent)
                {
                    // Stay at full speed
                    t = 0f;
                }
                else
                {
                    // Falloff phase
                    float falloffTime =
                        (normalizedTime - holdPercent) / (1f - holdPercent);

                    t = Mathf.Pow(falloffTime, falloffSharpness);
                }

                maxSpeed = Mathf.Lerp(boostedSpeed, originalSpeed, t);
                yield return null;
            }


            maxSpeed = originalSpeed;
            isDashing = false;
        }

        public void HandleRawInput()
        {
            if (joystick == null) return;
            Vector2 input = joystick.Value;

            //Vector2 input = new Vector2(
            //    Input.GetAxisRaw("Horizontal"),
            //    Input.GetAxisRaw("Vertical")
            //);

            if (joystick.TapThisFrame)
            {
                print("This is a TAP");
                return;
            }

            if (input.sqrMagnitude < 0.001f)
                return;

            GetComponent<Fighter>().Cancel();
            GetComponent<ActionScheduler>().StartAction(this);

            if (navAgent.hasPath) navAgent.ResetPath();
            navAgent.updateRotation = false;
            navAgent.autoBraking = false;

            Vector3 moveDir = GetWorldMoveDir(input);
            lastMoveDir = moveDir;

            navAgent.Move(moveDir * maxSpeed * Time.deltaTime);

            // Rotate like the agent would: toward current travel direction at angularSpeed
            RotateToward(moveDir);



            //Vector3 dir = new Vector3(input.x, 0f, input.y).normalized;
            //navAgent.Move(dir * maxSpeed * Time.deltaTime);

            return;
        }


        Vector3 GetWorldMoveDir(Vector2 input)
        {
            Vector3 dir = new Vector3(input.x, 0f, input.y).normalized;

            if (!cameraRelative || cam == null) return dir;

            Vector3 forward = cam.transform.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = cam.transform.right; right.y = 0f; right.Normalize();

            return (right * input.x + forward * input.y).normalized;
        }


        void RotateToward(Vector3 moveDir)
        {
            if (moveDir.sqrMagnitude < 0.0001f) return;

            Quaternion target = Quaternion.LookRotation(moveDir, Vector3.up);

            // NavMeshAgent.angularSpeed is in degrees/second
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                navAgent.angularSpeed * Time.deltaTime
            );
        }

        public bool CanMoveTo(Vector3 destination)
        {
            NavMeshPath path = new NavMeshPath();
            bool hasPath = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);
            if (!hasPath) return false;
            if (path.status != NavMeshPathStatus.PathComplete) return false;
            if (GetPathLength(path) > maxNavPathLength) return false;

            return true;
        }

        private float GetPathLength(NavMeshPath path)
        {
            float total = 0f;
            if (path.corners.Length < 2) return total;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                total += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }

            return total;
        }


        public void StartMoveAction(Vector3 destination, float speedFraction)
        {
            GetComponent<ActionScheduler>().StartAction(this);
            MoveTo(destination, speedFraction);
        }

        public void MoveTo(Vector3 destination, float speedFraction)
        {

            navAgent.isStopped = false;
            navAgent.speed = maxSpeed * Mathf.Clamp01(speedFraction);
            navAgent.SetDestination(destination);
            GetComponent<Fighter>().AttackRecovery = 0f;
        }

        public void Cancel()
        {
            print($"{name} Mover.Cancel() frame={Time.frameCount}\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }


        bool wasIdle;


        private void UpdateAnimation()
        {
            //Vector3 velocity = navAgent.velocity;
            //Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            //float speed = localVelocity.z; //forward speed (z axis)
            //print(velocity);
            //GetComponent<Animator>().SetFloat("forwardSpeed", speed);


            Vector3 delta = transform.position - prevPos;
            prevPos = transform.position;

            float rawSpeed = (delta / Time.deltaTime).magnitude;   // world units/sec
            GetComponent<Animator>().SetFloat("forwardSpeed", rawSpeed, 0.15f, Time.deltaTime);

            bool isIdle = rawSpeed < 0.05f; // tune

            if (isIdle && !wasIdle) print("NOT MOVING");
            wasIdle = isIdle;
        }

        public object CaptureState()
        {
            return new SerializableVector3(transform.position);
        }

        public void RestoreState(object state)
        {
            SerializableVector3 position = (SerializableVector3)state;
            navAgent.enabled = false;
            transform.position = position.ToVector();
            navAgent.enabled = true;
            GetComponent<ActionScheduler>().CancelCurrentAction();
        }
    }
}
