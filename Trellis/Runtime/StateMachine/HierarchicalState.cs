using System;

namespace Trellis.StateMachine
{
    /// <summary>
    /// Base class for hierarchical states that contain a child state machine.
    /// Handles tick propagation to the child machine automatically.
    /// </summary>
    public abstract class HierarchicalState<TChildState, TChildTrigger> : IHierarchicalState<TChildState, TChildTrigger>
        where TChildState : struct, Enum
        where TChildTrigger : struct, Enum
    {
        private readonly StateMachine<TChildState, TChildTrigger> childMachine;
        private readonly TChildState initialChildState;
        private Action<TChildState> onBubbleUp;

        public StateMachine<TChildState, TChildTrigger> ChildMachine => childMachine;

        protected HierarchicalState(TChildState initialChildState)
        {
            this.initialChildState = initialChildState;
            childMachine = new StateMachine<TChildState, TChildTrigger>();
        }

        /// <summary>
        /// Registers a callback for when child states want to signal the parent.
        /// This enables explicit bubble-up from child to parent machine.
        /// </summary>
        public void SetBubbleUpCallback(Action<TChildState> callback)
        {
            onBubbleUp = callback;
        }

        /// <summary>
        /// Called by child states to signal the parent. The parent can then
        /// fire triggers on itself based on child state changes.
        /// </summary>
        protected void BubbleUp(TChildState childState)
        {
            onBubbleUp?.Invoke(childState);
        }

        /// <summary>
        /// Called when entering this hierarchical state. Starts the child machine.
        /// Override to add custom behavior, but call base.Enter().
        /// </summary>
        public virtual void Enter()
        {
            childMachine.Start(initialChildState);
        }

        /// <summary>
        /// Called each tick. Ticks the child machine.
        /// Override to add custom behavior, but call base.Tick().
        /// </summary>
        public virtual void Tick(float deltaTime)
        {
            childMachine.Tick(deltaTime);
        }

        /// <summary>
        /// Called when exiting this hierarchical state.
        /// Override to add custom cleanup.
        /// </summary>
        public virtual void Exit()
        {
        }

        /// <summary>
        /// Convenience method to add a state to the child machine.
        /// </summary>
        protected void AddChildState(TChildState id, IState state)
        {
            childMachine.AddState(id, state);
        }

        /// <summary>
        /// Convenience method to add a transition to the child machine.
        /// </summary>
        protected void AddChildTransition(TChildState from, TChildTrigger trigger, TChildState to)
        {
            childMachine.AddTransition(from, trigger, to);
        }

        /// <summary>
        /// Fires a trigger on the child machine.
        /// </summary>
        protected void FireChild(TChildTrigger trigger)
        {
            childMachine.Fire(trigger);
        }
    }
}
