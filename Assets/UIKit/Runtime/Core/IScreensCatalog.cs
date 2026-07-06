using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public interface IScreensCatalog
    {
        VisualTreeAsset GetAssetFor<TView>(TView view) where TView : IView;
    }
}