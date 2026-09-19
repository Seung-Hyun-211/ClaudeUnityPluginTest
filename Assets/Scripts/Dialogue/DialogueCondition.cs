using System;

namespace Game.Dialogue
{
    /// <summary>"Flag X is (not) set" - the only condition kind so far. An empty flag name is always met.</summary>
    [Serializable]
    public struct DialogueCondition
    {
        public string flag;
        public bool expected;

        public bool IsMet(IDialogueFlags flags)
        {
            if (string.IsNullOrEmpty(flag))
            {
                return true;
            }

            return flags.GetFlag(flag) == expected;
        }
    }
}
