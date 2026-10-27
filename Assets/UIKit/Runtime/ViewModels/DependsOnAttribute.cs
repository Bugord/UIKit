using System;

namespace UIKit
{
    /// <summary>
    /// Marks a ViewModel property as computed from other properties of the same ViewModel: each
    /// time one of them is notified, this property is notified too.
    /// </summary>
    /// <remarks>
    /// <code>
    /// [CreateProperty, DependsOn(nameof(Volume), nameof(Muted))]
    /// public string VolumeText => muted ? "Muted" : $"{volume:P0}";
    /// </code>
    /// Dependencies chain: a property may depend on another computed property. The names must
    /// be properties of the ViewModel (or a base class), and the dependencies must not form a
    /// cycle; otherwise constructing the ViewModel throws <see cref="InvalidOperationException"/>.
    /// For a property computed from state outside the ViewModel, raise the change with
    /// <c>Notify</c> when that state changes instead.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class DependsOnAttribute : Attribute
    {
        public string[] PropertyNames { get; }

        /// <param name="propertyNames">The properties this one is computed from; pass <c>nameof(...)</c>.</param>
        public DependsOnAttribute(params string[] propertyNames)
        {
            PropertyNames = propertyNames ?? Array.Empty<string>();
        }
    }
}
