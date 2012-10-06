using System.Linq;
using Xunit;
using Yam.Core;

namespace Test
{
    public class DAGFacts
    {
        [Fact]
        public void should_get_correct_count()
        {
            var graph = new DAG<int>();
            Assert.Equal(graph.Out().Count(), 0);

            graph.Add(0);
            Assert.Equal(graph.Out().Count(), 1);
        }

        [Fact]
        public void should_detect_cycle()
        {
            var graph = new DAG<int>();
            graph.AddPath(0, 1);
            Assert.Throws<CyclePathException>(() => graph.AddPath(1, 0));
        }

        [Fact]
        public void should_get_vertexes_for_simple_direct()
        {
            var graph = new DAG<int>();
            const int from = 0;
            const int to = 1;
            graph.Add(from);
            graph.AddPath(from, to);

            var items = graph.Out();
            Assert.Equal(2, items.Length);
            Assert.Equal(to, items[0]);
            Assert.Equal(from, items[1]);
        }        
        
        [Fact]
        public void should_get_sub_vertexes_for_one()
        {
            var graph = new DAG<int>();
            const int from = 0;
            const int to = 1;
            graph.Add(from);
            graph.AddPath(from, to);

            var subOfTo = graph.GetSubOf(to);
            Assert.Equal(0, subOfTo.Length);

            var subOfFrom = graph.GetSubOf(from);
            Assert.Equal(1, subOfFrom.Length);
            Assert.Equal(to, subOfFrom[0]);
        }        
        
        [Fact]
        public void should_get_sub_vertexes_for_square()
        {
            var graph = new DAG<int>();
            const int v0 = 0;
            const int v1 = 1;
            const int v2 = 2;
            const int v3 = 3;
            graph.AddPath(v0, v1);
            graph.AddPath(v0, v2);
            graph.AddPath(v1, v3);
            graph.AddPath(v2, v3);

            var subOf0 = graph.GetSubOf(v0);
            Assert.Equal(3, subOf0.Length);
        }        
        
        [Fact]
        public void should_get_sub_vertexes_for_square_more()
        {
            var graph = new DAG<int>();
            const int v0 = 0;
            const int v1 = 1;
            const int v2 = 2;
            const int v3 = 3;
            graph.AddPath(v0, v1);
            graph.AddPath(v0, v2);
            graph.AddPath(v1, v3);
            graph.AddPath(v2, v3);
            graph.AddPath(v0, v3);
            graph.AddPath(v1, v2);

            var subOf0 = graph.GetSubOf(v0);
            Assert.Equal(3, subOf0.Length);
        }

        [Fact]
        public void should_get_vertexes_for_complex_direct()
        {
            var graph = new DAG<int>();
            const int v1 = 1;
            const int v2 = 2;
            const int v3 = 3;
            const int v4 = 4;
            graph.AddPath(v1, v2);
            graph.AddPath(v1, v3);
            graph.AddPath(v1, v4);
            graph.AddPath(v3, v2);
            graph.AddPath(v2, v4);
            graph.AddPath(v3, v4);

            var items = graph.Out();
            Assert.Equal(4, items.Length);
            Assert.Equal(v4, items[0]);
            Assert.Equal(v2, items[1]);
            Assert.Equal(v3, items[2]);
            Assert.Equal(v1, items[3]);
        }

        [Fact]
        public void should_simplify()
        {
            var graph = new DAG<int>();
            const int v1 = 1;
            const int v2 = 2;
            const int v3 = 3;
            const int v4 = 4;
            graph.AddPath(v1, v2);
            graph.AddPath(v1, v3);
            graph.AddPath(v1, v4);
            graph.AddMergePath(v3, v2);
            graph.AddPath(v2, v4);
            graph.AddPath(v3, v4);

            graph.Simplify();

            Assert.Null(graph.Get(v2));
            var incommings = graph.Get(v3).Incommings;
            Assert.Equal(1, incommings.Count);
            Assert.Equal(v1, incommings.First().Data);
            var outcommings = graph.Get(v3).Outcommings;
            Assert.Equal(1, outcommings.Count);
            Assert.Equal(v4, outcommings.First().Data);
        }

        [Fact]
        public void should_simplify_complex()
        {
            var graph = new DAG<int>();
            const int v1 = 1;
            const int v2 = 2;
            const int v3 = 3;
            const int v4 = 4;
            graph.AddPath(v1, v2);
            graph.AddPath(v1, v3);
            graph.AddPath(v1, v4);
            graph.AddMergePath(v3, v2);
            graph.AddPath(v3, v4);
            graph.AddPath(v4, v2);

            graph.Simplify();

            Assert.NotNull(graph.Get(v2));
            Assert.Equal(3, graph.Get(v2).Incommings.Count);
            Assert.Equal(0, graph.Get(v2).Outcommings.Count);
            Assert.Equal(1, graph.Get(v3).Incommings.Count);
            Assert.Equal(2, graph.Get(v3).Outcommings.Count);
        }
        
        [Fact]
        public void should_deal_with_merge_path()
        {
            var graph = new DAG<int>();
            const int v1 = 1;
            const int v2 = 2;
            const int v3 = 3;
            graph.AddMergePath(v1, v2);
            graph.AddPath(v1, v3);
            graph.AddMergePath(v2, v3);

            graph.Simplify();

            Assert.Null(graph.Get(v2));
            Assert.Equal(0, graph.Get(v1).Incommings.Count);
            Assert.Equal(1, graph.Get(v1).Outcommings.Count);
            Assert.Equal(1, graph.Get(v3).Incommings.Count);
            Assert.Equal(0, graph.Get(v3).Outcommings.Count);
        }

        [Fact]
        public void should_simplify_all_virtual()
        {
            var graph = new DAG<string>();
            const string extensions = "extensions";
            const string modelApi = "model-api";
            const string persistenceApi = "persistence-api";
            const string containerApi = "container-api";
            graph.AddMergePath(modelApi, extensions);
            graph.AddMergePath(persistenceApi, extensions);
            graph.AddMergePath(persistenceApi, modelApi);
            graph.AddMergePath(containerApi, extensions);
            graph.Simplify();
            var vertexes = graph.Out();
            Assert.Equal(2, vertexes.Length);
            Assert.Equal(persistenceApi, vertexes[0]);
            Assert.Equal(containerApi, vertexes[1]);
        }

        [Fact]
        public void should_simplify_more_complex()
        {
            var graph = new DAG<string>();

            const string securityCore = "security-core";
            const string securityCoreWeb = "security-core-web";
            const string extensions = "extensions";
            const string container = "container";
            const string containerApi = "container-api";
            const string web = "web";
            const string modelApi = "model-api";
            const string loggingApi = "logging-api";
            const string loggingSpi = "logging-spi";
            const string logging = "logging";
            const string persistenceApi = "persistence-api";
            const string templateApi = "template-api"; 
            const string persistenceSpi = "persistence-spi";
            const string template = "template";
            const string templateSpi = "template-spi";

            graph.AddPath(securityCore, extensions);
            graph.AddPath(securityCoreWeb, container);
            graph.AddPath(securityCoreWeb, containerApi);
            graph.AddPath(securityCoreWeb, extensions);
            graph.AddPath(securityCoreWeb, web);

            graph.AddMergePath(container, containerApi);
            graph.AddMergePath(container, extensions);
            graph.AddMergePath(container, loggingApi);

            graph.AddMergePath(containerApi, extensions);

            graph.AddMergePath(loggingApi, extensions);
            graph.AddMergePath(loggingApi, modelApi);

            graph.AddMergePath(modelApi, extensions);

            graph.AddMergePath(web, containerApi);
            graph.AddMergePath(web, container);
            graph.AddMergePath(web, extensions);
            graph.AddMergePath(web, loggingApi);
            graph.AddMergePath(web, loggingSpi);
            graph.AddMergePath(web, logging);
            graph.AddMergePath(web, modelApi);
            graph.AddMergePath(web, persistenceApi);
            graph.AddMergePath(web, templateSpi);
            graph.AddMergePath(web, template);

            graph.AddMergePath(loggingSpi, containerApi);
            graph.AddMergePath(loggingSpi, container);
            graph.AddMergePath(loggingSpi, extensions);
            graph.AddMergePath(loggingSpi, loggingApi);
            graph.AddMergePath(loggingSpi, modelApi);
            graph.AddMergePath(loggingSpi, persistenceApi);
            graph.AddMergePath(loggingSpi, persistenceSpi);

            graph.AddMergePath(persistenceApi, extensions);
            graph.AddMergePath(persistenceApi, modelApi);

            graph.AddMergePath(persistenceSpi, containerApi);
            graph.AddMergePath(persistenceSpi, extensions);
            graph.AddMergePath(persistenceSpi, modelApi);
            graph.AddMergePath(persistenceSpi, persistenceApi);

            graph.AddMergePath(logging, containerApi);
            graph.AddMergePath(logging, container);
            graph.AddMergePath(logging, extensions);
            graph.AddMergePath(logging, loggingApi);
            graph.AddMergePath(logging, loggingSpi);
            graph.AddMergePath(logging, modelApi);
            graph.AddMergePath(logging, persistenceApi);
            graph.AddMergePath(logging, persistenceSpi);
            graph.AddMergePath(logging, templateApi);
            graph.AddMergePath(logging, template);

            graph.AddMergePath(templateApi, extensions);

            graph.AddMergePath(template, containerApi);
            graph.AddMergePath(template, container);
            graph.AddMergePath(template, extensions);
            graph.AddMergePath(template, templateApi);
            graph.AddMergePath(template, templateSpi);

            graph.AddMergePath(templateSpi, container);
            graph.AddMergePath(templateSpi, extensions);
            graph.AddMergePath(templateSpi, templateApi);

            graph.Simplify();

            Assert.NotNull(graph.Get(securityCore));
            Assert.NotNull(graph.Get(securityCoreWeb));
            Assert.NotNull(graph.Get(web));
            var vertexes = graph.Out();
            Assert.Equal(3, vertexes.Length);
            Assert.Equal(web, vertexes[0]);
            Assert.Equal(securityCore, vertexes[1]);
            Assert.Equal(securityCoreWeb, vertexes[2]);
        }
    }
}
