using System;
using System.Collections.Generic;

namespace UIKit
{
    // Maps a ViewModel type to the binding that builds it. Groups add their screens as they
    // register and remove them as they are disposed, which is what lets one navigator open
    // screens that belong to different DI scopes.
    internal sealed class ScreenRegistry
    {
        private readonly Dictionary<Type, ScreenBinding> bindings = new();

        public void Add(Type viewModelType, ScreenBinding binding)
        {
            if (bindings.TryGetValue(viewModelType, out var existing)) {
                // Silently overwriting would make the winner depend on registration order.
                throw new InvalidOperationException(
                    $"Screen '{viewModelType.Name}' is already registered " +
                    (ReferenceEquals(existing.Owner, binding.Owner)
                        ? "in this group."
                        : "in another screen group that is still alive. A screen belongs to exactly one " +
                          "group - register it where its ViewModel's dependencies live, not in both."));
            }

            bindings.Add(viewModelType, binding);
        }

        public bool Contains(Type viewModelType) => bindings.ContainsKey(viewModelType);

        public ScreenBinding Get(Type viewModelType)
        {
            if (!bindings.TryGetValue(viewModelType, out var binding)) {
                throw new InvalidOperationException(
                    $"Screen '{viewModelType.Name}' is not registered. Register it with " +
                    $"Register<{viewModelType.Name}, ...>() on the {nameof(UIKitContext)}, a " +
                    $"{nameof(ScreenGroup)} or your DI container, and check that group is still alive - " +
                    "a screen is unregistered together with the group that registered it.");
            }

            return binding;
        }

        public void RemoveOwnedBy(ScreenGroup owner)
        {
            // Collected first because removing from the dictionary mid-enumeration throws.
            var owned = new List<Type>();

            foreach (var pair in bindings) {
                if (ReferenceEquals(pair.Value.Owner, owner)) {
                    owned.Add(pair.Key);
                }
            }

            foreach (var viewModelType in owned) {
                bindings.Remove(viewModelType);
            }
        }
    }
}
