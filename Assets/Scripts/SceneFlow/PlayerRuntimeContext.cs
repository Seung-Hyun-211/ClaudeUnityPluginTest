using UnityEngine;

namespace Game.SceneFlow
{
    /// <summary>
    /// Boot-scene singleton that owns the player's persistent runtime
    /// identity across scene loads (see scene-and-persistence-system.md §3).
    /// Lobby/Combat scenes each spawn their own scene-local player
    /// representation (motor, camera rig, animator) and rebind it here
    /// rather than recreating logical player state from scratch.
    ///
    /// Deviation from the design doc: the doc's sketch types
    /// <see cref="ActivePlayer"/> as Game.Characters.Player.PlayerController.
    /// That type belongs to a separate, parallel work track and is still in
    /// flux; depending on it here would create a hard compile-time coupling
    /// between two supposedly independent tracks and break this module the
    /// moment PlayerController's public surface changes. Typing
    /// ActivePlayer as plain GameObject keeps Game.SceneFlow independent of
    /// Game.Characters.Player (dependency inversion) — once both tracks
    /// exist, callers that need player-specific components simply
    /// GetComponent&lt;T&gt;() off ActivePlayer.
    /// </summary>
    public class PlayerRuntimeContext : MonoBehaviour
    {
        public static PlayerRuntimeContext Instance { get; private set; }

        /// <summary>The current scene's player representation object, or null if none is bound yet.</summary>
        public GameObject ActivePlayer { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Called by the newly-loaded scene's player spawn point once it has
        /// instantiated the player's representation, so the persistent
        /// context always points at the representation that is actually
        /// live in the current scene.
        /// </summary>
        public void BindActivePlayer(GameObject player) => ActivePlayer = player;
    }
}
