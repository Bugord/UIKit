using System.IO;
using NUnit.Framework;

namespace UIKit.Dev.Tests
{
    public class SampleMirrorTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "uikit-mirror-" + Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(root, "a"));
            Directory.CreateDirectory(Path.Combine(root, "b"));
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(root, true);
        }

        [Test]
        public void SamplesCopy_MatchesExampleSource()
        {
            var diff = SampleMirror.Diff(
                Path.Combine(SampleMirror.ProjectRoot, SampleMirror.Source),
                Path.Combine(SampleMirror.ProjectRoot, SampleMirror.Target));

            Assert.That(diff, Is.Empty,
                $"{SampleMirror.Target} is out of sync with {SampleMirror.Source}:\n" +
                string.Join("\n", diff) +
                "\nRun Tools > UIKit > Mirror Example to Samples~.");
        }

        [Test]
        public void Diff_IdenticalDirectories_IsEmpty()
        {
            Write("a/sub/x.txt", "one");
            Write("b/sub/x.txt", "one");

            Assert.That(SampleMirror.Diff(Path.Combine(root, "a"), Path.Combine(root, "b")), Is.Empty);
        }

        [Test]
        public void Diff_ChangedAndMissingFiles_AreReported()
        {
            Write("a/same.txt", "s");
            Write("b/same.txt", "s");
            Write("a/changed.txt", "one");
            Write("b/changed.txt", "two");
            Write("a/onlySource.txt", "x");
            Write("b/onlyTarget.txt", "y");

            var diff = SampleMirror.Diff(Path.Combine(root, "a"), Path.Combine(root, "b"));

            Assert.That(diff, Is.EquivalentTo(new[] { "changed.txt", "onlySource.txt", "onlyTarget.txt" }));
        }

        private void Write(string relative, string content)
        {
            var path = Path.Combine(root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }
    }
}
