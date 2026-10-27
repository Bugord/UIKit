using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UIKit.Tests
{
    public class CommandTests
    {
        [Test]
        public void Command_Execute_RunsTheAction()
        {
            var calls = 0;
            var command = new Command(() => calls++);

            command.Execute();

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Command_PredicateIsFalse_CannotExecuteAndIgnoresExecute()
        {
            var calls = 0;
            var command = new Command(() => calls++, () => false);

            command.Execute();

            Assert.That(command.CanExecute, Is.False);
            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void Command_WithoutAPredicate_CanExecute()
        {
            Assert.That(new Command(() => { }).CanExecute, Is.True);
        }

        [Test]
        public void Command_RaiseCanExecuteChanged_RaisesTheEvent()
        {
            var command = new Command(() => { });
            var raised = 0;
            command.CanExecuteChanged += () => raised++;

            command.RaiseCanExecuteChanged();

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void Command_NullAction_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Command(null));
        }

        [Test]
        public void AsyncCommand_NullAction_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new AsyncCommand(null));
        }

        [Test]
        public void AsyncCommand_WhileRunning_IsRunningAndCannotExecute()
        {
            var hold = new AwaitableCompletionSource();
            var command = new AsyncCommand(() => hold.Awaitable);

            var run = command.ExecuteAsync();

            Assert.That(command.IsRunning, Is.True);
            Assert.That(command.CanExecute, Is.False);

            hold.SetResult();
            AwaitableAssert.Completes(run);
        }

        [Test]
        public void AsyncCommand_CallWhileRunning_IsIgnoredAndCompletesAtOnce()
        {
            var hold = new AwaitableCompletionSource();
            var calls = 0;
            var command = new AsyncCommand(() => {
                calls++;
                return hold.Awaitable;
            });
            var first = command.ExecuteAsync();

            var second = command.ExecuteAsync();

            AwaitableAssert.Completes(second);
            Assert.That(calls, Is.EqualTo(1));

            hold.SetResult();
            AwaitableAssert.Completes(first);
        }

        [Test]
        public void AsyncCommand_IgnoredCalls_ReturnSeparateAwaitables()
        {
            var command = new AsyncCommand(() => Awaitables.Completed(), () => false);

            var first = command.ExecuteAsync();
            var second = command.ExecuteAsync();

            // Each may be awaited once, so a shared instance would break the second caller.
            Assert.That(second, Is.Not.SameAs(first));
            AwaitableAssert.Completes(first);
            AwaitableAssert.Completes(second);
        }

        [Test]
        public void AsyncCommand_Finished_CanExecuteAgain()
        {
            var hold = new AwaitableCompletionSource();
            var command = new AsyncCommand(() => hold.Awaitable);
            var run = command.ExecuteAsync();

            hold.SetResult();
            AwaitableAssert.Completes(run);

            Assert.That(command.IsRunning, Is.False);
            Assert.That(command.CanExecute, Is.True);
        }

        [Test]
        public void AsyncCommand_Run_RaisesCanExecuteChangedOnStartAndFinish()
        {
            var hold = new AwaitableCompletionSource();
            var command = new AsyncCommand(() => hold.Awaitable);
            var states = new List<bool>();
            command.CanExecuteChanged += () => states.Add(command.CanExecute);

            var run = command.ExecuteAsync();
            hold.SetResult();
            AwaitableAssert.Completes(run);

            Assert.That(states, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void AsyncCommand_ActionFails_ResetsIsRunningAndFaults()
        {
            var hold = new AwaitableCompletionSource();
            var command = new AsyncCommand(() => hold.Awaitable);
            var run = command.ExecuteAsync();

            hold.SetException(new InvalidOperationException("boom"));

            AwaitableAssert.Faults<InvalidOperationException>(run);
            Assert.That(command.IsRunning, Is.False);
            Assert.That(command.CanExecute, Is.True);
        }

        [Test]
        public void AsyncCommand_ActionThrowsSynchronously_ResetsIsRunningAndFaults()
        {
            var command = new AsyncCommand(() => throw new InvalidOperationException("boom"));

            var run = command.ExecuteAsync();

            AwaitableAssert.Faults<InvalidOperationException>(run);
            Assert.That(command.IsRunning, Is.False);
        }

        [Test]
        public void AsyncCommand_ActionCancelled_ResetsIsRunning()
        {
            var hold = new AwaitableCompletionSource();
            var command = new AsyncCommand(() => hold.Awaitable);
            var run = command.ExecuteAsync();

            hold.SetCanceled();

            AwaitableAssert.Faults<OperationCanceledException>(run);
            Assert.That(command.IsRunning, Is.False);
            Assert.That(command.CanExecute, Is.True);
        }

        [Test]
        public void AsyncCommand_PredicateIsFalse_DoesNotRun()
        {
            var calls = 0;
            var command = new AsyncCommand(() => {
                calls++;
                return Awaitables.Completed();
            }, () => false);

            AwaitableAssert.Completes(command.ExecuteAsync());

            Assert.That(command.CanExecute, Is.False);
            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void AsyncCommand_Execute_RunsWithoutWaiting()
        {
            var hold = new AwaitableCompletionSource();
            var command = new AsyncCommand(() => hold.Awaitable);

            command.Execute();

            Assert.That(command.IsRunning, Is.True);
            hold.SetResult();
            Assert.That(command.IsRunning, Is.False);
        }

        [Test]
        public void CommandGate_OneCommandRunning_BlocksTheOthers()
        {
            var gate = new CommandGate();
            var hold = new AwaitableCompletionSource();
            var otherCalls = 0;
            var first = new AsyncCommand(() => hold.Awaitable, gate);
            var second = new AsyncCommand(() => {
                otherCalls++;
                return Awaitables.Completed();
            }, gate);

            var run = first.ExecuteAsync();
            AwaitableAssert.Completes(second.ExecuteAsync());

            Assert.That(gate.IsBusy, Is.True);
            Assert.That(second.CanExecute, Is.False);
            Assert.That(second.IsRunning, Is.False);
            Assert.That(otherCalls, Is.Zero);

            hold.SetResult();
            AwaitableAssert.Completes(run);
        }

        [Test]
        public void CommandGate_RunFinishes_UnblocksEveryCommand()
        {
            var gate = new CommandGate();
            var hold = new AwaitableCompletionSource();
            var first = new AsyncCommand(() => hold.Awaitable, gate);
            var second = new AsyncCommand(() => Awaitables.Completed(), gate);
            var run = first.ExecuteAsync();

            hold.SetResult();
            AwaitableAssert.Completes(run);

            Assert.That(gate.IsBusy, Is.False);
            Assert.That(first.CanExecute, Is.True);
            Assert.That(second.CanExecute, Is.True);
        }

        [Test]
        public void CommandGate_Run_RaisesCanExecuteChangedOnEveryCommand()
        {
            var gate = new CommandGate();
            var hold = new AwaitableCompletionSource();
            var first = new AsyncCommand(() => hold.Awaitable, gate);
            var second = new AsyncCommand(() => Awaitables.Completed(), gate);
            var firstStates = new List<bool>();
            var secondStates = new List<bool>();
            first.CanExecuteChanged += () => firstStates.Add(first.CanExecute);
            second.CanExecuteChanged += () => secondStates.Add(second.CanExecute);

            var run = first.ExecuteAsync();
            hold.SetResult();
            AwaitableAssert.Completes(run);

            Assert.That(firstStates, Is.EqualTo(new[] { false, true }));
            Assert.That(secondStates, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void CommandGate_FailedRun_ReleasesTheGate()
        {
            var gate = new CommandGate();
            var command = new AsyncCommand(() => throw new InvalidOperationException("boom"), gate);

            AwaitableAssert.Faults<InvalidOperationException>(command.ExecuteAsync());

            Assert.That(gate.IsBusy, Is.False);
        }
    }
}
