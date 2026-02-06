using System;

namespace Trellis.StateMachine
{
    /// <summary>
    /// A state that contains a child state machine. Extends IState with access to the nested machine.
    /// </summary>
    public interface IHierarchicalState<TChildState, TChildTrigger> : IState
        where TChildState : struct, Enum
        where TChildTrigger : struct, Enum
    {
        /// <summary>
        /// The nested child state machine. Returns null if not initialized.
        /// </summary>
        StateMachine<TChildState, TChildTrigger> ChildMachine { get; }
    }
}
