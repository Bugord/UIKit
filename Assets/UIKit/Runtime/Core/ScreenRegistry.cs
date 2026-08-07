using System;
using System.Collections.Generic;

namespace UIKit.Runtime.Core
{
    /// <summary>
    /// Maps a ViewModel type to the <see cref="ScreenBinding"/> that builds it. Scopes add
    /// their screens as they are built and remove them as they are disposed, which is what
    /// lets a single navigator open screens that belong to different containers.
    /// </summary>
    /// <remarks>
    /// Deliberately a lookup table rather than an "active scope" the navigator resolves
    /// through: where a screen comes from is a property of that screen, fixed at
    /// registration, not of whichever scope happens to have been built most recently.
    /// </remarks>
    public sealed class ScreenRegistry
    {
        private readonly Dictionary<Type, ScreenBinding> bindings = new();

        public void Add(Type viewModelType, ScreenBinding binding)
        {
            if (bindings.TryGetValue(viewModelType, out var existing)) {
                // Silently overwriting would make the winner depend on scope build order,
                // and the loser's screens would resolve from a scope nobody expects.
                throw new InvalidOperationException(
                    $"{nameof(ScreenRegistry)}: screen '{viewModelType.Name}' is already registered " +
                    (ReferenceEquals(existing.Owner, binding.Owner)
                        ? "in this scope."
                        : "in another scope that is still alive. A screen belongs to exactly one " +
                          "scope - register it in the one whose services its ViewModel needs, not in both."));
            }

            bindings.Add(viewModelType, binding);
        }

        public ScreenBinding Get(Type viewModelType)
        {
            if (!bindings.TryGetValue(viewModelType, out var binding)) {
                throw new InvalidOperationException(
                    $"{nameof(ScreenRegistry)}: screen '{viewModelType.Name}' is not registered. " +
                    $"Call RegisterScreen<{viewModelType.Name}, ...>() in the scope whose services its " +
                    "ViewModel needs, and check that scope is still alive - a screen is unregistered " +
                    "together with the scope that registered it.");
            }

            return binding;
        }

        public void RemoveOwnedBy(object owner)
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
