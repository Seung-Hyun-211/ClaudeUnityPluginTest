using System;
using UnityEngine;

namespace Game.SceneFlow
{
    /// <summary>
    /// Full set of <see cref="SceneDefinitionData"/> entries known to the game.
    /// <see cref="SceneFlowController"/> only ever talks to a scene kind
    /// through this catalog, so new scene kinds/maps never require touching
    /// the controller (open-closed) — see scene-and-persistence-system.md §1.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/SceneFlow/Scene Catalog")]
    public class SceneCatalog : ScriptableObject
    {
        [SerializeField] private SceneDefinitionData[] scenes = Array.Empty<SceneDefinitionData>();

        /// <summary>Returns the first entry matching <paramref name="kind"/>, or null if none is registered.</summary>
        public SceneDefinitionData Get(SceneKind kind) => Array.Find(scenes, s => s != null && s.Kind == kind);
    }
}
