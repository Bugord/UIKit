using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Navigation
{
    public class RootPanelProvider
    {
        public UIDocument RootUIDocument { get; }
        public VisualElement Root { get; private set; }

        public RootPanelProvider(PanelSettings panelSettings)
        {
            var go = new GameObject("UIRoot");
            Object.DontDestroyOnLoad(go);
            RootUIDocument = go.AddComponent<UIDocument>();
            RootUIDocument.panelSettings = panelSettings;
            Root = RootUIDocument.rootVisualElement;
        }
    }
}
