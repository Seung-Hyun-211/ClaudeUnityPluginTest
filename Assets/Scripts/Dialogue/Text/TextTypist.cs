using System.Collections.Generic;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>
    /// The typing clock of a line: how many characters are visible after a
    /// given time, honouring the pauses from the markup. Pure logic, no UI.
    /// </summary>
    public sealed class TextTypist
    {
        private IReadOnlyList<TextPause> pauses = System.Array.Empty<TextPause>();
        private int total;
        private int nextPause;
        private float shown;
        private float pauseLeft;

        public int VisibleCount => Mathf.Min(total, Mathf.FloorToInt(shown));

        public bool IsDone => shown >= total && nextPause >= pauses.Count && pauseLeft <= 0f;

        public void Reset(int totalCharacters, IReadOnlyList<TextPause> linePauses)
        {
            total = Mathf.Max(0, totalCharacters);
            pauses = linePauses ?? System.Array.Empty<TextPause>();
            nextPause = 0;
            shown = 0f;
            pauseLeft = 0f;
        }

        public void Complete()
        {
            shown = total;
            nextPause = pauses.Count;
            pauseLeft = 0f;
        }

        public void Advance(float deltaSeconds, float charactersPerSecond)
        {
            if (charactersPerSecond <= 0f || IsDone)
            {
                return;
            }

            while (true)
            {
                if (pauseLeft > 0f)
                {
                    if (deltaSeconds <= pauseLeft)
                    {
                        pauseLeft -= deltaSeconds;
                        return;
                    }

                    deltaSeconds -= pauseLeft;
                    pauseLeft = 0f;
                }

                bool hasPause = nextPause < pauses.Count;
                float limit = hasPause ? Mathf.Clamp(pauses[nextPause].Index, 0, total) : total;
                float target = shown + charactersPerSecond * deltaSeconds;
                if (target < limit)
                {
                    shown = target;
                    return;
                }

                deltaSeconds = Mathf.Max(0f, deltaSeconds - Mathf.Max(0f, limit - shown) / charactersPerSecond);
                shown = limit;
                if (!hasPause)
                {
                    return;
                }

                pauseLeft = pauses[nextPause].Seconds;
                nextPause++;
            }
        }
    }
}
