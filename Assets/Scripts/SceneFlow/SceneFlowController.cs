using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.SceneFlow
{
    /// <summary>
    /// Sole entry point for scene transitions (see
    /// scene-and-persistence-system.md §2). No other script should call
    /// SceneManager.LoadScene directly — routing every transition through
    /// here is what guarantees "always via Loading" holds everywhere.
    ///
    /// Lives in the Boot scene, which is additive and never unloaded, and is
    /// additionally marked DontDestroyOnLoad so it survives every Single
    /// scene load it triggers on itself.
    /// </summary>
    public class SceneFlowController : MonoBehaviour, ISceneFlowController
    {
        [SerializeField] private SceneCatalog catalog;

        /// <summary>Raised as soon as a transition is requested, before the Loading scene is even loaded.</summary>
        public event Action<SceneKind> TransitionStarted;

        /// <summary>
        /// Raised once the Loading scene has finished loading and is on
        /// screen, right before the target scene starts loading. This is the
        /// only point in a transition where I/O (e.g. a disk save) is safe:
        /// gameplay is fully torn down/paused and the player already
        /// perceives a transition in progress. See Game.Persistence.SaveGameService.
        /// </summary>
        public event Action LoadingScreenEntered;

        /// <summary>Raised after the target scene has finished loading and activating.</summary>
        public event Action<SceneKind> TransitionCompleted;

        /// <summary>
        /// Normalized [0, 1] load progress of the target scene. The Loading
        /// scene's own UI subscribes to this to drive a progress bar;
        /// SceneFlowController deliberately has no reference to any UI type
        /// (dependency inversion — the low-level transition mechanism must
        /// not know about presentation).
        /// </summary>
        public event Action<float> LoadingProgressChanged;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void RequestTransition(SceneKind target)
        {
            StartCoroutine(TransitionRoutine(target));
        }

        private IEnumerator TransitionRoutine(SceneKind target)
        {
            TransitionStarted?.Invoke(target);

            var loading = catalog != null ? catalog.Get(SceneKind.Loading) : null;
            if (loading == null)
            {
                Debug.LogError($"{nameof(SceneCatalog)} has no entry for {SceneKind.Loading}.", this);
                yield break;
            }

            yield return SceneManager.LoadSceneAsync(loading.SceneName, LoadSceneMode.Single);

            LoadingScreenEntered?.Invoke();

            var targetDef = catalog.Get(target);
            if (targetDef == null)
            {
                Debug.LogError($"{nameof(SceneCatalog)} has no entry for {target}.", this);
                yield break;
            }

            var op = SceneManager.LoadSceneAsync(targetDef.SceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                LoadingProgressChanged?.Invoke(op.progress / 0.9f);
                yield return null;
            }

            LoadingProgressChanged?.Invoke(1f);
            op.allowSceneActivation = true;
            yield return op;

            TransitionCompleted?.Invoke(target);
        }
    }
}
