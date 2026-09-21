using System.Collections.Generic;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// The translucent piece shown where the next click would build. Made from
    /// the piece's own prefab with colliders and scripts switched off and every
    /// material swapped for the BuildGhost shader; green when the piece can be
    /// placed, red when not, fading between the two over a short time.
    /// </summary>
    public class GhostPreview : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Shader ghostShader;
        [SerializeField] private Color validColor = new(0.2f, 1f, 0.3f, 0.4f);
        [SerializeField] private Color invalidColor = new(1f, 0.2f, 0.2f, 0.4f);
        [SerializeField, Min(0f)] private float fadeSeconds = 0.1f;

        private readonly Dictionary<GameObject, GameObject> instances = new();
        private Material material;
        private GameObject active;
        private Color shown;

        public bool IsVisible => active != null && active.activeSelf;

        private void Awake()
        {
            if (ghostShader == null)
            {
                ghostShader = Shader.Find("Game/BuildGhost");
            }

            if (ghostShader == null)
            {
                Debug.LogError("GhostPreview needs the Game/BuildGhost shader.", this);
                return;
            }

            material = new Material(ghostShader) { name = "BuildGhost (instance)", hideFlags = HideFlags.HideAndDontSave };
            shown = validColor;
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        public void Show(BuildPieceData data, Vector3 position, Quaternion rotation, bool valid)
        {
            if (material == null || data == null || data.Prefab == null)
            {
                Hide();
                return;
            }

            var instance = GetInstance(data.Prefab);
            if (active != instance)
            {
                Hide();
                active = instance;
            }

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            var target = valid ? validColor : invalidColor;
            float step = fadeSeconds <= 0f ? 1f : Mathf.Clamp01(Time.unscaledDeltaTime / fadeSeconds);
            shown = Color.Lerp(shown, target, step);
            material.SetColor(ColorId, shown);
        }

        public void Hide()
        {
            if (active != null)
            {
                active.SetActive(false);
            }

            active = null;
        }

        private GameObject GetInstance(GameObject prefab)
        {
            if (instances.TryGetValue(prefab, out var existing) && existing != null)
            {
                return existing;
            }

            var instance = Instantiate(prefab, transform);
            instance.name = prefab.name + " (ghost)";

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            instance.SetActive(false);
            instances[prefab] = instance;
            return instance;
        }
    }
}
