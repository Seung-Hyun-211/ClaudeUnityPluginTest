using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>
    /// Drives the per-glyph effects of one TMP_Text: every frame it rebuilds
    /// the mesh and lets each span's effect move/recolour the glyphs it covers.
    /// Does nothing for lines without effects. The effects themselves know
    /// nothing about TextMeshPro (see ITextEffect).
    /// </summary>
    public class DialogueTextAnimator : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        [Tooltip("0 turns the movement effects (sway/wave/shake) off and keeps colours; 1 is full strength.")]
        [SerializeField, Range(0f, 1f)] private float motionScale = 1f;

        private IReadOnlyList<TextSpan> spans;

        public float MotionScale
        {
            get => motionScale;
            set => motionScale = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }
        }

        /// <summary>Sets the effect spans of the text currently on the label (null or empty = no effects).</summary>
        public void SetSpans(IReadOnlyList<TextSpan> value)
        {
            spans = value != null && value.Count > 0 ? value : null;
        }

        private void LateUpdate()
        {
            if (spans == null || text == null)
            {
                return;
            }

            // Regenerates the mesh from the text, so effects always start from the untouched glyphs.
            text.ForceMeshUpdate();
            var info = text.textInfo;
            int visible = Mathf.Min(info.characterCount, text.maxVisibleCharacters);
            float time = Time.unscaledTime;

            for (int i = 0; i < visible; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                var style = new GlyphStyle { Color = character.color };
                foreach (var span in spans)
                {
                    if (span.Covers(i))
                    {
                        span.Effect.Apply(new GlyphContext(i, i - span.Start, span.Length, time), ref style);
                    }
                }

                var mesh = info.meshInfo[character.materialReferenceIndex];
                Vector3 offset = (Vector3)(style.Offset * motionScale);
                int vertex = character.vertexIndex;
                for (int k = 0; k < 4; k++)
                {
                    mesh.vertices[vertex + k] += offset;
                    mesh.colors32[vertex + k] = style.Color;
                }
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
