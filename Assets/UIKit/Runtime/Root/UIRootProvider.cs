using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UIKit
{
    /// <summary>
    /// Hosts the UI layers on a panel of their own: a "UIKitRoot" GameObject, kept across scene
    /// loads, rendering with the given <see cref="PanelSettings"/>. Created on first use and
    /// destroyed on <see cref="Dispose"/>.
    /// </summary>
    public sealed class UIRootProvider : IUIRootProvider, IDisposable
    {
        private const string RootObjectName = "UIKitRoot";

        private readonly PanelSettings panelSettings;

        private VisualElement screenRoot;
        private VisualElement popupRoot;
        private GameObject rootObject;
        private bool disposed;

        public UIRootProvider(PanelSettings panelSettings)
        {
            if (panelSettings == null) {
                // A PanelSettings made up here would have no theme, so every control would render
                // unstyled - and nothing would own the instance.
                throw new ArgumentNullException(nameof(panelSettings),
                    "UIKit needs a PanelSettings asset with a theme. Create one via " +
                    "Assets > Create > UI Toolkit > Panel Settings Asset.");
            }

            this.panelSettings = panelSettings;
        }

        public VisualElement GetLayerRoot(UILayer layer)
        {
            EnsureCreated();

            return layer switch {
                UILayer.Screen => screenRoot,
                UILayer.Popup => popupRoot,
                _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null)
            };
        }

        public void Dispose()
        {
            if (disposed) {
                return;
            }

            disposed = true;

            if (rootObject != null) {
                if (Application.isPlaying) {
                    Object.Destroy(rootObject);
                } else {
                    Object.DestroyImmediate(rootObject);
                }
            }

            rootObject = null;
            screenRoot = null;
            popupRoot = null;
        }

        private void EnsureCreated()
        {
            if (disposed) {
                throw new ObjectDisposedException(nameof(UIRootProvider));
            }

            if (rootObject != null) {
                return;
            }

            screenRoot = LayerRoots.Create(LayerRoots.ScreenRootName);
            popupRoot = LayerRoots.Create(LayerRoots.PopupRootName);

            rootObject = new GameObject(RootObjectName);

            if (Application.isPlaying) {
                Object.DontDestroyOnLoad(rootObject);
            }

            CreatePanelRoot(rootObject);
        }

#if UNITY_6000_5_OR_NEWER
        private void CreatePanelRoot(GameObject go)
        {
            var panelRenderer = go.AddComponent<PanelRenderer>();

#if UNITY_6000_6_OR_NEWER
            // Re-adding the layers is idempotent, so the reload version is not needed.
            panelRenderer.RegisterUIReloadCallback(
                (PanelRenderer.VersionedUIReloadCallback)((renderer, newRoot, _) => OnUIReload(renderer, newRoot)));
#else
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
#endif
            panelRenderer.panelSettings = panelSettings;
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement newRoot)
        {
            newRoot.Add(screenRoot);
            newRoot.Add(popupRoot);
        }
#else
        private void CreatePanelRoot(GameObject go)
        {
            var uiDocument = go.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;

            if (uiDocument.rootVisualElement == null) {
                throw new InvalidOperationException(
                    $"{nameof(UIRootProvider)}: {nameof(UIDocument)} did not produce a root VisualElement.");
            }

            go.AddComponent<UIDocumentRootKeeper>().Keep(uiDocument, screenRoot, popupRoot);
        }
#endif
    }
}
