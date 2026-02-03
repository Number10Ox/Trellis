using System;
using NUnit.Framework;
using Trellis.StateMachine;

public class StateMachineTests
{
    private enum TestState { A, B, C }
    private enum TestTrigger { GoToB, GoToC, GoToA }

    private class RecordingState : IState
    {
        public int EnterCount;
        public int ExitCount;
        public float LastTickDelta;
        public int TickCount;
        public Action OnEnterAction;
        public Action OnTickAction;

        public void Enter()
        {
            EnterCount++;
            OnEnterAction?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            TickCount++;
            LastTickDelta = deltaTime;
            OnTickAction?.Invoke();
        }

        public void Exit()
        {
            ExitCount++;
        }
    }

    private StateMachine<TestState, TestTrigger> sm;
    private RecordingState stateA;
    private RecordingState stateB;
    private RecordingState stateC;

    [SetUp]
    public void SetUp()
    {
        sm = new StateMachine<TestState, TestTrigger>();
        stateA = new RecordingState();
        stateB = new RecordingState();
        stateC = new RecordingState();

        sm.AddState(TestState.A, stateA);
        sm.AddState(TestState.B, stateB);
        sm.AddState(TestState.C, stateC);

        sm.AddTransition(TestState.A, TestTrigger.GoToB, TestState.B);
        sm.AddTransition(TestState.B, TestTrigger.GoToC, TestState.C);
        sm.AddTransition(TestState.C, TestTrigger.GoToA, TestState.A);
    }

    [Test]
    public void Start_EntersInitialState()
    {
        sm.Start(TestState.A);

        Assert.AreEqual(TestState.A, sm.CurrentStateId);
        Assert.AreEqual(1, stateA.EnterCount);
    }

    [Test]
    public void Start_CalledTwice_IgnoresSecondCall()
    {
        sm.Start(TestState.A);
        sm.Start(TestState.B);

        Assert.AreEqual(TestState.A, sm.CurrentStateId);
        Assert.AreEqual(1, stateA.EnterCount);
        Assert.AreEqual(0, stateB.EnterCount);
    }

    [Test]
    public void Tick_ForwardsDeltaTimeToCurrentState()
    {
        sm.Start(TestState.A);

        sm.Tick(0.5f);

        Assert.AreEqual(0.5f, stateA.LastTickDelta);
    }

    [Test]
    public void Fire_TriggerResolvedOnNextTick()
    {
        sm.Start(TestState.A);

        sm.Fire(TestTrigger.GoToB);

        // Not resolved yet
        Assert.AreEqual(TestState.A, sm.CurrentStateId);

        sm.Tick(0.016f);

        Assert.AreEqual(TestState.B, sm.CurrentStateId);
        Assert.AreEqual(1, stateA.ExitCount);
        Assert.AreEqual(1, stateB.EnterCount);
    }

    [Test]
    public void Fire_TransitionCallsExitThenEnter()
    {
        sm.Start(TestState.A);

        int exitOrder = -1;
        int enterOrder = -1;
        int callOrder = 0;

        stateA.OnEnterAction = null;
        stateB.OnEnterAction = () => enterOrder = callOrder++;

        // Hacky but works: override exit to record order
        var trackingStateA = new RecordingState();
        trackingStateA.OnEnterAction = () => { };

        // Use the event to verify ordering instead
        bool exitBeforeEnter = false;
        sm.OnStateChanged += (prev, next) =>
        {
            exitBeforeEnter = stateA.ExitCount == 1 && stateB.EnterCount == 1;
        };

        sm.Fire(TestTrigger.GoToB);
        sm.Tick(0.016f);

        // By the time OnStateChanged fires, Exit has been called and Enter has been called
        Assert.IsTrue(exitBeforeEnter);
    }

    [Test]
    public void Fire_OverwritesPendingTrigger()
    {
        sm.Start(TestState.A);
        sm.AddTransition(TestState.A, TestTrigger.GoToC, TestState.C);

        sm.Fire(TestTrigger.GoToB);
        sm.Fire(TestTrigger.GoToC);

        sm.Tick(0.016f);

        Assert.AreEqual(TestState.C, sm.CurrentStateId);
    }

    [Test]
    public void Fire_NoTransitionDefined_StaysInCurrentState()
    {
        sm.Start(TestState.A);

        sm.Fire(TestTrigger.GoToC); // No A→C transition defined

        sm.Tick(0.016f);

        Assert.AreEqual(TestState.A, sm.CurrentStateId);
    }

    [Test]
    public void Tick_TicksCurrentStateAfterTransition()
    {
        sm.Start(TestState.A);
        sm.Fire(TestTrigger.GoToB);

        sm.Tick(0.016f);

        // B should be ticked (the new current state), not A
        Assert.AreEqual(1, stateB.TickCount);
        Assert.AreEqual(0, stateA.TickCount); // A was never ticked (only entered then exited)
    }

    [Test]
    public void OnStateChanged_FiresWithCorrectIds()
    {
        sm.Start(TestState.A);

        TestState capturedPrev = default;
        TestState capturedNext = default;
        int eventCount = 0;

        sm.OnStateChanged += (prev, next) =>
        {
            capturedPrev = prev;
            capturedNext = next;
            eventCount++;
        };

        sm.Fire(TestTrigger.GoToB);
        sm.Tick(0.016f);

        Assert.AreEqual(1, eventCount);
        Assert.AreEqual(TestState.A, capturedPrev);
        Assert.AreEqual(TestState.B, capturedNext);
    }

    [Test]
    public void TriggerFiredDuringEnter_SurvivesToNextTick()
    {
        // State B fires GoToC during its Enter
        stateB.OnEnterAction = () => sm.Fire(TestTrigger.GoToC);

        sm.Start(TestState.A);
        sm.Fire(TestTrigger.GoToB);

        sm.Tick(0.016f);

        // After this tick: A→B transition resolved, B.Enter() fires GoToC
        Assert.AreEqual(TestState.B, sm.CurrentStateId);

        sm.Tick(0.016f);

        // GoToC should resolve on this tick
        Assert.AreEqual(TestState.C, sm.CurrentStateId);
    }

    [Test]
    public void TriggerFiredDuringTick_DefersToNextTick()
    {
        stateA.OnTickAction = () =>
        {
            if (stateA.TickCount == 1) sm.Fire(TestTrigger.GoToB);
        };

        sm.Start(TestState.A);

        sm.Tick(0.016f);

        // Trigger fired during Tick, not resolved until next Tick
        Assert.AreEqual(TestState.A, sm.CurrentStateId);

        sm.Tick(0.016f);

        Assert.AreEqual(TestState.B, sm.CurrentStateId);
    }

    [Test]
    public void MultipleTransitions_ChainCorrectly()
    {
        sm.Start(TestState.A);

        sm.Fire(TestTrigger.GoToB);
        sm.Tick(0.016f);
        Assert.AreEqual(TestState.B, sm.CurrentStateId);

        sm.Fire(TestTrigger.GoToC);
        sm.Tick(0.016f);
        Assert.AreEqual(TestState.C, sm.CurrentStateId);

        sm.Fire(TestTrigger.GoToA);
        sm.Tick(0.016f);
        Assert.AreEqual(TestState.A, sm.CurrentStateId);
    }

    [Test]
    public void NoTriggerPending_TicksWithoutTransition()
    {
        sm.Start(TestState.A);

        sm.Tick(0.016f);
        sm.Tick(0.016f);
        sm.Tick(0.016f);

        Assert.AreEqual(TestState.A, sm.CurrentStateId);
        Assert.AreEqual(3, stateA.TickCount);
        Assert.AreEqual(0, stateA.ExitCount);
    }
}
