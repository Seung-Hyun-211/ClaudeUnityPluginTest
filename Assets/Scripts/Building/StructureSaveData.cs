using System;
using System.Collections.Generic;

namespace Game.Building
{
    /// <summary>One saved piece. Coordinates make supports implicit, so no references are stored.</summary>
    [Serializable]
    public class PieceRecord
    {
        public string pieceId;
        public int kind;
        public int x;
        public int z;
        public int level;
        public int axis;
        public float health;
        public bool doorOpen;
        public bool doorFlipped;

        public PieceKey ToKey() => new((PieceKind)kind, x, z, level, (Axis)axis);
    }

    [Serializable]
    public class SceneStructures
    {
        public string sceneId;
        public List<PieceRecord> pieces = new();
    }

    /// <summary>Everything built, for every scene: what StructureRepository saves.</summary>
    [Serializable]
    public class StructureSaveData
    {
        public List<SceneStructures> scenes = new();

        public SceneStructures Find(string sceneId)
        {
            foreach (var scene in scenes)
            {
                if (scene.sceneId == sceneId)
                {
                    return scene;
                }
            }

            return null;
        }

        /// <summary>Replaces one scene's pieces (an empty list removes its entry).</summary>
        public void Set(string sceneId, List<PieceRecord> pieces)
        {
            scenes.RemoveAll(s => s.sceneId == sceneId);
            if (pieces != null && pieces.Count > 0)
            {
                scenes.Add(new SceneStructures { sceneId = sceneId, pieces = pieces });
            }
        }
    }
}
