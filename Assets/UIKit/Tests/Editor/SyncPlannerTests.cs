using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UIKit.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UIKit.Tests
{
    public class SyncPlannerTests
    {
        private readonly List<Object> created = new();
        private VisualTreeAsset menuUxml;
        private VisualTreeAsset storeUxml;
        private VisualTreeAsset foundUxml;

        [SetUp]
        public void SetUp()
        {
            menuUxml = Uxml();
            storeUxml = Uxml();
            foundUxml = Uxml();
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
        public void Plan_ScreenFromAClaimedAssembly_GoesToTheClaimingCatalog()
        {
            var catalogs = new[] { Catalog("Main"), Catalog("Shop", new[] { "Shop.UI" }) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Store", "Shop.UI") }, FindNothing);

            Assert.That(Ids(result, 0), Is.Empty);
            Assert.That(Ids(result, 1), Is.EqualTo(new[] { "Store" }));
        }

        [Test]
        public void Plan_ScreenFromAnUnclaimedAssembly_GoesToTheDefaultCatalog()
        {
            var catalogs = new[] { Catalog("Shop", new[] { "Shop.UI" }), Catalog("Main") };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Menu", "Game") }, FindNothing);

            Assert.That(Ids(result, 0), Is.Empty);
            Assert.That(Ids(result, 1), Is.EqualTo(new[] { "Menu" }));
        }

        [Test]
        public void Plan_ScreenWithoutAnAssemblyName_GoesToTheDefaultCatalog()
        {
            var result = SyncPlanner.Plan(new[] { Catalog("Main") }, new[] { Screen("Menu", null) }, FindNothing);

            Assert.That(Ids(result, 0), Is.EqualTo(new[] { "Menu" }));
        }

        [Test]
        public void Plan_NewScreen_IsAddedAndReported()
        {
            var result = SyncPlanner.Plan(new[] { Catalog("Main") }, new[] { Screen("Menu") }, FindNothing);

            Assert.That(Ids(result, 0), Is.EqualTo(new[] { "Menu" }));
            Assert.That(result.Catalogs[0].Changed, Is.True);
            Assert.That(result.Report.Added, Is.EqualTo(new[] { ("Main", "Menu") }));
        }

        [Test]
        public void Plan_ScreenNoLongerInCode_IsRemovedAndReported()
        {
            var catalogs = new[] { Catalog("Main", null, ("Old", menuUxml), ("Menu", menuUxml)) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Menu") }, FindNothing);

            Assert.That(Ids(result, 0), Is.EqualTo(new[] { "Menu" }));
            Assert.That(result.Catalogs[0].Changed, Is.True);
            Assert.That(result.Report.Removed, Is.EqualTo(new[] { ("Main", "Old") }));
        }

        [Test]
        public void Plan_RemovedScreens_AreReportedInOrdinalOrder()
        {
            var catalogs = new[] { Catalog("Main", null, ("b", menuUxml), ("a", menuUxml), ("B", menuUxml)) };

            var result = SyncPlanner.Plan(catalogs, Array.Empty<ScreenSource>(), FindNothing);

            Assert.That(result.Report.Removed.Select(removed => removed.screenId), Is.EqualTo(new[] { "B", "a", "b" }));
        }

        [Test]
        public void Plan_ScreenWithHandAssignedUxml_KeepsIt()
        {
            var catalogs = new[] { Catalog("Main", null, ("Menu", menuUxml)) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Menu") }, _ => foundUxml);

            Assert.That(result.Catalogs[0].Entries.Single().uxml, Is.SameAs(menuUxml));
            Assert.That(result.Report.Added, Is.Empty);
        }

        [Test]
        public void Plan_ScreenMovedToAnotherCatalog_TakesItsUxmlAlong()
        {
            var catalogs = new[] {
                Catalog("Main", null, ("Store", storeUxml)),
                Catalog("Shop", new[] { "Shop.UI" })
            };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Store", "Shop.UI") }, _ => foundUxml);

            Assert.That(result.Catalogs[1].Entries.Single().uxml, Is.SameAs(storeUxml));
            Assert.That(result.Report.Removed, Is.EqualTo(new[] { ("Main", "Store") }));
            Assert.That(result.Report.Added, Is.EqualTo(new[] { ("Shop", "Store") }));
        }

        [Test]
        public void Plan_NewScreen_TakesTheUxmlFoundForItsId()
        {
            var lookedUp = new List<string>();

            var result = SyncPlanner.Plan(new[] { Catalog("Main") }, new[] { Screen("Menu") }, id => {
                lookedUp.Add(id);
                return foundUxml;
            });

            Assert.That(lookedUp, Is.EqualTo(new[] { "Menu" }));
            Assert.That(result.Catalogs[0].Entries.Single().uxml, Is.SameAs(foundUxml));
            Assert.That(result.Report.MissingUxml, Is.Empty);
        }

        [Test]
        public void Plan_ListedScreenWithoutUxml_LooksItUpWithoutReportingItAsAdded()
        {
            var catalogs = new[] { Catalog("Main", null, ("Menu", null)) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Menu") }, _ => foundUxml);

            Assert.That(result.Catalogs[0].Entries.Single().uxml, Is.SameAs(foundUxml));
            Assert.That(result.Catalogs[0].Changed, Is.True);
            Assert.That(result.Report.Added, Is.Empty);
        }

        [Test]
        public void Plan_NoUxmlFound_KeepsTheEntryAndReportsItMissing()
        {
            var result = SyncPlanner.Plan(new[] { Catalog("Main") }, new[] { Screen("Menu") }, FindNothing);

            Assert.That(result.Catalogs[0].Entries.Single().uxml, Is.Null);
            Assert.That(result.Report.MissingUxml, Is.EqualTo(new[] { ("Main", "Menu") }));
        }

        [Test]
        public void Plan_WithoutAUxmlLookup_ReportsMissingUxml()
        {
            var result = SyncPlanner.Plan(new[] { Catalog("Main") }, new[] { Screen("Menu") }, null);

            Assert.That(result.Report.MissingUxml, Is.EqualTo(new[] { ("Main", "Menu") }));
        }

        [Test]
        public void Plan_IdListedTwiceInACatalog_KeepsOneEntryWithTheUxml()
        {
            var catalogs = new[] { Catalog("Main", null, ("Menu", null), ("Menu", menuUxml)) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Menu") }, FindNothing);

            Assert.That(result.Catalogs[0].Entries.Single().uxml, Is.SameAs(menuUxml));
            Assert.That(result.Catalogs[0].Changed, Is.True);
        }

        [Test]
        public void Plan_TwoTypesWithTheSameId_SyncsTheFirstByTypeNameAndReportsBoth()
        {
            var catalogs = new[] { Catalog("Main"), Catalog("Shop", new[] { "Shop.UI" }) };
            var screens = new[] {
                new ScreenSource("Menu", "Game", "Game.MenuView"),
                new ScreenSource("Menu", "Shop.UI", "Debug.MenuView")
            };

            var result = SyncPlanner.Plan(catalogs, screens, FindNothing);

            Assert.That(Ids(result, 0), Is.Empty);
            Assert.That(Ids(result, 1), Is.EqualTo(new[] { "Menu" }));
            var duplicate = result.Report.DuplicateIds.Single();
            Assert.That(duplicate.screenId, Is.EqualTo("Menu"));
            Assert.That(duplicate.types, Is.EqualTo(new[] { "Debug.MenuView", "Game.MenuView" }));
            Assert.That(result.Report.HasErrors, Is.True);
        }

        [Test]
        public void Plan_NoCatalogTakesTheAssembly_ReportsTheScreenUnassigned()
        {
            var catalogs = new[] { Catalog("Shop", new[] { "Shop.UI" }) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Menu", "Game") }, FindNothing);

            Assert.That(Ids(result, 0), Is.Empty);
            Assert.That(result.Report.Unassigned, Is.EqualTo(new[] { ("Menu", "Game") }));
            Assert.That(result.Report.HasErrors, Is.True);
        }

        [Test]
        public void Plan_NoCatalogs_ReportsEveryScreenUnassigned()
        {
            var result = SyncPlanner.Plan(Array.Empty<CatalogState>(), new[] { Screen("Menu") }, FindNothing);

            Assert.That(result.Catalogs, Is.Empty);
            Assert.That(result.Report.Unassigned.Select(unassigned => unassigned.screenId), Is.EqualTo(new[] { "Menu" }));
        }

        [Test]
        public void Plan_AssemblyClaimedByTwoCatalogs_GoesToTheFirstAndIsReported()
        {
            var catalogs = new[] {
                Catalog("ShopA", new[] { "Shop.UI" }),
                Catalog("ShopB", new[] { "Shop.UI" })
            };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Store", "Shop.UI") }, FindNothing);

            Assert.That(Ids(result, 0), Is.EqualTo(new[] { "Store" }));
            Assert.That(Ids(result, 1), Is.Empty);
            var contested = result.Report.ContestedAssemblies.Single();
            Assert.That(contested.assembly, Is.EqualTo("Shop.UI"));
            Assert.That(contested.catalogs, Is.EqualTo(new[] { "ShopA", "ShopB" }));
        }

        [Test]
        public void Plan_AssemblyListedTwiceInOneCatalog_IsNotContested()
        {
            var catalogs = new[] { Catalog("Shop", new[] { "Shop.UI", "Shop.UI" }) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Store", "Shop.UI") }, FindNothing);

            Assert.That(result.Report.ContestedAssemblies, Is.Empty);
        }

        [Test]
        public void Plan_ListedAssemblies_AreTrimmedAndBlankRowsClaimNothing()
        {
            var catalogs = new[] { Catalog("Main"), Catalog("Shop", new[] { " Shop.UI ", " ", "" }) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Store", "Shop.UI") }, FindNothing);

            Assert.That(Ids(result, 1), Is.EqualTo(new[] { "Store" }));
            Assert.That(result.Report.DefaultCatalogs, Is.Empty);
        }

        [Test]
        public void Plan_CatalogWithOnlyBlankAssemblyRows_IsTheDefault()
        {
            var catalogs = new[] { Catalog("Main", new[] { "", "  " }) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Menu", "Game") }, FindNothing);

            Assert.That(Ids(result, 0), Is.EqualTo(new[] { "Menu" }));
        }

        [Test]
        public void Plan_SeveralDefaultCatalogs_FirstTakesTheUnclaimedScreensAndAllAreReported()
        {
            var catalogs = new[] { Catalog("Main"), Catalog("Extra") };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Menu") }, FindNothing);

            Assert.That(Ids(result, 0), Is.EqualTo(new[] { "Menu" }));
            Assert.That(Ids(result, 1), Is.Empty);
            Assert.That(result.Report.DefaultCatalogs, Is.EqualTo(new[] { "Main", "Extra" }));
            Assert.That(result.Report.HasErrors, Is.True);
        }

        [Test]
        public void Plan_OneDefaultCatalog_IsNotReported()
        {
            var result = SyncPlanner.Plan(new[] { Catalog("Main") }, new[] { Screen("Menu") }, FindNothing);

            Assert.That(result.Report.DefaultCatalogs, Is.Empty);
        }

        [Test]
        public void Plan_Entries_AreInOrdinalIdOrderWhateverTheInputOrder()
        {
            var screens = new[] { Screen("b"), Screen("a"), Screen("B") };

            var forward = SyncPlanner.Plan(new[] { Catalog("Main") }, screens, FindNothing);
            var backward = SyncPlanner.Plan(new[] { Catalog("Main") }, screens.Reverse().ToArray(), FindNothing);

            Assert.That(Ids(forward, 0), Is.EqualTo(new[] { "B", "a", "b" }));
            Assert.That(Ids(backward, 0), Is.EqualTo(Ids(forward, 0)));
        }

        [Test]
        public void Plan_AlreadyInSync_ReportsNoChanges()
        {
            var catalogs = new[] { Catalog("Main", null, ("Menu", menuUxml), ("Store", storeUxml)) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Store"), Screen("Menu") }, _ => foundUxml);

            Assert.That(result.Catalogs[0].Changed, Is.False);
            Assert.That(result.HasChanges, Is.False);
            Assert.That(result.Report.IsEmpty, Is.True);
        }

        [Test]
        public void Plan_SameEntriesOutOfOrder_IsAChangeWithNothingAddedOrRemoved()
        {
            var catalogs = new[] { Catalog("Main", null, ("Store", storeUxml), ("Menu", menuUxml)) };

            var result = SyncPlanner.Plan(catalogs, new[] { Screen("Menu"), Screen("Store") }, FindNothing);

            Assert.That(Ids(result, 0), Is.EqualTo(new[] { "Menu", "Store" }));
            Assert.That(result.Catalogs[0].Changed, Is.True);
            Assert.That(result.Report.Added, Is.Empty);
            Assert.That(result.Report.Removed, Is.Empty);
        }

        [Test]
        public void Plan_ScreenWithoutAnId_IsIgnored()
        {
            var result = SyncPlanner.Plan(new[] { Catalog("Main") }, new[] { Screen(""), Screen(null) }, FindNothing);

            Assert.That(Ids(result, 0), Is.Empty);
            Assert.That(result.Report.IsEmpty, Is.True);
        }

        private static VisualTreeAsset FindNothing(string screenId) => null;

        private static string[] Ids(SyncResult result, int catalog) =>
            result.Catalogs[catalog].Entries.Select(entry => entry.screenId).ToArray();

        private static ScreenSource Screen(string id, string assembly = "Game") =>
            new(id, assembly, $"{assembly}.{id}View");

        private static CatalogState Catalog(string name, string[] assemblies = null,
            params (string id, VisualTreeAsset uxml)[] entries) =>
            new(name, assemblies, entries.Select(entry => new ScreenCatalog.Entry { screenId = entry.id, uxml = entry.uxml }).ToArray());

        private VisualTreeAsset Uxml()
        {
            var asset = ScriptableObject.CreateInstance<VisualTreeAsset>();
            created.Add(asset);
            return asset;
        }
    }
}
