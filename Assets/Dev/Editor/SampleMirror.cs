using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UIKit.Dev
{
    /// <summary>
    /// Keeps the package sample an exact copy of Assets/Example, the single source of truth.
    /// Samples~ is hidden from Unity, so the copy is made from here instead of by hand.
    /// </summary>
    public static class SampleMirror
    {
        public const string Source = "Assets/Example";
        public const string Target = "Assets/UIKit/Samples~/Example";

        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        /// <summary>
        /// Relative paths (forward slashes) that exist on one side only or differ in content.
        /// </summary>
        public static IReadOnlyList<string> Diff(string sourceDir, string targetDir)
        {
            var source = List(sourceDir);
            var target = List(targetDir);
            var result = new List<string>();

            foreach (var path in source.Union(target).OrderBy(p => p, StringComparer.Ordinal)) {
                if (!source.Contains(path) || !target.Contains(path)
                    || !File.ReadAllBytes(Path.Combine(sourceDir, path))
                        .SequenceEqual(File.ReadAllBytes(Path.Combine(targetDir, path)))) {
                    result.Add(path);
                }
            }

            return result;
        }

        [MenuItem("Tools/UIKit/Mirror Example to Samples~")]
        public static void Mirror()
        {
            var source = Path.Combine(ProjectRoot, Source);
            var target = Path.Combine(ProjectRoot, Target);

            if (Directory.Exists(target)) {
                Directory.Delete(target, true);
            }

            var count = 0;
            foreach (var path in List(source)) {
                var destination = Path.Combine(target, path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(Path.Combine(source, path), destination);
                count++;
            }

            Debug.Log($"Mirrored {count} files from {Source} to {Target}.");
        }

        private static HashSet<string> List(string dir)
        {
            var set = new HashSet<string>();
            if (!Directory.Exists(dir)) {
                return set;
            }

            foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) {
                set.Add(Path.GetRelativePath(dir, file).Replace('\\', '/'));
            }

            return set;
        }
    }
}
