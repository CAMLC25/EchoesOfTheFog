using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerInputReader))]
    [RequireComponent(typeof(Health), typeof(PlayerStamina), typeof(PlayerCameraRig))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float walkSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float runSpeed = 5.2f;
        [SerializeField, Min(0.1f)] private float aimSpeed = 1.8f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.2f;
        [SerializeField, Min(1)] private float acceleration = 18;
        [SerializeField, Min(1)] private float deceleration = 26;
        [SerializeField, Min(1)] private float turnDegreesPerSecond = 540;
        [SerializeField, Range(0, 70)] private float maximumSlope = 48;
        [SerializeField, Range(0, 0.6f)] private float maximumStep = 0.45f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float deathBelowY = -12;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private PlayerInputReader input;
        private PlayerCameraRig view;
        private PlayerStamina stamina;
        private Health health;
        private Vector3 previousPosition;
        private readonly RaycastHit[] groundHits = new RaycastHit[12];

        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>();
            input = GetComponent<PlayerInputReader>(); view = GetComponent<PlayerCameraRig>();
            stamina = GetComponent<PlayerStamina>(); health = GetComponent<Health>();
            previousPosition = body.position;
        }

        private void OnEnable() => health.Died += OnDied;
        private void OnDisable()
        {
            if (health != null) health.Died -= OnDied;
            if (body != null && !body.isKinematic)
                body.linearVelocity = new Vector3(0, body.linearVelocity.y, 0);
        }

        private void OnDied()
        {
            input.SetControlsEnabled(false);
            IsSprinting = false;
            body.linearVelocity = new Vector3(0, body.linearVelocity.y, 0);
        }

        private void FixedUpdate()
        {
            if (health.IsDead) return;
            if (body.position.y < deathBelowY) { health.TakeDamage(health.CurrentHealth); return; }
            float actualSpeed = Vector3.ProjectOnPlane(body.position - previousPosition, Vector3.up).magnitude / Time.fixedDeltaTime;
            previousPosition = body.position;

            IsGrounded = FindGround(out var ground);
            IsGrounded &= body.linearVelocity.y <= 1f;

            Vector2 move = input.Move;
            Quaternion yaw = Quaternion.Euler(0, view.Yaw, 0);
            Vector3 wish = yaw * new Vector3(move.x, 0, move.y);
            bool wantsSprint = IsGrounded && input.SprintHeld && !view.IsAiming && move.sqrMagnitude > 0.01f && stamina.CanSprint;
            IsSprinting = wantsSprint && actualSpeed > walkSpeed + 0.15f;
            stamina.Tick(IsSprinting, Time.fixedDeltaTime);
            float speed = view.IsAiming ? aimSpeed : wantsSprint ? runSpeed : walkSpeed;
            Vector3 desired = wish * speed;
            Vector3 planar = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            planar = Vector3.MoveTowards(planar, desired, (wish.sqrMagnitude > 0 ? acceleration : deceleration) * Time.fixedDeltaTime);
            float vertical = body.linearVelocity.y;
            // The rounded capsule can lift slightly on a riser before the floor probe
            // considers it grounded. Step assistance also accepts a nearby floor below.
            if (wish.sqrMagnitude > 0.01f && vertical <= 1f && TryStep(wish.normalized))
            {
                IsGrounded = FindGround(out ground);
                vertical = Mathf.Min(vertical, 0);
            }
            if (IsGrounded && vertical <= 1f)
            {
                Vector3 tangent = Vector3.ProjectOnPlane(planar, ground.normal);
                vertical = tangent.y - 0.8f;
            }
            body.linearVelocity = new Vector3(planar.x, vertical, planar.z);
            if (input.ConsumeJump() && IsGrounded)
            {
                var velocity = body.linearVelocity;
                velocity.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
                body.linearVelocity = velocity;
                IsGrounded = false;
                IsSprinting = false;
            }
            if (view.IsAiming || wish.sqrMagnitude > 0.01f)
            {
                Quaternion target = view.IsAiming ? yaw : Quaternion.LookRotation(wish);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, target, turnDegreesPerSecond * Time.fixedDeltaTime));
            }
        }

        private bool FindGround(out RaycastHit ground)
        {
            // At a step edge the sphere can hit its vertical riser before the floor.
            // Choose the nearest walkable contact instead of treating that riser as airborne.
            int count = Physics.SphereCastNonAlloc(body.position + Vector3.up * (capsule.radius + 0.12f), capsule.radius * 0.9f,
                Vector3.down, groundHits, 0.25f, groundMask, QueryTriggerInteraction.Ignore);
            ground = default;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.distance >= nearest || Vector3.Angle(hit.normal, Vector3.up) > maximumSlope) continue;
                nearest = hit.distance; ground = hit;
            }
            if (nearest == float.PositiveInfinity && Physics.Raycast(body.position + Vector3.up * 0.15f,
                Vector3.down, out var belowFeet, 0.3f, groundMask, QueryTriggerInteraction.Ignore)
                && Vector3.Angle(belowFeet.normal, Vector3.up) <= maximumSlope)
            {
                ground = belowFeet;
                return true;
            }
            return nearest < float.PositiveInfinity;
        }

        private bool TryStep(Vector3 direction)
        {
            if (maximumStep <= 0) return false;
            if (!Physics.Raycast(body.position + Vector3.up * 0.1f, Vector3.down, out var support,
                maximumStep + 0.15f, groundMask, QueryTriggerInteraction.Ignore)
                || Vector3.Angle(support.normal, Vector3.up) > maximumSlope) return false;
            float reach = capsule.radius + 0.24f;
            // A thin timber platform has open space below its collider. Sweep the
            // whole lower-leg height so that a ray cannot simply pass underneath it.
            Vector3 lower = body.position + Vector3.up * 0.05f;
            Vector3 upperLeg = body.position + Vector3.up * Mathf.Max(0.05f, maximumStep - 0.025f);
            if (!Physics.CapsuleCast(lower, upperLeg, 0.025f, direction, out var obstacle, reach, groundMask, QueryTriggerInteraction.Ignore)
                || Vector3.Angle(obstacle.normal, Vector3.up) <= maximumSlope) return false;
            Vector3 above = body.position + Vector3.up * (maximumStep + 0.06f);
            if (Physics.Raycast(above, direction, reach, groundMask, QueryTriggerInteraction.Ignore)) return false;
            if (!Physics.Raycast(above + direction * reach, Vector3.down, out var top, maximumStep + 0.05f, groundMask, QueryTriggerInteraction.Ignore)
                || Vector3.Angle(top.normal, Vector3.up) > maximumSlope) return false;
            float rise = top.point.y - body.position.y;
            if (rise <= 0.02f || rise > maximumStep) return false;
            Vector3 next = body.position + Vector3.up * (rise + 0.02f);
            if (Physics.CheckCapsule(next + Vector3.up * (capsule.radius + 0.02f),
                next + Vector3.up * (capsule.height - capsule.radius), capsule.radius * 0.95f,
                groundMask, QueryTriggerInteraction.Ignore)) return false;
            body.position = next;
            return true;
        }
    }
}
