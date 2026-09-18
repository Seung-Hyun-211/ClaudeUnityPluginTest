using UnityEngine;
using Game.AI;

namespace Game.Characters.Npc
{
    /// <summary>
    /// Instantiates an NPC prefab, optionally wiring an IAiBrain via
    /// NpcController.InitializeAi for roles that need AI (CombatHelper,
    /// a wandering Village NPC). Pass brain = null for a plain standing NPC
    /// that has no AiSensor (InitializeAi would throw for those).
    /// </summary>
    public static class NpcSpawner
    {
        public static NpcController Spawn(GameObject prefab, Vector3 position, Quaternion rotation, IAiBrain brain = null)
        {
            var instance = Object.Instantiate(prefab, position, rotation);
            var controller = instance.GetComponent<NpcController>();
            if (brain != null)
            {
                controller.InitializeAi(brain);
            }
            return controller;
        }
    }
}
