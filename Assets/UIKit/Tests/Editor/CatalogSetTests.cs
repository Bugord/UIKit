using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UIKit.Tests
{
    public class CatalogSetTests
    {
        private readonly List<Object> created = new();
        private VisualTreeAsset uxml;

        [SetUp]
        public void SetUp()
        {
            uxml = Track(ScriptableObject.CreateInstance<VisualTreeAsset>());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in created) {
                Object.DestroyImmediate(asset);
            }

            created.Clear();
        }

        [Test]
        public void GetUxml_IdInAnyCatalog_ReturnsItsUxml()
        {
            var other = Track(ScriptableObject.CreateInstance<VisualTreeAsset>());
            var set = new CatalogSet(new[] {
                Catalog("Main", ("Menu", uxml)),
                Catalog("Shop", ("Store", other))
            });

            Assert.That(set.GetUxml("Store"), Is.SameAs(other));
        }

        [Test]
        public void GetUxml_IdInNoCatalog_Throws()
        {
            var set = new CatalogSet(new[] { Catalog("Main", ("Menu", uxml)) });

            var thrown = Assert.Throws<InvalidOperationException>(() => set.GetUxml("Store"));

            StringAssert.Contains("no catalog lists screen 'Store'", thrown.Message);
        }

        [Test]
        public void GetUxml_EntryWithoutUxml_ThrowsNamingTheCatalog()
        {
            var set = new CatalogSet(new[] { Catalog("Main", ("Menu", null)) });

            var thrown = Assert.Throws<InvalidOperationException>(() => set.GetUxml("Menu"));

            StringAssert.Contains("no UXML assigned for screen 'Menu' in 'Main'", thrown.Message);
        }

        [Test]
        public void Constructor_SameIdInTwoCatalogs_ThrowsNamingBoth()
        {
            var catalogs = new[] { Catalog("Main", ("Menu", uxml)), Catalog("Shop", ("Menu", uxml)) };

            var thrown = Assert.Throws<InvalidOperationException>(() => new CatalogSet(catalogs));

            StringAssert.Contains("'Menu' is listed in both 'Main' and 'Shop'", thrown.Message);
        }

        [Test]
        public void Constructor_SameIdTwiceInOneCatalog_ThrowsListedTwice()
        {
            var catalogs = new[] { Catalog("Main", ("Menu", uxml), ("Menu", uxml)) };

            var thrown = Assert.Throws<InvalidOperationException>(() => new CatalogSet(catalogs));

            StringAssert.Contains("'Menu' is listed twice in 'Main'", thrown.Message);
        }

        [Test]
        public void Constructor_NullCatalog_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CatalogSet(new ScreenCatalog[] { null }));
        }

        [Test]
        public void Constructor_EntriesWithoutAnId_AreSkipped()
        {
            var set = new CatalogSet(new[] { Catalog("Main", ("", uxml), (null, uxml), ("Menu", uxml)) });

            Assert.That(set.GetUxml("Menu"), Is.SameAs(uxml));
        }

        [Test]
        public void Constructor_NoCatalogs_ListsNothing()
        {
            var set = new CatalogSet(null);

            Assert.Throws<InvalidOperationException>(() => set.GetUxml("Menu"));
        }

        private ScreenCatalog Catalog(string name, params (string id, VisualTreeAsset uxml)[] entries) =>
            Track(Catalogs.Create(name, entries));

        private T Track<T>(T asset) where T : Object
        {
            created.Add(asset);
            return asset;
        }
    }
}
