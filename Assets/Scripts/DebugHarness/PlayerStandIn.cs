using UnityEngine;
using Game.SceneFlow;

namespace Game.DebugHarness
{
    /// <summary>
    /// Stands in for the real Characters.Player.PlayerController, which this
    /// track deliberately avoids depending on (see PlayerRuntimeContext's own
    /// doc comment on the GameObject-vs-PlayerController typing decision).
    /// Placed in Lobby/Combat to demonstrate the rebind point every real
    /// player spawn point will call.
    /// </summary>
    public class PlayerStandIn : MonoBehaviour
    {
        private void Start()
        {
            // Null when this scene is entered directly (not via Boot -> ... ->
            // Lobby/Combat) - e.g. testing this scene in isolation. Real spawn
            // points always go through Boot first, so this guard only exists
            // for this test scene's convenience.
            if (PlayerRuntimeContext.Instance == null)
            {
                Debug.LogWarning($"{nameof(PlayerStandIn)}: no PlayerRuntimeContext in the scene - press Play from Boot.unity to exercise the real flow.");
                return;
            }

            PlayerRuntimeContext.Instance.BindActivePlayer(gameObject);
        }
    }
}
