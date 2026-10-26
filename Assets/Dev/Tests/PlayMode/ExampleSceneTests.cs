using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UIKit.Dev.PlayModeTests
{
    /// <summary>
    /// Smoke tests for the example scenes: each one must start without an error, open the main
    /// menu, and run the Profile flow through its real UI, data bindings included. The UI is
    /// found through the panel alone, so the DI scenes need no container references here.
    /// </summary>
    public class ExampleSceneTests
    {
        private const string RootObjectName = "UIKitRoot";
        private const string ScreenRootName = "ScreenRoot";
        private const int MaxFrames = 60;

        private Scene loadedScene;

        [UnityTest]
        public IEnumerator NoDI_RunsTheProfileFlow() => RunFlow("Assets/Example/Example.unity");

        [UnityTest]
        public IEnumerator VContainer_RunsTheProfileFlow() => RunFlow("Assets/Example/VContainer/ExampleVContainer.unity");

        [UnityTest]
        public IEnumerator Zenject_RunsTheProfileFlow() => RunFlow("Assets/Example/Zenject/ExampleZenject.unity");

        [UnityTearDown]
        public IEnumerator UnloadScene()
        {
            if (!loadedScene.IsValid() || !loadedScene.isLoaded) {
                yield break;
            }

            // Unloading destroys the scene's bootstrap or container, which disposes the context
            // and with it the (DontDestroyOnLoad) UI root.
            SceneManager.SetActiveScene(SceneManager.CreateScene("ExampleSceneTests_Empty"));
            yield return SceneManager.UnloadSceneAsync(loadedScene);

            yield return Until(() => GameObject.Find(RootObjectName) == null);
            Assert.That(GameObject.Find(RootObjectName), Is.Null, "The UI root outlived its scene.");
        }

        private IEnumerator RunFlow(string scenePath)
        {
            yield return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            loadedScene = SceneManager.GetSceneByPath(scenePath);
            Assert.That(loadedScene.isLoaded, Is.True, $"{scenePath} did not load.");

            VisualElement panelRoot = null;
            yield return FindPanelRoot(root => panelRoot = root);

            var screenLayer = panelRoot.Q(ScreenRootName);
            Assert.That(screenLayer, Is.Not.Null, "The screen layer is not on the panel.");

            // Main menu.
            yield return Until(() => screenLayer.Q("main-menu") != null);
            AssertShown(screenLayer, "main-menu");
            Assert.That(screenLayer.Q<Label>("greeting-label").text, Does.Contain("Player"));

            // Styles: the theme (.tss) gives buttons a background, Example.uss their height.
            var profileButton = screenLayer.Q<Button>("profile-button");
            yield return Until(() => profileButton.resolvedStyle.height > 0);
            Assert.That(profileButton.resolvedStyle.backgroundColor.a, Is.GreaterThan(0f),
                "Buttons are unstyled: the theme did not load.");
            Assert.That(profileButton.resolvedStyle.height, Is.EqualTo(36f).Within(0.5f),
                "Example.uss did not apply.");

            // Profile, opened from its button.
            Click(profileButton);
            yield return Until(() => IsShown(screenLayer, "profile"));
            AssertShown(screenLayer, "profile");
            Assert.That(IsShown(screenLayer, "main-menu"), Is.False, "The main menu stayed visible under Profile.");

            var profile = ScreenOf(screenLayer, "profile");
            var title = profile.Q<Label>("name-label");
            var name = profile.Q<Label>("player-name-label");
            var level = profile.Q<Label>("level-label");
            var score = profile.Q<Label>("score-label");

            Assert.That(profile.Q("player-badge"), Is.Not.Null, "The badge child view was not created.");
            foreach (var label in new[] { name, level, score }) {
                Assert.That(label.GetBinding("text"), Is.InstanceOf<DataBinding>(),
                    $"{label.name} lost its UXML data binding.");
            }

            // The code binding replaces the UXML placeholder "Welcome!".
            yield return Until(() => title.text == "Welcome, Player!");
            Assert.That(title.text, Is.EqualTo("Welcome, Player!"));
            Assert.That(name.text, Is.EqualTo("Player"));
            Assert.That(level.text, Is.EqualTo("Level 1"));
            Assert.That(score.text, Is.EqualTo("Score: 0"));

            // Five presses of +10 reach 50 points: level 2. Both labels refresh only through the
            // UXML bindings.
            var addScore = profile.Q<Button>("add-score-button");
            for (var i = 0; i < 5; i++) {
                Click(addScore);
            }

            yield return Until(() => score.text == "Score: 50" && level.text == "Level 2");
            Assert.That(score.text, Is.EqualTo("Score: 50"));
            Assert.That(level.text, Is.EqualTo("Level 2"));

            // Back to the main menu.
            Click(profile.Q<Button>("back-button"));
            yield return Until(() => ScreenOf(screenLayer, "profile") == null && IsShown(screenLayer, "main-menu"));
            Assert.That(ScreenOf(screenLayer, "profile"), Is.Null, "Profile was not removed after Back.");
            AssertShown(screenLayer, "main-menu");

            LogAssert.NoUnexpectedReceived();
        }

        // The UI root is built by UIKit; the reload callback hands over its root element.
        private static IEnumerator FindPanelRoot(Action<VisualElement> found)
        {
            GameObject rootObject = null;
            yield return Until(() => (rootObject = GameObject.Find(RootObjectName)) != null);
            Assert.That(rootObject, Is.Not.Null, $"No {RootObjectName} was created.");

            VisualElement root = null;
#if UNITY_6000_5_OR_NEWER
            var renderer = rootObject.GetComponent<PanelRenderer>();
            Assert.That(renderer.panelSettings.themeStyleSheet, Is.Not.Null, "The panel has no theme.");
#if UNITY_6000_6_OR_NEWER
            PanelRenderer.VersionedUIReloadCallback callback = (_, newRoot, _) => root = newRoot;
#else
            PanelRenderer.UIReloadCallback callback = (_, newRoot) => root = newRoot;
#endif
            renderer.RegisterUIReloadCallback(callback);
            yield return Until(() => root?.panel != null);
            renderer.UnregisterUIReloadCallback(callback);
#else
            var document = rootObject.GetComponent<UIDocument>();
            yield return Until(() => (root = document.rootVisualElement)?.panel != null);
#endif
            Assert.That(root?.panel, Is.Not.Null, "The UI root never got a panel.");
            found(root);
        }

        // A submit (Enter / gamepad A) on the button: Button turns it into a click the same way
        // as a pointer press and release, without needing pointer coordinates.
        private static void Click(Button button)
        {
            Assert.That(button, Is.Not.Null);

            using var evt = NavigationSubmitEvent.GetPooled();
            evt.target = button;
            button.SendEvent(evt);
        }

        // The view root added to the screen layer that holds the named element, or null.
        private static VisualElement ScreenOf(VisualElement screenLayer, string elementName)
        {
            foreach (var child in screenLayer.Children()) {
                if (child.name == elementName || child.Q(elementName) != null) {
                    return child;
                }
            }

            return null;
        }

        private static bool IsShown(VisualElement screenLayer, string elementName)
        {
            var screen = ScreenOf(screenLayer, elementName);
            return screen != null && screen.style.display != DisplayStyle.None;
        }

        private static void AssertShown(VisualElement screenLayer, string elementName)
        {
            Assert.That(ScreenOf(screenLayer, elementName), Is.Not.Null, $"'{elementName}' is not on the screen layer.");
            Assert.That(IsShown(screenLayer, elementName), Is.True, $"'{elementName}' is hidden.");
        }

        private static IEnumerator Until(Func<bool> condition)
        {
            for (var frame = 0; frame < MaxFrames && !condition(); frame++) {
                yield return null;
            }
        }
    }
}
