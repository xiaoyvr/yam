using System;
using System.IO;
using Xunit;
using Xunit.Extensions;
using Yam.Core;

namespace UT.ReferenceResolve.Task
{
    public class RefinablePathFacts
    {
        [Fact]
        public void should_refine_by_hint()
        {
            const string lib1Path = "c:\\lib1\\a.dll";
            const string lib2Path = "c:\\lib2\\a.dll";
            var refinablePath = new RefinablePath("a", new[] { lib1Path, lib2Path });
            Assert.Throws<ApplicationException>(() => refinablePath.Refine(string.Empty, Path.GetDirectoryName("c:/src/test.csproj")));
        }

        [Theory]
        [InlineData("../lib1/a.dll", "c:\\lib1\\a.dll")]
        [InlineData("../lib2/a.dll", "c:\\lib2\\a.dll")]
        public void should_refine(string hintPath, string result)
        {
            const string lib1Path = "c:\\lib1\\a.dll";
            const string lib2Path = "c:\\lib2\\a.dll";
            var refinablePath = new RefinablePath("a", new[] { lib1Path, lib2Path });
            Assert.Equal(result, refinablePath.Refine(hintPath, Path.GetDirectoryName("c:/src/test.csproj")));
        }
    }
}