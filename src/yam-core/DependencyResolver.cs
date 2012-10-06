using System.Collections.Generic;
using System.Linq;
using Yam.Core.Graph;
using Yam.Core.MSProject;

namespace Yam.Core
{
    public class DependencyResolver
    {
        private readonly ResolveConfig resolveConfig;
        private readonly string[] excludeNodes;
        private readonly bool reverse;

        public DependencyResolver(ResolveConfig resolveConfig, string[] excludeNodes = null, bool reverse = false)
        {
            this.resolveConfig = resolveConfig;
            this.excludeNodes = excludeNodes??new string[0];
            this.reverse = reverse;
        }

        public IDictionary<string, string[]> Resolve(string[] inputs, string[] endNodes = null, string runtimeProfile = "")
        {
//            System.Diagnostics.Debugger.Launch();
            var endPaths = endNodes ?? new string[0];
            var resolveContext = new ResolveContext(resolveConfig, new Dictionary<string, ProjectNode>(), runtimeProfile);
            var inputProjects = resolveContext.GetValidProjects(inputs);
            var dagRootProjects = reverse ? resolveContext.GetAllProjects() : inputProjects;

            var dag = new ProjectDAGBuilder(resolveContext, dagRootProjects, excludeNodes).BuildDAG();
            if (endPaths.Length > 0)
            {
                dag.Cut(v => MatchEnd(v.Data, endPaths));
            }

            var result = new Dictionary<string, string[]>();
            foreach (var project in inputProjects)
            {
                if (dag.GetVertex(project) != null)
                {
                    ProcessNode(dag.GetVertex(project), result);
                }
            }
            return result;
        }

        private static bool MatchEnd(IDependencyNode dependencyNode, IEnumerable<string> endPaths)
        {
            return endPaths.Any(p => p == dependencyNode.GetPath());
        }

        private void ProcessNode(Vertex<IDependencyNode> vertex, IDictionary<string, string[]> result)
        {
            var path = vertex.Data.GetPath();
            if (result.ContainsKey(path))
                return;
            var outCommings = new List<string>();
            foreach (var to in GetChildNodes(vertex))
            {
                var toPath = to.Data.GetPath();
                outCommings.Add(toPath);
                ProcessNode(to, result);
            }
            result.Add(path, outCommings.ToArray());
        }

        private IEnumerable<Vertex<IDependencyNode>> GetChildNodes(Vertex<IDependencyNode> vertex)
        {
            return reverse ? vertex.Incommings : vertex.Outcommings;
        }
    }
}