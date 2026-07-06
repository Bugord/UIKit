using UIKit.Runtime.Core;
using UIKit.Runtime.Utils;

namespace Example.Widgets.Coins
{
    [UIWidget(true)]
    public class Coins : Widget<CoinsViewModel>
    {
        public Coins(CoinsViewModel model) : base(model)
        {
        }
    }
}