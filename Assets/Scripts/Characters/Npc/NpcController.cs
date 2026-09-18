using System;
using UnityEngine;
using Game.AI;
using Game.AI.States;
using Game.Characters;
using Game.Combat;

namespace Game.Characters.Npc
{
    /// <summary>
    /// The only NPC component — Village and CombatHelper are the same
    /// NpcController, distinguished by NpcData.Role plus whichever optional
    /// components a prefab attaches alongside it (DialogueInteractable,
    /// HealthComponent, AiSensor + InitializeAi(CompanionBrain/WanderBrain),
    /// CompanionOrderReceiver, ...). Adding a third role never means adding a
    /// subclass here (open-closed) — see npc-roles.md.
    ///
    /// Identity (NpcData) and faction are always present; the AI framework is
    /// optional (a plain standing Village NPC never calls InitializeAi). This
    /// class does NOT implement IInteractable itself — the Village NPC worked
    /// example in npc-roles.md attaches the existing DialogueInteractable
    /// component side by side instead, avoiding two competing IInteractable
    /// implementations fighting over one GameObject.
    /// </summary>
    [RequireComponent(typeof(FactionMember))]
    public class NpcController : MonoBehaviour
    {
        [SerializeField] private NpcData data;
        [SerializeField] private FactionMember faction;
        [SerializeField] private HealthComponent health;
        [SerializeField] private AiSensor sensor;

        private AiStateMachine stateMachine;
        private IAiState deadState;
        private bool aiActive;

        public NpcData Data => data;
        public FactionMember Faction => faction;

        /// <summary>Null on NPCs that can't be damaged (see npc-roles.md) — HealthComponent is attached only on prefabs that need it.</summary>
        public HealthComponent Health => health;

        /// <summary>Null on NPCs with no AI at all (a plain standing Village NPC) — see InitializeAi.</summary>
        public AiSensor Sensor => sensor;

        private void Awake()
        {
            if (data != null)
            {
                faction.SetFaction(data.Faction);
            }
        }

        /// <summary>
        /// Wires the AI framework for roles that need one (CombatHelper via
        /// CompanionBrain, wandering Village NPCs via WanderBrain) — mirrors
        /// Game.Characters.Enemy.EnemyController.Initialize's pattern. Never
        /// called for a plain standing Village NPC, which has no AiSensor and
        /// stays purely NpcData + FactionMember (+ DialogueInteractable).
        /// </summary>
        public void InitializeAi(IAiBrain brain)
        {
            if (sensor == null)
            {
                throw new InvalidOperationException($"{nameof(InitializeAi)} requires an AiSensor to be assigned.");
            }

            stateMachine = new AiStateMachine();
            deadState = new DeadState();

            var context = new AiContext(gameObject, sensor, stateMachine);
            stateMachine.Initialize(context);
            stateMachine.ChangeState(brain.BuildInitialState(context));

            if (health != null)
            {
                health.Died += HandleDied;
            }

            aiActive = true;
        }

        private void Update()
        {
            if (aiActive)
            {
                stateMachine.Tick(Time.deltaTime);
            }
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
    }
}
