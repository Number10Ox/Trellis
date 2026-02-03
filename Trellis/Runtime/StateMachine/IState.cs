namespace Trellis.StateMachine
{
    // Lifecycle contract for states: Enter, Tick, Exit.
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
