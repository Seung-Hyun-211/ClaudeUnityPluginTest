namespace Game.AI
{
    public interface IAiState
    {
        void Enter(AiContext context);
        void Tick(AiContext context, float deltaTime);
        void Exit(AiContext context);
    }
}
