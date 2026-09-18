using System;
using UnityEngine;
using Game.AI;
using Game.AI.States;
using Game.Characters;
using Game.Combat;
using Game.Weapons;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// The single Enemy component for every tier (see character-system.md's
    /// Enemy 절: "EnemyController는 하나뿐이다"). Tier differences come
    /// entirely from which EnemyData asset is assigned and, derived from
    /// EnemyData.Tier, which IAiBrain builds the initial state graph - never
    /// add a NormalEnemy/EliteEnemy/BossEnemy subclass; add a new IAiBrain
    /// implementation instead (open-closed).
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private FactionMember faction;
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private AiSensor sensor;

        // Optional - see weapon-system.md "픽업 — 적도 집어서 즉시 사용 가능":
        // WeaponPickup.CanInteract only checks GetComponent<WeaponLoadout>(),
        // so simply having this attached is enough for an Enemy to pick up
        // and use weapons. No Enemy-specific weapon code is needed here.
        [SerializeField] private WeaponLoadout weaponLoadout;

        private readonly AiStateMachine stateMachine = new AiStateMachine();
        private readonly IAiState deadState = new DeadState();

        public EnemyData Data { get; private set; }
        public HealthComponent Health => health;
        public FactionMember Faction => faction;
        public CharacterMotor Motor => motor;
        public AiSensor Sensor => sensor;
        public WeaponLoadout WeaponLoadout => weaponLoadout;

        /// <summary>
        /// Applies EnemyData's stats and picks the IAiBrain matching
        /// data.Tier to build the initial AiContext/state (see
        /// character-system.md's EnemyController class diagram).
        /// </summary>
        public void Initialize(EnemyData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Data = data;

            faction.SetFaction(Game.Characters.Faction.Hostile);
            motor.MoveSpeed = data.MoveSpeed;

            // NOTE (see this track's final report for detail): HealthComponent
            // and AiSensor currently expose no public API to override
            // maxHealth/detectionRadius at runtime - both are private
            // SerializeField-only with no setter, so EnemyData.MaxHealth and
            // EnemyData.DetectionRadius cannot be applied here without
            // modifying Combat/AI files that are out of scope for this track.
            // Until HealthComponent/AiSensor grow that hook, set the
            // prefab's own HealthComponent/AiSensor inspector values to match
            // the EnemyData asset being used.

            var context = new AiContext(gameObject, sensor, stateMachine);
            IAiBrain brain = CreateBrain(data);

            stateMachine.Initialize(context);
            stateMachine.ChangeState(brain.BuildInitialState(context));

            health.Died += HandleDied;
        }

        private void Update()
        {
            stateMachine.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void HandleDied()
        {
            // DeadState never transitions itself (see its own doc comment) -
            // the owner forces the switch directly, once, here.
            stateMachine.ChangeState(deadState);
        }

        private static IAiBrain CreateBrain(EnemyData data)
        {
            switch (data.Tier)
            {
                case EnemyTier.Elite:
                    return new EliteEnemyBrain(data);
                case EnemyTier.Boss:
                    return new BossEnemyBrain(data);
                case EnemyTier.Normal:
                default:
                    return new NormalEnemyBrain(data);
            }
        }
    }
}
