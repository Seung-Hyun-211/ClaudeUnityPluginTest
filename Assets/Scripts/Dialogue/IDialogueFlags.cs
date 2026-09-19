namespace Game.Dialogue
{
    /// <summary>Named boolean story flags that Branch nodes, Choice conditions and SetFlag events read/write.</summary>
    public interface IDialogueFlags
    {
        bool GetFlag(string name);
        void SetFlag(string name, bool value);
    }
}
