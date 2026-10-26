using NUnit.Framework;

namespace UIKit.Tests
{
    public class NavigationGateTests
    {
        private double now;
        private NavigationGate gate;

        [SetUp]
        public void SetUp()
        {
            now = 100;

            // Zero while queueing keeps the frame-based watchdog from starting; tests that check
            // for a stall raise it before calling CheckStall.
            gate = new NavigationGate(() => now) { StallWarningSeconds = 0 };
        }

        [TearDown]
        public void TearDown()
        {
            while (gate.IsHeld) {
                gate.Release();
            }
        }

        [Test]
        public void WaitAsync_GateFree_CompletesAtOnce()
        {
            AwaitableAssert.Completes(gate.WaitAsync("open A"));

            Assert.That(gate.IsHeld, Is.True);
        }

        [Test]
        public void WaitAsync_GateHeld_WaitsForRelease()
        {
            AwaitableAssert.Completes(gate.WaitAsync("open A"));

            var second = gate.WaitAsync("open B");
            Assert.That(second.GetAwaiter().IsCompleted, Is.False);

            gate.Release();
            AwaitableAssert.Completes(second);
        }

        [Test]
        public void Release_ServesWaitersFirstInFirstOut()
        {
            AwaitableAssert.Completes(gate.WaitAsync("open A"));
            var first = gate.WaitAsync("open B");
            var second = gate.WaitAsync("open C");

            gate.Release();

            Assert.That(first.GetAwaiter().IsCompleted, Is.True);
            Assert.That(second.GetAwaiter().IsCompleted, Is.False);
            AwaitableAssert.Completes(first);
        }

        [Test]
        public void Release_WithAWaiter_HandsTheGateStraightToIt()
        {
            AwaitableAssert.Completes(gate.WaitAsync("open A"));
            var queued = gate.WaitAsync("open B");

            gate.Release();
            var latecomer = gate.WaitAsync("open C");

            AwaitableAssert.Completes(queued);
            Assert.That(gate.IsHeld, Is.True);
            Assert.That(latecomer.GetAwaiter().IsCompleted, Is.False);
        }

        [Test]
        public void Release_NoWaiters_FreesTheGate()
        {
            AwaitableAssert.Completes(gate.WaitAsync("open A"));

            gate.Release();

            Assert.That(gate.IsHeld, Is.False);
            AwaitableAssert.Completes(gate.WaitAsync("open B"));
        }

        [Test]
        public void CheckStall_NobodyWaiting_ReturnsNull()
        {
            AwaitableAssert.Completes(gate.WaitAsync("open A"));
            gate.StallWarningSeconds = 5;

            Assert.That(gate.CheckStall(now + 60), Is.Null);
        }

        [Test]
        public void CheckStall_BeforeTheThreshold_ReturnsNull()
        {
            HoldWithOneWaiting();

            Assert.That(gate.CheckStall(now + 4.9), Is.Null);
        }

        [Test]
        public void CheckStall_AtTheThreshold_DescribesTheHolderAndTheQueue()
        {
            HoldWithOneWaiting();

            var stall = gate.CheckStall(now + 5);

            StringAssert.Contains("'open A'", stall);
            StringAssert.Contains("1 operation(s) queued", stall);
            StringAssert.Contains("next: 'open B'", stall);
        }

        [Test]
        public void CheckStall_SameAcquisition_ReportsOnce()
        {
            HoldWithOneWaiting();
            gate.CheckStall(now + 5);

            Assert.That(gate.CheckStall(now + 10), Is.Null);
        }

        [Test]
        public void CheckStall_NextAcquisitionStalling_ReportsAgain()
        {
            HoldWithOneWaiting();
            gate.CheckStall(now + 5);
            now += 5;
            gate.StallWarningSeconds = 0;
            gate.Release();
            gate.WaitAsync("open C");
            gate.StallWarningSeconds = 5;

            var stall = gate.CheckStall(now + 5);

            StringAssert.Contains("'open B'", stall);
        }

        [Test]
        public void CheckStall_WarningDisabled_ReturnsNull()
        {
            HoldWithOneWaiting();
            gate.StallWarningSeconds = 0;

            Assert.That(gate.CheckStall(now + 60), Is.Null);
        }

        [Test]
        public void CheckStall_GateFree_ReturnsNull()
        {
            gate.StallWarningSeconds = 5;

            Assert.That(gate.CheckStall(now + 60), Is.Null);
        }

        private void HoldWithOneWaiting()
        {
            AwaitableAssert.Completes(gate.WaitAsync("open A"));
            gate.WaitAsync("open B");
            gate.StallWarningSeconds = 5;
        }
    }
}
