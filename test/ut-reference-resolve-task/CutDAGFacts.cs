using Xunit;
using Yam.Core;
using Yam.Core.Graph;

namespace UT.ReferenceResolve.Task
{
    public class CutDAGFacts
    {
        [Fact]
        public void should_cut()
        {
            var graph = new DAG<int>();
            graph.AddPath(1, 2);
            graph.AddPath(1, 3);
            graph.AddPath(3, 2);
            graph.AddPath(3, 4);
            graph.Cut(v => v.Data == 2);

            var ints = graph.Out();
            Assert.DoesNotContain(4, ints);
        }

        [Fact]
        public void should_cut_to_empty()
        {
            var graph = new DAG<int>();
            graph.AddPath(1, 2);
            graph.AddPath(1, 3);
            graph.AddPath(3, 2);
            graph.AddPath(3, 4);
            graph.Cut(v => v.Data == 9);

            var ints = graph.Out();
            Assert.Empty(ints);
        }
    }
}