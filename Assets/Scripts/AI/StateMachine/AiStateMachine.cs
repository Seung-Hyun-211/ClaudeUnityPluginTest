namespace Game.AI
{
    /// <summary>
    /// Knows nothing about which states exist or when to transition between
    /// them — only that a change is Exit(old) -> swap -> Enter(new). All
    /// transition conditions live in each IAiState's Tick (see ai-state-machine.md).
    /// Initialize(context) must be called once before Tick/ChangeState are used.
    /// </summary>
    public class AiStateMachine
    {
        private AiContext context;

        public IAiState CurrentState { get; private set; }

        public void Initialize(AiContext context)
        {
            this.context = context;
        }

        public void ChangeState(IAiState next)
        {
            CurrentState?.Exit(context);
            CurrentState = next;
            CurrentState?.Enter(context);
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Tick(context, deltaTime);
        }
    }
}
