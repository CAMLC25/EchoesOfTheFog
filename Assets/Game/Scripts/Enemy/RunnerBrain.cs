using System;
using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Data;
using UnityEngine;
using UnityEngine.AI;

namespace EchoesOfTheFog.Enemy
{
    public enum RunnerState { Idle, Patrol, Chase, Attack, Search, HitReact, Dead }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(Health), typeof(EnemyPerception))]
    public sealed class RunnerBrain : MonoBehaviour
    {
        [SerializeField] private RunnerTuning tuning;
        [SerializeField] private Health target;
        [SerializeField] private Transform[] patrolPoints = Array.Empty<Transform>();
        [SerializeField] private bool findPlayerOnStart = true;
        private NavMeshAgent agent;
        private Health health;
        private EnemyPerception perception;
        private EnemyConfig config;
        private Vector3 lastKnownPosition;
        private bool hasMemory, visible, attackPending, started;
        private float lastSeen, nextSense, nextRepath, searchEnd, waitEnd, stunEnd, attackImpact, attackEnd, nextAttack, previousHealth, nextBind;
        private int waypoint, attackIndex = -1;

        public RunnerState State { get; private set; } = RunnerState.Idle;
        public Health Target => target;
        public EnemyConfig Config => config;
        public RunnerTuning Tuning => tuning;
        public bool NavigationReady => agent != null && agent.enabled && agent.isOnNavMesh;
        public float MovementSpeed => NavigationReady ? agent.velocity.magnitude : 0;
        public int CurrentAttackIndex => attackIndex;
        public event Action<RunnerState> StateChanged;
        public event Action<int> AttackStarted;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>(); health = GetComponent<Health>(); perception = GetComponent<EnemyPerception>();
        }
        private void OnEnable()
        {
            health.HealthChanged += OnHealthChanged; health.Died += OnDied; GameplayNoise.Emitted += OnNoise;
        }
        private void OnDisable()
        {
            health.HealthChanged -= OnHealthChanged; health.Died -= OnDied; GameplayNoise.Emitted -= OnNoise;
            attackPending = false;
            if (NavigationReady) { agent.ResetPath(); agent.isStopped = true; }
        }
        private void Start()
        {
            if (tuning == null) { Debug.LogError("RunnerBrain needs RunnerTuning.", this); enabled = false; return; }
            config = GameConfigLoader.LoadDefault()["Runner"];
            health.ResetHealth(config.Health); previousHealth = config.Health;
            if (target == null && findPlayerOnStart)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.GetComponent<Health>();
            }
            agent.speed = config.MoveSpeed; agent.stoppingDistance = config.AttackRange * 0.75f;
            started = true;
            if (BindNavigation()) ReturnToPatrol();
        }

        public void SetTarget(Health value) { target = value; visible = false; nextSense = 0; }
        public void SetPatrolPoints(Transform[] points) { patrolPoints = points ?? Array.Empty<Transform>(); waypoint = 0; }

        private bool BindNavigation()
        {
            if (NavigationReady) return true;
            if (Time.time < nextBind) return false;
            nextBind = Time.time + 1;
            if (!NavMesh.SamplePosition(transform.position, out var hit, tuning.spawnSnapDistance, agent.areaMask)) return false;
            if (agent.enabled) agent.enabled = false;
            transform.position = hit.position; agent.enabled = true;
            return agent.isOnNavMesh;
        }

        private void Update()
        {
            if (!started || health.IsDead || Time.timeScale <= 0) return;
            if (!BindNavigation()) { ChangeState(RunnerState.Idle); return; }
            if (State == RunnerState.HitReact)
            {
                if (Time.time >= stunEnd) { Sense(); if (visible) ChangeState(RunnerState.Chase); else if (hasMemory) BeginSearch(); else ReturnToPatrol(); }
                return;
            }
            if (Time.time >= nextSense)
            {
                Sense();
                if (visible && State != RunnerState.Attack) ChangeState(RunnerState.Chase);
                else if (!visible && State == RunnerState.Chase && Time.time - lastSeen >= tuning.sightMemory) BeginSearch();
            }
            if (target == null || target.IsDead || !target.gameObject.activeInHierarchy)
            {
                visible = false; hasMemory = false;
                if (State == RunnerState.Attack || State == RunnerState.Chase || State == RunnerState.Search) ReturnToPatrol();
            }
            switch (State)
            {
                case RunnerState.Idle:
                    if (Time.time >= waitEnd && HasWaypoint()) ChangeState(RunnerState.Patrol);
                    break;
                case RunnerState.Patrol: Patrol(); break;
                case RunnerState.Chase:
                    if (visible && InAttackRange())
                    {
                        if (Time.time >= nextAttack) BeginAttack(); else Stop();
                    }
                    else MoveTo(lastKnownPosition, config.MoveSpeed, config.AttackRange * 0.75f);
                    break;
                case RunnerState.Attack: Attack(); break;
                case RunnerState.Search:
                    MoveTo(lastKnownPosition, config.MoveSpeed, 0.25f);
                    if (Time.time >= searchEnd) { hasMemory = false; ReturnToPatrol(); }
                    break;
            }
        }

        private void Sense()
        {
            nextSense = Time.time + tuning.senseInterval;
            visible = perception.CanSee(target, config.DetectionRadius, State == RunnerState.Chase || State == RunnerState.Attack);
            if (visible) { lastKnownPosition = target.transform.position; lastSeen = Time.time; hasMemory = true; }
        }
        private bool HasWaypoint() => patrolPoints.Length > 0 && Array.Exists(patrolPoints, point => point != null);
        private void Patrol()
        {
            if (!HasWaypoint()) { ChangeState(RunnerState.Idle); return; }
            for (int i = 0; i < patrolPoints.Length && patrolPoints[waypoint] == null; i++) waypoint = (waypoint + 1) % patrolPoints.Length;
            Vector3 position = patrolPoints[waypoint].position;
            MoveTo(position, tuning.patrolSpeed, 0.2f);
            if (!agent.pathPending && Vector3.ProjectOnPlane(transform.position - position, Vector3.up).sqrMagnitude < 0.5f * 0.5f)
            {
                waypoint = (waypoint + 1) % patrolPoints.Length; waitEnd = Time.time + tuning.patrolWait; ChangeState(RunnerState.Idle);
            }
        }
        private void MoveTo(Vector3 position, float speed, float stoppingDistance)
        {
            if (!NavigationReady) return;
            agent.speed = speed; agent.stoppingDistance = stoppingDistance;
            if (Time.time < nextRepath) return;
            nextRepath = Time.time + tuning.repathInterval;
            if (!NavMesh.SamplePosition(position, out var hit, 1.5f, agent.areaMask)) { Stop(); return; }
            agent.isStopped = false;
            if (!agent.SetDestination(hit.position) || (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid)) Stop();
            // Partial paths are allowed up to their reachable end; attacks still require distance and clear sight.
        }
        private void Stop() { if (NavigationReady) { agent.isStopped = true; agent.ResetPath(); } }
        private void ChangeState(RunnerState state)
        {
            if (State == state) return;
            attackPending = false; State = state; nextRepath = 0;
            if (state == RunnerState.Idle || state == RunnerState.Attack || state == RunnerState.HitReact || state == RunnerState.Dead) Stop();
            StateChanged?.Invoke(state);
        }
        private void ReturnToPatrol() { ChangeState(HasWaypoint() ? RunnerState.Patrol : RunnerState.Idle); }
        private void BeginSearch() { searchEnd = Time.time + config.SearchDuration; ChangeState(RunnerState.Search); }
        private bool InAttackRange() => target != null && !target.IsDead && Vector3.Distance(transform.position, target.transform.position) <= config.AttackRange;
        private void BeginAttack()
        {
            ChangeState(RunnerState.Attack);
            attackIndex = (attackIndex + 1) % 2;
            float duration = tuning.AttackDuration(attackIndex);
            attackImpact = Time.time + duration * tuning.ImpactFraction(attackIndex); attackEnd = Time.time + duration;
            nextAttack = Time.time + Mathf.Max(config.AttackCooldown, duration);
            attackPending = true; AttackStarted?.Invoke(attackIndex);
        }
        private void Attack()
        {
            if (target != null)
            {
                Vector3 toward = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
                if (toward.sqrMagnitude > 0.001f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toward), tuning.turnSpeed * Time.deltaTime);
            }
            if (attackPending && Time.time >= attackImpact)
            {
                attackPending = false;
                if (InAttackRange() && perception.HasClearSight(target))
                {
                    Vector3 direction = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
                    if (direction.sqrMagnitude < 0.001f || Vector3.Angle(transform.forward, direction) <= tuning.attackArc * 0.5f) target.TakeDamage(config.Damage);
                }
            }
            if (Time.time >= attackEnd) { Sense(); if (visible) ChangeState(RunnerState.Chase); else if (hasMemory) BeginSearch(); else ReturnToPatrol(); }
        }
        private void OnNoise(Vector3 position, float radius, Health source)
        {
            if (!started || health.IsDead || source == health || (target != null && source != target)
                || Vector3.Distance(transform.position, position) > radius * tuning.hearingMultiplier) return;
            if (target == null) target = source;
            lastKnownPosition = position; hasMemory = true;
            if (State == RunnerState.Chase || State == RunnerState.Attack || State == RunnerState.HitReact) return;
            BeginSearch();
        }
        private void OnHealthChanged(float current, float maximum)
        {
            bool hit = started && current > 0 && current < previousHealth;
            previousHealth = current;
            if (!hit) return;
            stunEnd = Time.time + tuning.hitStun;
            // Repeated hits refresh the stun but do not queue animation triggers indefinitely.
            ChangeState(RunnerState.HitReact);
        }
        private void OnDied()
        {
            ChangeState(RunnerState.Dead);
            if (agent != null) agent.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
    }
}
