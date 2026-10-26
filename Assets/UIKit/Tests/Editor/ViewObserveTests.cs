using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace UIKit.Tests
{
    public class ViewObserveTests : NavigatorTestBase
    {
        private ScoreViewModel score;
        private TestView<ScoreViewModel> view;

        [SetUp]
        public void CreateView()
        {
            ui.Context.RegisterView<ScoreViewModel, TestView<ScoreViewModel>>(() => new TestView<ScoreViewModel>());
            score = new ScoreViewModel();
            view = ui.Context.ViewFactory.Create<TestView<ScoreViewModel>, ScoreViewModel>(
                score, new VisualElement(), disposeViewModel: false);
        }

        [Test]
        public void Observe_ObservedPropertyChanges_RunsTheCallback()
        {
            var calls = 0;
            view.ObserveProperty(nameof(ScoreViewModel.Score), () => calls++);

            score.Score = 3;

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Observe_AnotherPropertyChanges_DoesNotRunTheCallback()
        {
            var calls = 0;
            view.ObserveProperty(nameof(ScoreViewModel.Score), () => calls++);

            score.NotifyProperty("Other");

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void Observe_OnRegistration_DoesNotRunTheCallback()
        {
            var calls = 0;

            view.ObserveProperty(nameof(ScoreViewModel.Score), () => calls++);

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void Observe_TwoCallbacksForTheSameProperty_RunInRegistrationOrder()
        {
            var calls = new List<string>();
            view.ObserveProperty(nameof(ScoreViewModel.Score), () => calls.Add("first"));
            view.ObserveProperty(nameof(ScoreViewModel.Score), () => calls.Add("second"));

            score.Score = 3;

            Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));
        }

        [Test]
        public void Observe_PropertyChanges_RunsTheCallbackBeforeOnPropertyChanged()
        {
            var calls = new List<string>();
            view.ObserveProperty(nameof(ScoreViewModel.Score), () => calls.Add("observer"));
            view.PropertyChangedAction = _ => calls.Add("OnPropertyChanged");

            score.Score = 3;

            Assert.That(calls, Is.EqualTo(new[] { "observer", "OnPropertyChanged" }));
        }

        [Test]
        public void Observe_PropertyChangesAfterTheViewIsDisposed_DoesNotRunTheCallback()
        {
            var calls = 0;
            view.ObserveProperty(nameof(ScoreViewModel.Score), () => calls++);
            view.Dispose();

            score.Score = 3;

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void Observe_NullPropertyName_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => view.ObserveProperty(null, () => { }));
        }

        [Test]
        public void Observe_EmptyPropertyName_Throws()
        {
            Assert.Throws<ArgumentException>(() => view.ObserveProperty(string.Empty, () => { }));
        }

        [Test]
        public void Observe_NullCallback_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => view.ObserveProperty(nameof(ScoreViewModel.Score), null));
        }
    }
}
