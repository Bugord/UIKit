using System;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace UIKit.Runtime.Core
{
    public class UIRootProvider : IUIRootProvider, IInitializable
    {
        private const string ScreenRootName = "ScreenRoot";
        private const string PopupsRootName = "PopupsRoot";

        private readonly UIDocument uiDocument;

        private VisualElement screenRoot;
        private VisualElement popupsRoot;
        
        public UIRootProvider(UIDocument uiDocument)
        {
            this.uiDocument = uiDocument;
        }

        public void Initialize()
        {
            screenRoot = uiDocument.rootVisualElement.Q<VisualElement>(ScreenRootName);
            popupsRoot = uiDocument.rootVisualElement.Q<VisualElement>(PopupsRootName);
        }

        public VisualElement GetLayerRoot(UILayer layer) =>
            layer switch {
                UILayer.Screen => screenRoot,
                UILayer.Popup => popupsRoot,
                _ => throw new ArgumentException($"Unknown UILayer: {layer}")
            };
    }
}