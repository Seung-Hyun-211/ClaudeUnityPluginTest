using UnityEngine;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// Instantiates an Enemy prefab and calls EnemyController.Initialize with
    /// the given EnemyData - the factory step every test harness this session
    /// did by hand (place in scene, then call Initialize in Start()).
    /// </summary>
    public static class EnemySpawner
    {
        public static EnemyController Spawn(GameObject prefab, EnemyData data, Vector3 position, Quaternion rotation)
        {
            var instance = Object.Instantiate(prefab, position, rotation);
            var controller = instance.GetComponent<EnemyController>();
            controller.Initialize(data);
            return controller;
        }
    }
}
