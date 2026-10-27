using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit
{
    /// <summary>
    /// Keeps the layer roots in a <see cref="UIDocument"/>, which builds a new root element, without
    /// the elements added from code, whenever it is enabled again.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class UIDocumentRootKeeper : MonoBehaviour
    {
        private UIDocument document;
        private VisualElement[] layerRoots;

        public void Keep(UIDocument uiDocument, params VisualElement[] roots)
        {
            document = uiDocument;
            layerRoots = roots;
            Attach();
        }

        // The document gives no notice of a new root, so it is checked once per frame.
        private void LateUpdate()
        {
            Attach();
        }

        private void Attach()
        {
            var root = document != null && document.isActiveAndEnabled ? document.rootVisualElement : null;
            if (root == null || IsAttachedTo(root)) {
                return;
            }

            // Added again in their order, so the popups stay above the screens.
            foreach (var layerRoot in layerRoots) {
                root.Add(layerRoot);
            }
        }

        private bool IsAttachedTo(VisualElement root)
        {
            foreach (var layerRoot in layerRoots) {
                if (layerRoot.parent != root) {
                    return false;
                }
            }
            return true;
        }
    }
}
