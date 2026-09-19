using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// The stand-in shown for an item that has no WorldPrefab, so anything
    /// dropped is always visible and pick-up-able. A small cube with a
    /// trigger collider: triggers do not push the player around, and the
    /// InteractionDetector's OverlapSphere still finds them.
    /// </summary>
    public static class DefaultWorldVisual
    {
        private const float Size = 0.3f;

        public static GameObject Create(string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.localScale = Vector3.one * Size;
            go.GetComponent<Collider>().isTrigger = true;
            return go;
        }

        /// <summary>Authored prefabs may have no collider at all; interaction needs one, and a trigger keeps it from blocking movement.</summary>
        public static void EnsureCollider(GameObject go)
        {
            if (go.GetComponentInChildren<Collider>() != null)
            {
                return;
            }

            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
        }
    }
}
