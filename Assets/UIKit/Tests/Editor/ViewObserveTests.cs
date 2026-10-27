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

        [Test]
        public void Observe_UnknownProperty_ThrowsNamingTheViewModelAndProperty()
        {
            var exception = Assert.Throws<ArgumentException>(() => view.ObserveProperty("Scroe", () => { }));

            Assert.That(exception.Message, Does.Contain(nameof(ScoreViewModel)).And.Contain("Scroe"));
        }

        [Test]
        public void ObserveValue_PropertyChanges_PassesTheNewValue()
        {
            var values = new List<int>();
            view.ObserveValue<int>(nameof(ScoreViewModel.Score), values.Add);

            score.Score = 3;
            score.Score = 5;

            Assert.That(values, Is.EqualTo(new[] { 3, 5 }));
        }

        [Test]
        public void ObserveValue_OnRegistration_DoesNotRunTheCallback()
        {
            var calls = 0;

            view.ObserveValue<int>(nameof(ScoreViewModel.Score), _ => calls++);

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void ObserveValue_AnotherPropertyChanges_DoesNotRunTheCallback()
        {
            var calls = 0;
            view.ObserveValue<int>(nameof(ScoreViewModel.Score), _ => calls++);

            score.NotifyProperty("Other");

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void ObserveValue_TypeTheValueIsAssignableTo_PassesTheValue()
        {
            var values = new List<object>();
            view.ObserveValue<object>(nameof(ScoreViewModel.Score), values.Add);

            score.Score = 3;

            Assert.That(values, Is.EqualTo(new object[] { 3 }));
        }

        [Test]
        public void ObserveValue_WrongType_ThrowsNamingBothTypes()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                view.ObserveValue<string>(nameof(ScoreViewModel.Score), _ => { }));

            Assert.That(exception.Message, Does.Contain(nameof(ScoreViewModel.Score))
                .And.Contain(nameof(Int32)).And.Contain(nameof(String)));
        }

        [Test]
        public void ObserveValue_UnknownProperty_Throws()
        {
            Assert.Throws<ArgumentException>(() => view.ObserveValue<int>("Scroe", _ => { }));
        }

        [Test]
        public void ObserveValue_WithPlainObservers_RunsInRegistrationOrderBeforeOnPropertyChanged()
        {
            var calls = new List<string>();
            view.ObserveProperty(nameof(ScoreViewModel.Score), () => calls.Add("plain"));
            view.ObserveValue<int>(nameof(ScoreViewModel.Score), value => calls.Add($"value {value}"));
            view.PropertyChangedAction = _ => calls.Add("OnPropertyChanged");

            score.Score = 3;

            Assert.That(calls, Is.EqualTo(new[] { "plain", "value 3", "OnPropertyChanged" }));
        }

        [Test]
        public void ObserveValue_PropertyChangesAfterTheViewIsDisposed_DoesNotRunTheCallback()
        {
            var calls = 0;
            view.ObserveValue<int>(nameof(ScoreViewModel.Score), _ => calls++);
            view.Dispose();

            score.Score = 3;

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void ObserveValue_CallbackDisposesTheView_LaterCallbacksDoNotRun()
        {
            var calls = new List<string>();
            view.ObserveValue<int>(nameof(ScoreViewModel.Score), _ => {
                calls.Add("first");
                view.Dispose();
            });
            view.ObserveValue<int>(nameof(ScoreViewModel.Score), _ => calls.Add("second"));

            score.Score = 3;

            Assert.That(calls, Is.EqualTo(new[] { "first" }));
        }

        [Test]
        public void ObserveValue_NullPropertyName_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => view.ObserveValue<int>(null, _ => { }));
        }

        [Test]
        public void ObserveValue_NullCallback_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => view.ObserveValue<int>(nameof(ScoreViewModel.Score), null));
        }
    }
}
