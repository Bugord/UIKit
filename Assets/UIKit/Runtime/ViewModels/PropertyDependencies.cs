using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UIKit
{
    // The [DependsOn] graph of one ViewModel type: for each property, the properties computed
    // from it. Built once per runtime type by reflection and cached, so a ViewModel pays only a
    // dictionary lookup when it is constructed.
    internal sealed class PropertyDependencies
    {
        private const BindingFlags DeclaredInstanceProperties =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly Dictionary<Type, PropertyDependencies> cache = new();
        private static readonly object cacheLock = new();
        private static readonly PropertyDependencies none = new(null);

        // Direct dependents per source, in declaration order (base class first). Null when the
        // type declares no dependencies, the common case.
        private readonly Dictionary<string, string[]> dependents;

        private PropertyDependencies(Dictionary<string, string[]> dependents)
        {
            this.dependents = dependents;
        }

        /// <summary>
        /// The graph for <paramref name="type"/>, built on first use. A graph that fails to build
        /// is not cached, so every construction of the faulty type throws, not just the first.
        /// </summary>
        /// <exception cref="InvalidOperationException">A [DependsOn] names an unknown property, or the dependencies form a cycle.</exception>
        public static PropertyDependencies For(Type type)
        {
            lock (cacheLock) {
                if (cache.TryGetValue(type, out var cached)) {
                    return cached;
                }
            }

            var built = Build(type);

            lock (cacheLock) {
                cache[type] = built;
            }

            return built;
        }

        public bool HasDependents(string propertyName) =>
            dependents != null && dependents.ContainsKey(propertyName);

        /// <summary>
        /// Appends the dependents of everything in <paramref name="order"/> to it, breadth-first,
        /// skipping names already in <paramref name="seen"/>. The sources stay first, and each
        /// property appears once even when it is reached along several paths.
        /// </summary>
        public void AppendDependents(List<string> order, HashSet<string> seen)
        {
            if (dependents == null) {
                return;
            }

            for (var i = 0; i < order.Count; i++) {
                if (!dependents.TryGetValue(order[i], out var direct)) {
                    continue;
                }

                foreach (var dependent in direct) {
                    if (seen.Add(dependent)) {
                        order.Add(dependent);
                    }
                }
            }
        }

        private static PropertyDependencies Build(Type type)
        {
            // Base class first, so inherited dependents come before the subclass's own. Private
            // properties of a base class are only visible from the class that declares them.
            var hierarchy = new List<Type>();
            for (var current = type; current != null && current != typeof(object); current = current.BaseType) {
                hierarchy.Add(current);
            }

            hierarchy.Reverse();

            var propertyNames = new HashSet<string>(StringComparer.Ordinal);
            var declarations = new List<(string property, string[] sources)>();

            foreach (var declaringType in hierarchy) {
                foreach (var property in declaringType.GetProperties(DeclaredInstanceProperties)
                             .OrderBy(property => property.MetadataToken)) {
                    propertyNames.Add(property.Name);

                    var attribute = property.GetCustomAttribute<DependsOnAttribute>(false);
                    if (attribute != null) {
                        declarations.Add((property.Name, attribute.PropertyNames));
                    }
                }
            }

            if (declarations.Count == 0) {
                return none;
            }

            var edges = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var (property, sources) in declarations) {
                foreach (var source in sources) {
                    if (string.IsNullOrEmpty(source) || !propertyNames.Contains(source)) {
                        throw new InvalidOperationException(
                            $"{type.Name}.{property}: [DependsOn] names '{source}', which is not a property " +
                            $"of {type.Name}.");
                    }

                    if (!edges.TryGetValue(source, out var list)) {
                        list = new List<string>();
                        edges.Add(source, list);
                    }

                    // An override that repeats its base property's [DependsOn] adds nothing.
                    if (!list.Contains(property)) {
                        list.Add(property);
                    }
                }
            }

            ThrowOnCycle(type, edges);

            return new PropertyDependencies(edges.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(),
                StringComparer.Ordinal));
        }

        // A cycle would notify forever, so it is rejected up front, naming the loop.
        private static void ThrowOnCycle(Type type, Dictionary<string, List<string>> edges)
        {
            var done = new HashSet<string>(StringComparer.Ordinal);
            var path = new List<string>();

            foreach (var source in edges.Keys) {
                Visit(source);
            }

            void Visit(string property)
            {
                if (done.Contains(property)) {
                    return;
                }

                var index = path.IndexOf(property);
                if (index >= 0) {
                    var loop = new StringBuilder();
                    for (var i = index; i < path.Count; i++) {
                        loop.Append(path[i]).Append(" -> ");
                    }

                    loop.Append(property);
                    throw new InvalidOperationException(
                        $"{type.Name}: [DependsOn] forms a cycle ({loop}, each notifying the next).");
                }

                path.Add(property);

                if (edges.TryGetValue(property, out var next)) {
                    foreach (var dependent in next) {
                        Visit(dependent);
                    }
                }

                path.RemoveAt(path.Count - 1);
                done.Add(property);
            }
        }
    }
}
