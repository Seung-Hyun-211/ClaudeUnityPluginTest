using UnityEngine;
using Game.Characters.Enemy;
using Game.Characters.Npc;

namespace Game.DebugHarness
{
    /// <summary>
    /// Proves the real Enemy/VillageNpc/CompanionNpc prefabs (Assets/Prefabs/Characters)
    /// work when instantiated through EnemySpawner/NpcSpawner at runtime, instead of
    /// hand-composed in a scene like the other Test_* harnesses.
    /// </summary>
    public class PrefabSpawnTestHarness : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject villageNpcPrefab;
        [SerializeField] private GameObject companionNpcPrefab;
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private Transform followTarget;

        private EnemyController spawnedEnemy;
        private NpcController spawnedVillageNpc;
        private NpcController spawnedCompanion;

        private void Start()
        {
            spawnedEnemy = EnemySpawner.Spawn(enemyPrefab, enemyData, new Vector3(-3f, 0f, 0f), Quaternion.identity);
            spawnedVillageNpc = NpcSpawner.Spawn(villageNpcPrefab, Vector3.zero, Quaternion.identity);
            spawnedCompanion = NpcSpawner.Spawn(companionNpcPrefab, new Vector3(3f, 0f, 0f), Quaternion.identity,
                new CompanionBrain(followTarget));
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 420, 120), GUI.skin.box);
            GUILayout.Label($"Enemy ({enemyData.Tier}): HP {spawnedEnemy.Health.Current:0}/{spawnedEnemy.Health.Max:0}");
            GUILayout.Label($"VillageNpc: faction={spawnedVillageNpc.Faction.Faction}");
            GUILayout.Label($"CompanionNpc: faction={spawnedCompanion.Faction.Faction}, HP {spawnedCompanion.Health.Current:0}/{spawnedCompanion.Health.Max:0}");
            GUILayout.EndArea();
        }
    }
}
