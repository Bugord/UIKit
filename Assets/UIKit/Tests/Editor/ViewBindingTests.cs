using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit.Tests
{
    // EditMode has no panel, so bindings never apply their values here: these tests check what
    // gets bound, and the validation done when binding. ExampleSceneTests covers values on a panel.
    public class ViewBindingTests : NavigatorTestBase
    {
        private BindingViewModel viewModel;
        private TestView<BindingViewModel> view;
        private Label label;
        private Slider slider;
        private Button button;

        [SetUp]
        public void CreateView()
        {
            ui.Context.RegisterView<BindingViewModel, TestView<BindingViewModel>>(
                () => new TestView<BindingViewModel>());
            viewModel = new BindingViewModel();
            view = Create(viewModel);
            (label, slider, button) = AddElements(view);
        }

        [Test]
        public void Require_ExistingElement_ReturnsIt()
        {
            Assert.That(view.RequireElement<Slider>("volume-slider"), Is.SameAs(slider));
        }

        [Test]
        public void Require_MissingElement_ThrowsNamingTheViewTypeAndName()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => view.RequireElement<Label>("missing"));

            Assert.That(exception.Message, Does.Contain("TestView").And.Contain(nameof(Label)).And.Contain("missing"));
        }

        [Test]
        public void Require_ElementOfAnotherType_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => view.RequireElement<Button>("volume-slider"));
        }

        [Test]
        public void BindText_ByName_BindsTheTextOneWay()
        {
            var binding = view.BindTextOf("title-label", nameof(BindingViewModel.Greeting));

            Assert.That(label.GetBinding("text"), Is.SameAs(binding));
            Assert.That(binding.dataSourcePath.ToString(), Is.EqualTo(nameof(BindingViewModel.Greeting)));
            Assert.That(binding.bindingMode, Is.EqualTo(BindingMode.ToTarget));
        }

        [Test]
        public void BindText_Element_BindsTheText()
        {
            var binding = view.BindTextOf(label, nameof(BindingViewModel.Name));

            Assert.That(label.GetBinding("text"), Is.SameAs(binding));
        }

        [Test]
        public void BindText_MissingElement_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => view.BindTextOf("missing", nameof(BindingViewModel.Name)));
        }

        [Test]
        public void BindValue_ByName_BindsTheValueTwoWay()
        {
            var binding = view.BindValueOf("volume-slider", nameof(BindingViewModel.Volume));

            Assert.That(slider.GetBinding("value"), Is.SameAs(binding));
            Assert.That(binding.dataSourcePath.ToString(), Is.EqualTo(nameof(BindingViewModel.Volume)));
            Assert.That(binding.bindingMode, Is.EqualTo(BindingMode.TwoWay));
        }

        [Test]
        public void BindValue_Field_BindsTheValueInTheGivenMode()
        {
            var binding = view.BindValueOf(slider, nameof(BindingViewModel.Volume), BindingMode.ToTarget);

            Assert.That(slider.GetBinding("value"), Is.SameAs(binding));
            Assert.That(binding.bindingMode, Is.EqualTo(BindingMode.ToTarget));
        }

        [Test]
        public void BindValue_ElementWithoutAValue_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => view.BindValueOf("plain", nameof(BindingViewModel.Name)));
        }

        [Test]
        public void BindProperty_UnknownProperty_ThrowsNamingTheViewModelAndProperty()
        {
            var exception = Assert.Throws<ArgumentException>(() => view.BindTextOf(label, "Nmae"));

            Assert.That(exception.Message, Does.Contain(nameof(BindingViewModel)).And.Contain("Nmae"));
            Assert.That(label.GetBinding("text"), Is.Null);
        }

        [Test]
        public void BindProperty_PropertyWithoutCreateProperty_Throws()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                view.BindTextOf(label, nameof(BindingViewModel.Hidden)));

            Assert.That(exception.Message, Does.Contain("CreateProperty"));
        }

        [TestCase(BindingMode.TwoWay)]
        [TestCase(BindingMode.ToSource)]
        public void BindProperty_ReadOnlyPropertyInAModeThatWritesBack_Throws(BindingMode mode)
        {
            var field = new TextField { name = "greeting-field" };
            view.Root.Add(field);

            var exception = Assert.Throws<ArgumentException>(() =>
                view.BindValueOf(field, nameof(BindingViewModel.Greeting), mode));

            Assert.That(exception.Message, Does.Contain(nameof(BindingViewModel.Greeting)).And.Contain("read-only"));
        }

        [Test]
        public void BindProperty_ReadOnlyPropertyToTarget_Binds()
        {
            var binding = view.BindElementProperty(label, "text", nameof(BindingViewModel.Greeting));

            Assert.That(label.GetBinding("text"), Is.SameAs(binding));
        }

        [Test]
        public void BindProperty_EmptyViewModelProperty_Throws()
        {
            Assert.Throws<ArgumentException>(() => view.BindElementProperty(label, "text", string.Empty));
        }

        [Test]
        public void BindProperty_PropertyOfTheRuntimeSubclass_Binds()
        {
            var derived = Create(new DerivedBindingViewModel());
            var (derivedLabel, _, _) = AddElements(derived);

            var binding = derived.BindTextOf(derivedLabel, nameof(DerivedBindingViewModel.Extra));

            Assert.That(derivedLabel.GetBinding("text"), Is.SameAs(binding));
        }

        [Test]
        public void Observe_PropertyWithoutCreateProperty_Throws()
        {
            Assert.Throws<ArgumentException>(() => view.ObserveProperty(nameof(BindingViewModel.Hidden), () => { }));
        }

        [Test]
        public void ObserveValue_PropertyOfTheRuntimeSubclass_PassesItsValue()
        {
            var derivedModel = new DerivedBindingViewModel();
            var derived = Create(derivedModel);
            var values = new List<int>();
            derived.ObserveValue<int>(nameof(DerivedBindingViewModel.Extra), values.Add);

            derivedModel.Extra = 7;

            Assert.That(values, Is.EqualTo(new[] { 7 }));
        }

        [Test]
        public void ObserveValue_ComputedProperty_PassesTheValueReadAtNotifyTime()
        {
            var values = new List<string>();
            view.ObserveValue<string>(nameof(BindingViewModel.Greeting), values.Add);
            viewModel.Name = "Ada";

            viewModel.NotifyProperty(nameof(BindingViewModel.Greeting));

            Assert.That(values, Is.EqualTo(new[] { "Hello, Ada" }));
        }

        [Test]
        public void BindCommand_CommandCanExecute_EnablesTheButton()
        {
            button.SetEnabled(false);

            view.BindCommandTo(button, new Command(() => { }));

            Assert.That(button.enabledSelf, Is.True);
        }

        [Test]
        public void BindCommand_CommandCannotExecute_DisablesTheButton()
        {
            view.BindCommandTo(button, new Command(() => { }, () => false));

            Assert.That(button.enabledSelf, Is.False);
        }

        [Test]
        public void BindCommand_CanExecuteChanges_UpdatesTheButton()
        {
            var allowed = true;
            var command = new Command(() => { }, () => allowed);
            view.BindCommandTo(button, command);

            allowed = false;
            command.RaiseCanExecuteChanged();

            Assert.That(button.enabledSelf, Is.False);
        }

        [Test]
        public void BindCommand_Click_ExecutesTheCommand()
        {
            var clickable = UseTestClickable(button);
            var calls = 0;
            view.BindCommandTo(button, new Command(() => calls++));

            clickable.Click();

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void BindCommand_ByName_BindsThatButton()
        {
            var clickable = UseTestClickable(button);
            var calls = 0;
            view.BindCommandTo("ok-button", new Command(() => calls++));

            clickable.Click();

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void BindCommand_MissingButton_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => view.BindCommandTo("missing", new Command(() => { })));
        }

        [Test]
        public void BindCommand_AsyncCommandRunning_DisablesTheButtonUntilItFinishes()
        {
            var clickable = UseTestClickable(button);
            var hold = new AwaitableCompletionSource();
            view.BindCommandTo(button, new AsyncCommand(() => hold.Awaitable));

            clickable.Click();
            Assert.That(button.enabledSelf, Is.False);

            hold.SetResult();
            Assert.That(button.enabledSelf, Is.True);
        }

        [Test]
        public void BindCommand_SharedGate_DisablesTheOtherButtonsToo()
        {
            var clickable = UseTestClickable(button);
            var other = new Button { name = "other-button" };
            view.Root.Add(other);
            var gate = new CommandGate();
            var hold = new AwaitableCompletionSource();
            view.BindCommandTo(button, new AsyncCommand(() => hold.Awaitable, gate));
            view.BindCommandTo(other, new AsyncCommand(() => Awaitables.Completed(), gate));

            clickable.Click();
            Assert.That(other.enabledSelf, Is.False);

            hold.SetResult();
            Assert.That(other.enabledSelf, Is.True);
        }

        [Test]
        public void BindCommand_ViewDisposed_UnsubscribesFromTheCommandAndTheButton()
        {
            var clickable = UseTestClickable(button);
            var command = new CountingCommand();
            view.BindCommandTo(button, command);
            Assert.That(command.Subscribers, Is.EqualTo(1));

            view.Dispose();
            clickable.Click();

            Assert.That(command.Subscribers, Is.Zero);
            Assert.That(command.ExecuteCount, Is.Zero);
        }

        [Test]
        public void BindCommand_CommandFinishesAfterTheViewIsDisposed_LeavesTheButtonAlone()
        {
            var hold = new AwaitableCompletionSource();
            var command = new AsyncCommand(() => hold.Awaitable);
            view.BindCommandTo(button, command);
            var run = command.ExecuteAsync();

            view.Dispose();
            hold.SetResult();

            AwaitableAssert.Completes(run);
            Assert.That(button.enabledSelf, Is.False, "The disposed view still drove its button.");
        }

        [Test]
        public void BindCommand_AfterDispose_IsIgnored()
        {
            var command = new CountingCommand();
            view.Dispose();

            view.BindCommandTo(button, command);

            Assert.That(command.Subscribers, Is.Zero);
        }

        private TestView<BindingViewModel> Create(BindingViewModel model) =>
            ui.Context.ViewFactory.Create<TestView<BindingViewModel>, BindingViewModel>(
                model, new VisualElement(), disposeViewModel: false);

        private static (Label, Slider, Button) AddElements(TestView<BindingViewModel> target)
        {
            var titleLabel = new Label { name = "title-label" };
            var volumeSlider = new Slider { name = "volume-slider" };
            var okButton = new Button { name = "ok-button" };

            target.Root.Add(titleLabel);
            target.Root.Add(volumeSlider);
            target.Root.Add(okButton);
            target.Root.Add(new VisualElement { name = "plain" });
            return (titleLabel, volumeSlider, okButton);
        }

        // Without a panel a button gets no pointer or submit events, so the test clicks through
        // the button's manipulator instead.
        private static TestClickable UseTestClickable(Button target)
        {
            var clickable = new TestClickable();
            target.clickable = clickable;
            return clickable;
        }

        private sealed class TestClickable : Clickable
        {
            public TestClickable() : base((Action)null)
            {
            }

            public void Click() => Invoke(null);
        }

        private sealed class CountingCommand : ICommand
        {
            private Action canExecuteChanged;

            public event Action CanExecuteChanged
            {
                add {
                    canExecuteChanged += value;
                    Subscribers++;
                }
                remove {
                    canExecuteChanged -= value;
                    Subscribers--;
                }
            }

            public int Subscribers { get; private set; }
            public int ExecuteCount { get; private set; }
            public bool CanExecute => true;

            public void Execute() => ExecuteCount++;

            public void RaiseCanExecuteChanged() => canExecuteChanged?.Invoke();
        }
    }
}
