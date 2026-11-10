namespace EchoesOfTheFog.Data
{
    public sealed class EnemyConfig
    {
        public string Id { get; }
        public float Health { get; }
        public float MoveSpeed { get; }
        public float Damage { get; }
        public float DetectionRadius { get; }
        public float AttackRange { get; }
        public float AttackCooldown { get; }
        public float SearchDuration { get; }

        internal EnemyConfig(string id, float health, float moveSpeed, float damage,
            float detectionRadius, float attackRange, float attackCooldown, float searchDuration)
        {
            Id = id;
            Health = health;
            MoveSpeed = moveSpeed;
            Damage = damage;
            DetectionRadius = detectionRadius;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
            SearchDuration = searchDuration;
        }
    }
}
