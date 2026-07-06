using System.Collections.Generic;
using UnityEngine;

namespace UIKit.Editor
{
    public sealed class SyncReport
    {
        public readonly List<string> Added = new();
        public readonly List<string> Removed = new();
        public readonly List<string> MissingUxml = new();
        public readonly List<string> DuplicateIds = new();
 
        public bool HasErrors => MissingUxml.Count > 0 || DuplicateIds.Count > 0;
 
        public void Log(string catalogName)
        {
            if (Added.Count > 0) {
                Debug.Log($"{catalogName}: added {string.Join(", ", Added)}");
            }
            if (Removed.Count > 0) {
                Debug.Log($"{catalogName}: removed {string.Join(", ", Removed)}");
            }
            foreach (var id in DuplicateIds) {
                Debug.LogError($"{catalogName}: duplicate id '{id}' — pin one type with an id attribute");
            }
            if (MissingUxml.Count > 0) {
                Debug.LogWarning(
                    $"{catalogName}: no uxml for {string.Join(", ", MissingUxml)}. " +
                    "Name the file after the id or assign manually.");
            }
        }
    }
}