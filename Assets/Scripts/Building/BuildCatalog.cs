using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// Which BuildPieceData each key-bound category uses, plus the corner
    /// pillar. It is pure data: a category without an entry (stairs and ladder
    /// in stage 1) is unavailable, and a new kind of piece is a new entry - no
    /// code changes here.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Building/Catalog", fileName = "BuildCatalog")]
    public class BuildCatalog : ScriptableObject
    {
        [Serializable]
        public struct CategoryEntry
        {
            public BuildCategory category;
            public BuildPieceData data;
        }

        [SerializeField] private CategoryEntry[] entries = Array.Empty<CategoryEntry>();

        [Tooltip("The corner post made automatically where walls meet.")]
        [SerializeField] private BuildPieceData pillar;

        /// <summary>The piece for a category, or null when that category is not available yet.</summary>
        public BuildPieceData Get(BuildCategory category)
        {
            foreach (var entry in entries)
            {
                if (entry.category == category && entry.data != null)
                {
                    return entry.data;
                }
            }

            return null;
        }

        public bool IsAvailable(BuildCategory category) => Get(category) != null;

        /// <summary>The data for a piece kind (what a saved or placed piece is made from).</summary>
        public BuildPieceData ForKind(PieceKind kind)
        {
            if (pillar != null && pillar.Kind == kind)
            {
                return pillar;
            }

            foreach (var entry in entries)
            {
                if (entry.data != null && entry.data.Kind == kind)
                {
                    return entry.data;
                }
            }

            return null;
        }

        /// <summary>Configuration mistakes: two entries of the same kind (placed pieces are found by kind, so the second would never be used), a missing prefab, a pillar in a category.</summary>
        public IReadOnlyList<string> FindProblems()
        {
            var problems = new List<string>();
            var kinds = new HashSet<PieceKind>();
            var categories = new HashSet<BuildCategory>();

            foreach (var entry in entries)
            {
                if (entry.data == null)
                {
                    problems.Add($"{entry.category}: no piece data.");
                    continue;
                }

                if (entry.data.Prefab == null)
                {
                    problems.Add($"{entry.category}: '{entry.data.name}' has no prefab.");
                }

                if (entry.data.Kind == PieceKind.Pillar)
                {
                    problems.Add($"{entry.category}: pillars are made automatically - use the pillar slot.");
                }

                if (!categories.Add(entry.category))
                {
                    problems.Add($"{entry.category} appears twice.");
                }

                if (!kinds.Add(entry.data.Kind))
                {
                    problems.Add($"{entry.category}: another entry already uses kind {entry.data.Kind}; pieces are looked up by kind, so variants of one kind are not supported.");
                }
            }

            if (pillar != null && pillar.Kind != PieceKind.Pillar)
            {
                problems.Add($"The pillar slot holds '{pillar.name}' of kind {pillar.Kind}.");
            }

            return problems;
        }

        private void OnValidate()
        {
            foreach (var problem in FindProblems())
            {
                Debug.LogWarning($"BuildCatalog '{name}': {problem}", this);
            }
        }

        public BuildPieceData Find(string pieceId)
        {
            if (pillar != null && pillar.PieceId == pieceId)
            {
                return pillar;
            }

            foreach (var entry in entries)
            {
                if (entry.data != null && entry.data.PieceId == pieceId)
                {
                    return entry.data;
                }
            }

            return null;
        }
    }
}
