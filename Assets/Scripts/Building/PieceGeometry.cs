using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// Where one kind of piece sits in the world and how a point snaps to it.
    /// A new piece kind (stairs, ladder) brings its own geometry and registers
    /// it in <see cref="PieceGeometries"/>; BuildGrid and everything using it
    /// stay as they are.
    /// </summary>
    public interface IPieceGeometry
    {
        Vector3 Size { get; }

        /// <summary>Snaps a point (in cell units from the zone origin) at a level to the nearest piece of this kind.</summary>
        PieceKey KeyAt(PieceKind kind, float cellX, float cellZ, int level);

        Vector3 Center(PieceKey key, Vector3 origin);

        Quaternion Rotation(PieceKey key);
    }

    /// <summary>A floor slab filling one cell, top flush with the level's floor height.</summary>
    public sealed class FloorGeometry : IPieceGeometry
    {
        public Vector3 Size => new(BuildGrid.CellSize, BuildGrid.SlabThickness, BuildGrid.CellSize);

        public PieceKey KeyAt(PieceKind kind, float cellX, float cellZ, int level) =>
            new(kind, Mathf.FloorToInt(cellX), Mathf.FloorToInt(cellZ), level);

        public Vector3 Center(PieceKey key, Vector3 origin) =>
            new(origin.x + (key.X + 0.5f) * BuildGrid.CellSize,
                BuildGrid.TopOf(key.Level, origin.y) - BuildGrid.SlabThickness * 0.5f,
                origin.z + (key.Z + 0.5f) * BuildGrid.CellSize);

        public Quaternion Rotation(PieceKey key) => Quaternion.identity;
    }

    /// <summary>A wall or door standing on a grid line. Authored along X; edges along Z are turned 90 degrees.</summary>
    public sealed class EdgeGeometry : IPieceGeometry
    {
        public Vector3 Size => new(BuildGrid.CellSize, BuildGrid.WallHeight, BuildGrid.WallThickness);

        public PieceKey KeyAt(PieceKind kind, float cellX, float cellZ, int level)
        {
            var (x, z, axis) = BuildGrid.EdgeAt(cellX, cellZ);
            return new PieceKey(kind, x, z, level, axis);
        }

        public Vector3 Center(PieceKey key, Vector3 origin)
        {
            float y = BuildGrid.TopOf(key.Level, origin.y) + BuildGrid.WallHeight * 0.5f;
            return key.Axis == Axis.X
                ? new Vector3(origin.x + (key.X + 0.5f) * BuildGrid.CellSize, y, origin.z + key.Z * BuildGrid.CellSize)
                : new Vector3(origin.x + key.X * BuildGrid.CellSize, y, origin.z + (key.Z + 0.5f) * BuildGrid.CellSize);
        }

        public Quaternion Rotation(PieceKey key) => key.Axis == Axis.Z ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
    }

    /// <summary>The corner post at a grid vertex. Made automatically, so it cannot be aimed at.</summary>
    public sealed class PillarGeometry : IPieceGeometry
    {
        public Vector3 Size => new(BuildGrid.PillarWidth, BuildGrid.WallHeight, BuildGrid.PillarWidth);

        public PieceKey KeyAt(PieceKind kind, float cellX, float cellZ, int level) =>
            throw new ArgumentException("Pillars are made automatically and cannot be aimed at.", nameof(kind));

        public Vector3 Center(PieceKey key, Vector3 origin) =>
            new(origin.x + key.X * BuildGrid.CellSize,
                BuildGrid.TopOf(key.Level, origin.y) + BuildGrid.WallHeight * 0.5f,
                origin.z + key.Z * BuildGrid.CellSize);

        public Quaternion Rotation(PieceKey key) => Quaternion.identity;
    }

    /// <summary>Which geometry each piece kind uses.</summary>
    public sealed class PieceGeometries
    {
        private readonly Dictionary<PieceKind, IPieceGeometry> geometries = new();

        /// <summary>Shared registry with the stage-1 kinds; BuildGrid reads it.</summary>
        public static PieceGeometries Default { get; } = CreateDefault();

        /// <summary>A fresh registry with the built-ins, for callers (tests) that must not touch Default.</summary>
        public static PieceGeometries CreateDefault()
        {
            var registry = new PieceGeometries();
            var edge = new EdgeGeometry();
            registry.Register(PieceKind.Floor, new FloorGeometry());
            registry.Register(PieceKind.Wall, edge);
            registry.Register(PieceKind.Door, edge);
            registry.Register(PieceKind.Pillar, new PillarGeometry());
            return registry;
        }

        public void Register(PieceKind kind, IPieceGeometry geometry)
        {
            geometries[kind] = geometry ?? throw new ArgumentNullException(nameof(geometry));
        }

        public IPieceGeometry For(PieceKind kind)
        {
            if (!geometries.TryGetValue(kind, out var geometry))
            {
                throw new ArgumentException($"No geometry registered for {kind}.", nameof(kind));
            }

            return geometry;
        }
    }
}
