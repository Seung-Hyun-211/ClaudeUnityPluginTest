using UnityEngine;

namespace Game.SceneFlow
{
    /// <summary>
    /// One catalog entry mapping a <see cref="SceneKind"/> to the actual
    /// build-settings scene name. Adding a new scene (or a new map for
    /// <see cref="SceneKind.Combat"/>) is a new asset instance, never a code
    /// change (open-closed) — see scene-and-persistence-system.md §1.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/SceneFlow/Scene Definition")]
    public class SceneDefinitionData : ScriptableObject
    {
        [SerializeField] private SceneKind kind;
        [SerializeField] private string sceneName;
        [SerializeField] private bool requiresSaveDataLoaded;

        public SceneKind Kind => kind;

        /// <summary>Scene name as registered in Build Settings.</summary>
        public string SceneName => sceneName;

        /// <summary>True for Lobby/Combat: these scenes assume save data has already been applied to PlayerRuntimeContext.</summary>
        public bool RequiresSaveDataLoaded => requiresSaveDataLoaded;
    }
}
