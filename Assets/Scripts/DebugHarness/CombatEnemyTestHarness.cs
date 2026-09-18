using System.Reflection;
using UnityEngine;
using Game.AI;
using Game.Characters.Enemy;
using Game.Combat;

namespace Game.DebugHarness
{
    /// <summary>
    /// Manual test harness for combat-system.md/ai-state-machine.md/
    /// character-system.md's Enemy section. The three EnemyController
    /// instances (Normal/Elite/Boss) are pre-placed and wired in the scene;
    /// this only initializes them and lets you damage them / move a target
    /// near or away to watch their AiStateMachine react.
    /// Reads EnemyController's private state-machine field via reflection
    /// purely for on-screen display - production code is never touched or
    /// required to expose this. OnGUI only; never shipped in a real build.
    /// </summary>
    public class CombatEnemyTestHarness : MonoBehaviour
    {
        [SerializeField] private EnemyController normalEnemy;
        [SerializeField] private EnemyData normalEnemyData;
        [SerializeField] private EnemyController eliteEnemy;
        [SerializeField] private EnemyData eliteEnemyData;
        [SerializeField] private EnemyController bossEnemy;
        [SerializeField] private EnemyData bossEnemyData;
        [SerializeField] private Transform target;
        [SerializeField] private Transform farAwayPoint;
        [SerializeField] private Transform nearPoint;

        private static readonly FieldInfo StateMachineField =
            typeof(EnemyController).GetField("stateMachine", BindingFlags.NonPublic | BindingFlags.Instance);

        private void Start()
        {
            normalEnemy.Initialize(normalEnemyData);
            eliteEnemy.Initialize(eliteEnemyData);
            bossEnemy.Initialize(bossEnemyData);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 460, 360), GUI.skin.box);
            GUILayout.Label("Combat + Enemy AI Test Harness");

            if (GUILayout.Button("Move target near enemies")) target.position = nearPoint.position;
            if (GUILayout.Button("Move target far away")) target.position = farAwayPoint.position;
            if (GUILayout.Button("Damage target (20)")) Damage(target.GetComponent<HealthComponent>(), 20f);

            GUILayout.Space(8);
            DrawEnemy("Normal", normalEnemy);
            DrawEnemy("Elite", eliteEnemy);
            DrawEnemy("Boss", bossEnemy);
            GUILayout.EndArea();
        }

        private void DrawEnemy(string label, EnemyController enemy)
        {
            var health = enemy.Health;
            string stateName = GetCurrentStateName(enemy);
            GUILayout.Label($"{label}: HP {health.Current}/{health.Max} alive={health.IsAlive} state={stateName}");
            if (GUILayout.Button($"Damage {label} (30)")) Damage(health, 30f);
        }

        private static string GetCurrentStateName(EnemyController enemy)
        {
            if (StateMachineField?.GetValue(enemy) is AiStateMachine machine && machine.CurrentState != null)
            {
                return machine.CurrentState.GetType().Name;
            }
            return "(none)";
        }

        private static void Damage(HealthComponent health, float amount)
        {
            if (health == null || !health.IsAlive)
            {
                return;
            }
            health.TakeDamage(new DamageInfo(amount, DamageType.Physical, null, health.transform.position));
        }
    }
}
