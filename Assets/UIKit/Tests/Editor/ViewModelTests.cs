using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UIKit.Tests
{
    public class ViewModelTests
    {
        private ScoreViewModel score;
        private List<string> raised;

        [SetUp]
        public void SetUp()
        {
            score = new ScoreViewModel();
            raised = new List<string>();
            score.propertyChanged += (_, args) => raised.Add(args.propertyName.ToString());
        }

        [Test]
        public void SetProperty_NewValue_StoresItAndRaisesPropertyChanged()
        {
            var changed = score.TrySetScore(5);

            Assert.That(changed, Is.True);
            Assert.That(score.Score, Is.EqualTo(5));
            Assert.That(raised, Is.EqualTo(new[] { "Score" }));
        }

        [Test]
        public void SetProperty_SameValue_ReturnsFalseWithoutRaising()
        {
            score.Score = 5;
            raised.Clear();

            var changed = score.TrySetScore(5);

            Assert.That(changed, Is.False);
            Assert.That(raised, Is.Empty);
        }

        [Test]
        public void SetProperty_FromAPropertySetter_RaisesWithThePropertyName()
        {
            score.Score = 1;

            Assert.That(raised, Is.EqualTo(new[] { nameof(ScoreViewModel.Score) }));
        }

        [Test]
        public void Notify_WithoutAName_UsesTheCallingMember()
        {
            score.NotifyFromCaller();

            Assert.That(raised, Is.EqualTo(new[] { nameof(ScoreViewModel.NotifyFromCaller) }));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Notify_NullOrEmptyName_Throws(string propertyName)
        {
            Assert.Throws<ArgumentException>(() => score.NotifyProperty(propertyName));
        }

        [Test]
        public void LifetimeToken_BeforeDispose_IsNotCancelled()
        {
            Assert.That(score.LifetimeToken.IsCancellationRequested, Is.False);
        }

        [Test]
        public void LifetimeToken_TakenBeforeDispose_IsCancelledByDispose()
        {
            var token = score.LifetimeToken;

            score.Dispose();

            Assert.That(token.IsCancellationRequested, Is.True);
        }

        [Test]
        public void LifetimeToken_TakenAfterDispose_IsAlreadyCancelled()
        {
            score.Dispose();

            Assert.That(score.LifetimeToken.IsCancellationRequested, Is.True);
        }

        [Test]
        public void Dispose_Twice_RunsOnDisposeOnce()
        {
            score.Dispose();
            score.Dispose();

            Assert.That(score.OnDisposeCount, Is.EqualTo(1));
            Assert.That(score.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_DropsPropertyChangedSubscribers()
        {
            score.Dispose();

            score.NotifyProperty("Score");

            Assert.That(raised, Is.Empty);
        }
    }
}
