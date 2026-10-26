using System;
using System.Linq;
using NUnit.Framework;
using UIKit.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UIKit.Tests
{
    public class ScreenCatalogBuildValidatorTests
    {
        private VisualTreeAsset uxml;

        [SetUp]
        public void SetUp()
        {
            uxml = ScriptableObject.CreateInstance<VisualTreeAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(uxml);
        }

        [Test]
        public void Validate_EveryScreenListedOnceWithUxml_FindsNoProblems()
        {
            var catalogs = new[] { Catalog("Main", ("Menu", uxml)), Catalog("Shop", ("Store", uxml)) };

            var problems = ScreenCatalogBuildValidator.Validate(catalogs, new[] { Screen("Menu"), Screen("Store") });

            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void Validate_EntryWithoutUxml_IsAProblem()
        {
            var problems = ScreenCatalogBuildValidator.Validate(new[] { Catalog("Main", ("Menu", null)) },
                new[] { Screen("Menu") });

            Assert.That(problems, Is.EqualTo(new[] { "'Menu' in 'Main' has no UXML." }));
        }

        [Test]
        public void Validate_ScreenListedInTwoCatalogs_IsAProblem()
        {
            var catalogs = new[] { Catalog("Main", ("Menu", uxml)), Catalog("Shop", ("Menu", uxml)) };

            var problems = ScreenCatalogBuildValidator.Validate(catalogs, new[] { Screen("Menu") });

            Assert.That(problems, Is.EqualTo(new[] { "'Menu' is listed in Main, Shop; a screen belongs to one catalog." }));
        }

        [Test]
        public void Validate_ScreenListedTwiceInOneCatalog_IsAProblem()
        {
            var catalogs = new[] { Catalog("Main", ("Menu", uxml), ("Menu", uxml)) };

            var problems = ScreenCatalogBuildValidator.Validate(catalogs, new[] { Screen("Menu") });

            Assert.That(problems, Has.Count.EqualTo(1));
            StringAssert.Contains("'Menu'", problems[0]);
            StringAssert.Contains("Main", problems[0]);
        }

        [Test]
        public void Validate_TwoTypesWithTheSameId_IsAProblemNamingThemInOrder()
        {
            var screens = new[] {
                new ScreenSource("Menu", "Game", "Game.MenuView"),
                new ScreenSource("Menu", "Debug", "Debug.MenuView")
            };

            var problems = ScreenCatalogBuildValidator.Validate(new[] { Catalog("Main", ("Menu", uxml)) }, screens);

            Assert.That(problems, Is.EqualTo(new[] { "'Menu' is the id of Debug.MenuView, Game.MenuView." }));
        }

        [Test]
        public void Validate_ScreenInNoCatalog_IsAProblem()
        {
            var problems = ScreenCatalogBuildValidator.Validate(new[] { Catalog("Main", ("Menu", uxml)) },
                new[] { Screen("Menu"), Screen("Store") });

            Assert.That(problems, Is.EqualTo(new[] { "'Store' is in no catalog." }));
        }

        [Test]
        public void Validate_NullEntriesAndEntriesWithoutAnId_AreSkipped()
        {
            var catalog = new CatalogState("Main", null, new[] {
                null,
                new ScreenCatalog.Entry { screenId = "", uxml = null },
                new ScreenCatalog.Entry { screenId = "Menu", uxml = uxml }
            });

            var problems = ScreenCatalogBuildValidator.Validate(new[] { catalog }, new[] { Screen("Menu") });

            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void Validate_ScreensWithoutAnId_AreSkipped()
        {
            var problems = ScreenCatalogBuildValidator.Validate(Array.Empty<CatalogState>(), new[] { Screen("") });

            Assert.That(problems, Is.Empty);
        }

        private static ScreenSource Screen(string id) => new(id, "Game", $"Game.{id}View");

        private static CatalogState Catalog(string name, params (string id, VisualTreeAsset uxml)[] entries) =>
            new(name, null, entries.Select(entry => new ScreenCatalog.Entry { screenId = entry.id, uxml = entry.uxml }).ToArray());
    }
}
