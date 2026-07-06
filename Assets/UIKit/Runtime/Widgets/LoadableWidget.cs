using System;
using System.Collections.Generic;
using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Core;
using UIKit.Runtime.Utils;

namespace UIKit.Runtime.Widgets
{
    public abstract class LoadableWidget<TViewModel> : Widget<TViewModel>, ILoadableWidget where TViewModel : ViewModel
    {
        private static readonly Dictionary<Type, string> IdCache = new();

        protected LoadableWidget(TViewModel model) : base(model)
        {
        }

        public virtual string WidgetId
        {
            get
            {
                var type = GetType();
                if (!IdCache.TryGetValue(type, out var id)) {
                    id = UIIdConventions.ResolveWidgetId(type);
                    IdCache[type] = id;
                }
                return id;
            }
        }
    }
}