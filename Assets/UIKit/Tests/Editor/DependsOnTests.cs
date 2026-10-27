using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Properties;

namespace UIKit.Tests
{
    public class DependsOnTests
    {
        [Test]
        public void SetProperty_SourceChanges_NotifiesItsDependentsAfterIt()
        {
            var viewModel = new ChainViewModel();
            var raised = Record(viewModel);

            viewModel.Value = 2;

            Assert.That(raised, Is.EqualTo(new[] { "Value", "Doubled", "Label" }));
        }

        [Test]
        public void Notify_DependentOfADependent_IsNotifiedTransitively()
        {
            var viewModel = new ChainViewModel();
            var raised = Record(viewModel);

            viewModel.NotifyProperty(nameof(ChainViewModel.Doubled));

            Assert.That(raised, Is.EqualTo(new[] { "Doubled", "Label" }));
        }

        [Test]
        public void Notify_PropertyWithoutDependents_RaisesOnlyIt()
        {
            var viewModel = new ChainViewModel();
            var raised = Record(viewModel);

            viewModel.NotifyProperty(nameof(ChainViewModel.Label));

            Assert.That(raised, Is.EqualTo(new[] { "Label" }));
        }

        [Test]
        public void Notify_Diamond_NotifiesEachPropertyOnce()
        {
            var viewModel = new DiamondViewModel();
            var raised = Record(viewModel);

            viewModel.NotifyProperty(nameof(DiamondViewModel.Top));

            Assert.That(raised, Is.EqualTo(new[] { "Top", "Left", "Right", "Bottom" }));
        }

        [Test]
        public void Notify_Dependents_AreRaisedBreadthFirst()
        {
            var viewModel = new LevelsViewModel();
            var raised = Record(viewModel);

            viewModel.NotifyProperty(nameof(LevelsViewModel.Source));

            // Deep is declared before Second, but is one level further from Source.
            Assert.That(raised, Is.EqualTo(new[] { "Source", "First", "Second", "Deep" }));
        }

        [Test]
        public void Notify_InheritedDependencies_AreIncluded()
        {
            var viewModel = new DerivedDependentViewModel();
            var raised = Record(viewModel);

            viewModel.NotifyProperty("BaseSource");

            // BaseDependent is private to the base class; DerivedDependent is declared in the subclass.
            Assert.That(raised, Is.EqualTo(new[] { "BaseSource", "BaseDependent", "DerivedDependent" }));
        }

        [Test]
        public void Notify_SeveralProperties_RaisesThemInOrderThenTheirDependentsOnce()
        {
            var viewModel = new DiamondViewModel();
            var raised = Record(viewModel);

            viewModel.NotifyMany(nameof(DiamondViewModel.Right), nameof(DiamondViewModel.Left));

            Assert.That(raised, Is.EqualTo(new[] { "Right", "Left", "Bottom" }));
        }

        [Test]
        public void Notify_SamePropertyTwice_RaisesItOnce()
        {
            var viewModel = new DiamondViewModel();
            var raised = Record(viewModel);

            viewModel.NotifyMany(nameof(DiamondViewModel.Bottom), nameof(DiamondViewModel.Bottom),
                nameof(DiamondViewModel.Bottom));

            Assert.That(raised, Is.EqualTo(new[] { "Bottom" }));
        }

        [Test]
        public void Notify_SeveralPropertiesWithoutDependencies_RaisesEach()
        {
            var viewModel = new ScoreViewModel();
            var raised = Record(viewModel);

            viewModel.NotifyMany("First", "Second", "Third");

            Assert.That(raised, Is.EqualTo(new[] { "First", "Second", "Third" }));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Notify_SeveralPropertiesWithAnEmptyName_ThrowsWithoutRaising(string propertyName)
        {
            var viewModel = new DiamondViewModel();
            var raised = Record(viewModel);

            Assert.Throws<ArgumentException>(() => viewModel.NotifyMany(nameof(DiamondViewModel.Top), "Left",
                propertyName));
            Assert.That(raised, Is.Empty);
        }

        [Test]
        public void Construct_Cycle_ThrowsNamingIt()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => new CycleViewModel());

            Assert.That(exception.Message, Does.Contain(nameof(CycleViewModel)).And.Contain("cycle")
                .And.Contain("Ping").And.Contain("Pong"));
        }

        [Test]
        public void Construct_PropertyDependingOnItself_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new SelfCycleViewModel());
        }

        [Test]
        public void Construct_UnknownDependency_ThrowsNamingIt()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => new UnknownDependencyViewModel());

            Assert.That(exception.Message, Does.Contain(nameof(UnknownDependencyViewModel)).And.Contain("Missing"));
        }

        [Test]
        public void Construct_InvalidGraph_ThrowsEveryTime()
        {
            Assert.Throws<InvalidOperationException>(() => new CycleViewModel());
            Assert.Throws<InvalidOperationException>(() => new CycleViewModel());
        }

        private static List<string> Record(ViewModel viewModel)
        {
            var raised = new List<string>();
            viewModel.propertyChanged += (_, args) => raised.Add(args.propertyName.ToString());
            return raised;
        }

        private sealed class ChainViewModel : ViewModel
        {
            private int value;

            [CreateProperty]
            public int Value
            {
                get => value;
                set => SetProperty(ref this.value, value);
            }

            [CreateProperty, DependsOn(nameof(Value))]
            public int Doubled => value * 2;

            [CreateProperty, DependsOn(nameof(Doubled))]
            public string Label => $"x2 = {Doubled}";

            public void NotifyProperty(string propertyName) => Notify(propertyName);
        }

        private sealed class DiamondViewModel : ViewModel
        {
            public int Top => 0;

            [DependsOn(nameof(Top))]
            public int Left => Top;

            [DependsOn(nameof(Top))]
            public int Right => Top;

            [DependsOn(nameof(Left), nameof(Right))]
            public int Bottom => Left + Right;

            public void NotifyProperty(string propertyName) => Notify(propertyName);

            public void NotifyMany(string first, string second, params string[] rest) => Notify(first, second, rest);
        }

        private sealed class LevelsViewModel : ViewModel
        {
            public int Source => 0;

            [DependsOn(nameof(Source))]
            public int First => Source;

            [DependsOn(nameof(First))]
            public int Deep => First;

            [DependsOn(nameof(Source))]
            public int Second => Source;

            public void NotifyProperty(string propertyName) => Notify(propertyName);
        }

        private class BaseDependentViewModel : ViewModel
        {
            protected int BaseSource => 0;

            [DependsOn(nameof(BaseSource))]
            private int BaseDependent => BaseSource;

            public void NotifyProperty(string propertyName) => Notify(propertyName);
        }

        private sealed class DerivedDependentViewModel : BaseDependentViewModel
        {
            [DependsOn(nameof(BaseSource))]
            public int DerivedDependent => BaseSource;
        }

        private sealed class CycleViewModel : ViewModel
        {
            [DependsOn(nameof(Pong))]
            public int Ping => 0;

            [DependsOn(nameof(Ping))]
            public int Pong => 0;
        }

        private sealed class SelfCycleViewModel : ViewModel
        {
            [DependsOn(nameof(Itself))]
            public int Itself => 0;
        }

        private sealed class UnknownDependencyViewModel : ViewModel
        {
            [DependsOn("Missing")]
            public int Dependent => 0;
        }
    }
}
