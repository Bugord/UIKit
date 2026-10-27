using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;

namespace UIKit.Editor
{
    // Finds the views in player code that take their root from a catalog's UXML.
    internal static class ScreenDiscovery
    {
        public static List<ScreenSource> FindScreens()
        {
            // TypeCache also returns Editor and test assemblies; a view declared there never reaches
            // the player, so a catalog entry for it would only fail the build for a missing UXML.
            var playerAssemblies = GetPlayerAssemblyNames();
            var screens = new List<ScreenSource>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<IView>()) {
                if (type.IsAbstract || type.ContainsGenericParameters) {
                    continue;
                }

                var assemblyName = type.Assembly.GetName().Name;
                if (!playerAssemblies.Contains(assemblyName)) {
                    continue;
                }

                var viewModelType = FindViewModelType(type);
                if (viewModelType == null || BuildsRootInCode(type)) {
                    continue;
                }

                screens.Add(new ScreenSource(ScreenInfo.Of(viewModelType).Id, assemblyName, type.FullName));
            }

            return screens;
        }

        private static Type FindViewModelType(Type viewType)
        {
            for (var type = viewType.BaseType; type != null; type = type.BaseType) {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(View<>)) {
                    return type.GetGenericArguments()[0];
                }
            }

            // An IView that is not a View<T> has no catalog-driven root.
            return null;
        }

        private static bool BuildsRootInCode(Type viewType)
        {
            MethodInfo method;
            try {
                method = viewType.GetMethod("CreateRoot", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
            } catch (AmbiguousMatchException) {
                // Only a method hiding CreateRoot with 'new' gets here; rare enough to assume UXML.
                return false;
            }

            var declaringType = method?.DeclaringType;
            return declaringType != null
                && !(declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeof(View<>));
        }

        private static HashSet<string> GetPlayerAssemblyNames()
        {
            var names = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Select(a => a.name)
                .ToHashSet();

            // Precompiled DLLs bypass the compilation pipeline; their import settings decide whether
            // they ship. The importer only knows the file, so a DLL whose file name differs from its
            // assembly name is not matched.
            foreach (var plugin in PluginImporter.GetImporters(EditorUserBuildSettings.activeBuildTarget)) {
                if (!plugin.isNativePlugin) {
                    names.Add(Path.GetFileNameWithoutExtension(plugin.assetPath));
                }
            }

            return names;
        }
    }
}
