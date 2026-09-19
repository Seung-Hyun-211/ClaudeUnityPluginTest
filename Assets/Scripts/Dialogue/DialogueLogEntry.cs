namespace Game.Dialogue
{
    public readonly struct DialogueLogEntry
    {
        public readonly string Speaker;
        public readonly string Text;

        public DialogueLogEntry(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }
}
