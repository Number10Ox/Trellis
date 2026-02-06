using System;
using System.Collections.Generic;
using Trellis.Scheduling;

namespace Trellis.Timing
{
    /// <summary>
    /// Manages scheduled timers. Implements ISystem to be driven by SystemScheduler.
    /// Timer objects are pooled internally to avoid allocations.
    /// </summary>
    public class TimerManager : ISystem
    {
        private readonly List<Timer> activeTimers = new();
        private readonly Stack<Timer> timerPool = new();
        private readonly List<Timer> pendingAdditions = new();
        private readonly List<Timer> pendingRemovals = new();
        private bool isTicking;

        /// <summary>
        /// Number of currently active timers.
        /// </summary>
        public int ActiveCount => activeTimers.Count;

        /// <summary>
        /// Number of pooled timer objects available for reuse.
        /// </summary>
        public int PooledCount => timerPool.Count;

        /// <summary>
        /// Schedules a one-shot timer that fires after the specified delay.
        /// </summary>
        public ITimerHandle Schedule(float delay, Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (delay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be non-negative.");
            }

            var timer = AcquireTimer();
            timer.Initialize(delay, callback, false, 0f);
            AddTimer(timer);

            return timer;
        }

        /// <summary>
        /// Schedules a repeating timer that fires at the specified interval.
        /// </summary>
        public ITimerHandle ScheduleRepeating(float interval, Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (interval <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive.");
            }

            var timer = AcquireTimer();
            timer.Initialize(interval, callback, true, interval);
            AddTimer(timer);

            return timer;
        }

        /// <summary>
        /// Schedules a repeating timer with an initial delay different from the interval.
        /// </summary>
        public ITimerHandle ScheduleRepeating(float initialDelay, float interval, Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (initialDelay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialDelay), "Initial delay must be non-negative.");
            }

            if (interval <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive.");
            }

            var timer = AcquireTimer();
            timer.Initialize(initialDelay, callback, true, interval);
            AddTimer(timer);

            return timer;
        }

        /// <summary>
        /// Cancels all active timers.
        /// </summary>
        public void CancelAll()
        {
            for (int i = activeTimers.Count - 1; i >= 0; i--)
            {
                var timer = activeTimers[i];
                timer.Cancel();
                ReturnTimer(timer);
            }

            activeTimers.Clear();

            for (int i = pendingAdditions.Count - 1; i >= 0; i--)
            {
                var timer = pendingAdditions[i];
                timer.Cancel();
                ReturnTimer(timer);
            }

            pendingAdditions.Clear();
        }

        /// <summary>
        /// Advances all timers by deltaTime. Called by SystemScheduler.
        /// </summary>
        public void Tick(float deltaTime)
        {
            // Add pending timers
            for (int i = 0; i < pendingAdditions.Count; i++)
            {
                activeTimers.Add(pendingAdditions[i]);
            }
            pendingAdditions.Clear();

            isTicking = true;

            for (int i = 0; i < activeTimers.Count; i++)
            {
                var timer = activeTimers[i];
                if (!timer.IsActive)
                {
                    pendingRemovals.Add(timer);
                    continue;
                }

                timer.TimeRemaining -= deltaTime;

                if (timer.TimeRemaining <= 0)
                {
                    timer.Callback?.Invoke();

                    if (timer.IsRepeating && timer.IsActive)
                    {
                        timer.TimeRemaining += timer.Interval;
                    }
                    else
                    {
                        timer.MarkInactive();
                        pendingRemovals.Add(timer);
                    }
                }
            }

            isTicking = false;

            // Remove and pool completed/cancelled timers
            for (int i = 0; i < pendingRemovals.Count; i++)
            {
                var timer = pendingRemovals[i];
                activeTimers.Remove(timer);
                ReturnTimer(timer);
            }
            pendingRemovals.Clear();
        }

        private Timer AcquireTimer()
        {
            if (timerPool.Count > 0)
            {
                return timerPool.Pop();
            }

            return new Timer(OnTimerCancelled);
        }

        private void ReturnTimer(Timer timer)
        {
            timer.Reset();
            timerPool.Push(timer);
        }

        private void AddTimer(Timer timer)
        {
            if (isTicking)
            {
                pendingAdditions.Add(timer);
            }
            else
            {
                activeTimers.Add(timer);
            }
        }

        private void OnTimerCancelled(Timer timer)
        {
            // Timer will be removed and pooled on next tick
        }
    }

    /// <summary>
    /// Internal timer class. Not exposed publicly - use ITimerHandle.
    /// </summary>
    internal class Timer : ITimerHandle
    {
        private readonly Action<Timer> onCancelled;
        private bool isActive;

        public float TimeRemaining;
        public float Interval;
        public bool IsRepeating;
        public Action Callback;

        public bool IsActive => isActive;

        public Timer(Action<Timer> onCancelled)
        {
            this.onCancelled = onCancelled;
        }

        public void Initialize(float delay, Action callback, bool isRepeating, float interval)
        {
            TimeRemaining = delay;
            Callback = callback;
            IsRepeating = isRepeating;
            Interval = interval;
            isActive = true;
        }

        public void Cancel()
        {
            if (isActive)
            {
                isActive = false;
                onCancelled?.Invoke(this);
            }
        }

        public void MarkInactive()
        {
            isActive = false;
        }

        public void Reset()
        {
            TimeRemaining = 0;
            Interval = 0;
            IsRepeating = false;
            Callback = null;
            isActive = false;
        }
    }
}
