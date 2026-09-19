using UnityEngine;
using Game.Items;

namespace Game.Weapons
{
    /// <summary>
    /// Puts a weapon into the world as a WeaponPickup holding a live instance,
    /// so a modded rifle with a half-empty magazine stays that way when picked
    /// up. Given only weapon data (no instance), it builds a fresh default one
    /// (no parts, no magazine). Registered with the WorldItemFactory
    /// automatically at startup (see WeaponWorldSpawnerRegistration), which
    /// keeps the dependency pointing Weapons -> Items.
    /// </summary>
    public class WeaponWorldSpawner : IWorldItemSpawner
    {
        public bool CanSpawn(in WorldSpawnRequest request)
        {
            return request.State is IWeapon || request.Item is FirearmData || request.Item is MeleeWeaponData;
        }

        public GameObject Spawn(in WorldSpawnRequest request, Vector3 position)
        {
            var weapon = request.State as IWeapon ?? CreateDefaultInstance(request.Item);
            if (weapon == null)
            {
                return null;
            }

            var prefab = weapon.Item.WorldPrefab;
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                go = DefaultWorldVisual.Create(weapon.Item.DisplayName);
                go.transform.position = position;
            }

            if (!go.TryGetComponent(out WeaponPickup pickup))
            {
                DefaultWorldVisual.EnsureCollider(go);
                pickup = go.AddComponent<WeaponPickup>();
            }

            pickup.Initialize(weapon);
            return go;
        }

        private static IWeapon CreateDefaultInstance(ItemData item)
        {
            return item switch
            {
                FirearmData firearm => new FirearmInstance(firearm),
                MeleeWeaponData melee => new MeleeWeaponInstance(melee),
                _ => null
            };
        }
    }

    internal static class WeaponWorldSpawnerRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => WorldItemFactory.RegisterGlobalSpawner(new WeaponWorldSpawner());
    }
}
