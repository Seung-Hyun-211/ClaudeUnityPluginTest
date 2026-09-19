using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Game.Items;
using Game.Items.Equipment;
using Game.Items.Grid;

namespace Game.Tests
{
    /// <summary>
    /// Builds game objects/assets in code and tears them down after each test.
    /// Private [SerializeField] values are set by reflection since these
    /// objects never go through the inspector; MonoBehaviour.Awake does not
    /// run in EditMode, so tests call it explicitly when a component needs it.
    /// </summary>
    public abstract class TestBase
    {
        private readonly List<UnityEngine.Object> created = new();

        [TearDown]
        public void DestroyCreated()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
            created.Clear();
        }

        protected T Track<T>(T obj) where T : UnityEngine.Object
        {
            created.Add(obj);
            return obj;
        }

        protected T NewAsset<T>() where T : ScriptableObject => Track(ScriptableObject.CreateInstance<T>());

        protected GameObject NewGameObject(string name = "test") => Track(new GameObject(name));

        protected T AddComponent<T>(GameObject go = null) where T : Component
        {
            return (go != null ? go : NewGameObject(typeof(T).Name)).AddComponent<T>();
        }

        protected ItemData MakeItem(string id, int width = 1, int height = 1, int maxStack = 1)
        {
            var item = NewAsset<ItemData>();
            Set(item, "itemId", id);
            Set(item, "displayName", id);
            Set(item, "gridWidth", width);
            Set(item, "gridHeight", height);
            Set(item, "maxStackSize", maxStack);
            item.name = id;
            return item;
        }

        protected ContainerItemData MakeContainer(string id, ContainerCategory category, GridShapeData shape)
        {
            var container = NewAsset<ContainerItemData>();
            Set(container, "itemId", id);
            Set(container, "category", category);
            Set(container, "shape", shape);
            container.name = id;
            return container;
        }

        protected GridShapeData MakeShape(int width, int height)
        {
            var shape = NewAsset<GridShapeData>();
            Set(shape, "width", width);
            Set(shape, "height", height);
            return shape;
        }

        protected GridInventory MakeGrid(int width, int height)
        {
            var grid = AddComponent<GridInventory>();
            grid.SetShape(MakeShape(width, height));
            return grid;
        }

        protected ItemDatabase MakeDatabase(params ItemData[] items)
        {
            var database = NewAsset<ItemDatabase>();
            Set(database, "items", items);
            return database;
        }

        protected static void Set(object target, string field, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (info != null)
                {
                    info.SetValue(target, value);
                    return;
                }
            }

            throw new MissingFieldException(target.GetType().Name, field);
        }

        protected static void Invoke(object target, string method)
        {
            var info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (info == null)
            {
                throw new MissingMethodException(target.GetType().Name, method);
            }
            info.Invoke(target, null);
        }
    }
}
