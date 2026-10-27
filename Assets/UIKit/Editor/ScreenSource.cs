namespace UIKit.Editor
{
    // A screen found in code: a view that needs UXML from a catalog.
    internal sealed class ScreenSource
    {
        public readonly string ScreenId;
        public readonly string AssemblyName;
        public readonly string TypeName;

        public ScreenSource(string screenId, string assemblyName, string typeName)
        {
            ScreenId = screenId;
            AssemblyName = assemblyName;
            TypeName = typeName;
        }
    }
}
