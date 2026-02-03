using System;
using System.Collections.Generic;
using UnityEngine;

namespace Trellis.StateMachine
{
    // Generic trigger-based state machine. Owns state registry and transition table.
    // Resolves one pending trigger per tick, manages Enter/Exit/Tick lifecycle.
    public class StateMachine<TState, TTrigger>
        where TState : struct, Enum
        where TTrigger : struct, Enum
    {
        private readonly struct StateAndTrigger : IEquatable<StateAndTrigger>
        {
            public readonly TState State;
            public readonly TTrigger Trigger;

            public StateAndTrigger(TState state, TTrigger trigger)
            {
                State = state;
                Trigger = trigger;
            }

            public bool Equals(StateAndTrigger other)
            {
                return EqualityComparer<TState>.Default.Equals(State, other.State)
                    && EqualityComparer<TTrigger>.Default.Equals(Trigger, other.Trigger);
            }

            public override bool Equals(object obj)
            {
                return obj is StateAndTrigger other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    const int HASH_PRIME = 397;
                    return (EqualityComparer<TState>.Default.GetHashCode(State) * HASH_PRIME)
                        ^ EqualityComparer<TTrigger>.Default.GetHashCode(Trigger);
                }
            }
        }

        private readonly Dictionary<TState, IState> states = new();
        private readonly Dictionary<StateAndTrigger, TState> transitions = new();
        private IState currentState;
        private TState currentStateId;
        private TTrigger? pendingTrigger;
        private bool started;

        public TState CurrentStateId => currentStateId;

        public event Action<TState, TState> OnStateChanged;

        public void AddState(TState id, IState state)
        {
            if (states.ContainsKey(id))
            {
                Debug.LogWarning($"Duplicate state {id} registered. Overwriting.");
            }
            states[id] = state;
        }

        public void AddTransition(TState from, TTrigger trigger, TState to)
        {
            var key = new StateAndTrigger(from, trigger);
            if (transitions.ContainsKey(key))
            {
                Debug.LogWarning($"Duplicate transition: ({from}, {trigger}) already registered. Overwriting.");
            }
            transitions[key] = to;
        }

        public void Start(TState initialState)
        {
            if (started)
            {
                Debug.LogWarning("StateMachine.Start() called more than once. Ignoring.");
                return;
            }

            if (!states.TryGetValue(initialState, out var initialStateInstance))
            {
                Debug.LogError($"StateMachine.Start(): No state registered for {initialState}.");
                return;
            }

            started = true;
            currentStateId = initialState;
            currentState = initialStateInstance;
            currentState.Enter();
        }

        // Queues a trigger for resolution on the next Tick. Only one pending trigger
        // is held at a time; if Fire is called again before Tick resolves, the previous
        // trigger is overwritten. Triggers fired during a state's Tick are deferred to
        // the following frame.
        public void Fire(TTrigger trigger)
        {
            if (!started)
            {
                Debug.LogWarning($"StateMachine.Fire() called before Start(). Ignoring trigger {trigger}.");
                return;
            }

            if (pendingTrigger.HasValue)
            {
                Debug.LogWarning($"Trigger {pendingTrigger.Value} overwritten by {trigger} before resolution.");
            }
            pendingTrigger = trigger;
        }

        public void Tick(float deltaTime)
        {
            if (!started)
            {
                Debug.LogWarning("StateMachine.Tick() called before Start(). Ignoring.");
                return;
            }

            if (pendingTrigger.HasValue)
            {
                var trigger = pendingTrigger.Value;
                pendingTrigger = null;
                ResolveTrigger(trigger);
            }

            currentState.Tick(deltaTime);
        }

        private void ResolveTrigger(TTrigger trigger)
        {
            var key = new StateAndTrigger(currentStateId, trigger);
            if (!transitions.TryGetValue(key, out TState destination))
            {
                Debug.LogWarning($"No transition for ({currentStateId}, {trigger}). Ignoring.");
                return;
            }

            if (!states.TryGetValue(destination, out var destinationState))
            {
                Debug.LogError($"StateMachine: Transition ({currentStateId}, {trigger}) leads to {destination}, but no state is registered for it.");
                return;
            }

            TState previousStateId = currentStateId;
            currentState.Exit();

            currentStateId = destination;
            currentState = destinationState;
            currentState.Enter();

            OnStateChanged?.Invoke(previousStateId, destination);
        }
    }
}
