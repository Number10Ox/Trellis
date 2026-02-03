namespace Trellis.Scheduling
{
    // Tick contract for systems driven by SystemScheduler.
    public interface ISystem
    {
        void Tick(float deltaTime);
    }
}
