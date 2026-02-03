namespace Trellis.Scheduling
{
    // Ticks an ordered ISystem array sequentially. Deterministic execution order.
    public class SystemScheduler
    {
        private readonly ISystem[] systems;

        public SystemScheduler(ISystem[] systems)
        {
            this.systems = systems ?? System.Array.Empty<ISystem>();
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i]?.Tick(deltaTime);
            }
        }
    }
}
