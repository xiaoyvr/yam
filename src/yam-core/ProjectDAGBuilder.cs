using System;
using System.Linq;
using Yam.Core.Graph;
using Yam.Core.MSProject;

namespace Yam.Core
{
    internal class ProjectDAGBuilder
    {
        private readonly ProjectNode[] projectsNode;
        private readonly string[] excludes;
        private readonly ResolveContext context;

        public ProjectDAGBuilder(ResolveContext context, ProjectNode[] projectsNode):
            this(context, projectsNode, new string[0])
        {
        }

        public ProjectDAGBuilder(ResolveContext context, ProjectNode[] projectsNode, string[] excludes)
        {
            this.projectsNode = projectsNode;
            this.excludes = excludes;
            this.context = context;
        }

        public DAG<IDependencyNode> BuildDAG()
        {
            var dag = new DAG<IDependencyNode>();
            Array.ForEach(projectsNode, p => AddIntoDAG(p, dag));
            return dag;
        }

        private void AddIntoDAG(IDependencyNode node, DAG<IDependencyNode> dag)
        {
            if (Excluded(node))
            {
                return;
            }
            dag.Add(node);
            var projectNode = node as ProjectNode;
            if (null == projectNode)
            {
                return;
            }

            var referenceNodes = context.LoadAssemblyNodes(projectNode.AssemblyReferences)
                .Where(n => !Excluded(n)).ToArray();
            Array.ForEach(referenceNodes, p => AddReferenceIntoDAG(p, dag, projectNode));
            var projectReferences = projectNode.ProjectReferences
                .Where(n => !Excluded(n.Node)).ToArray();
            Array.ForEach(projectReferences, p => AddProjectReferenceIntoDAG(dag, p.Node, projectNode));
            var runtimeReferenceNodes = context.LoadAssemblyNodes(projectNode.RuntimeReferences)
                .Where(n => !Excluded(n)).ToArray();
            Array.ForEach(runtimeReferenceNodes, p => AddReferenceIntoDAG(p, dag, projectNode));
        }

        private void AddReferenceIntoDAG(IDependencyNode child, DAG<IDependencyNode> dag, IDependencyNode parent)
        {
            var project = child as ProjectNode;
            if (project != null)
            {
                if (!dag.Contains(project))
                {
                    AddIntoDAG(project, dag);
                }
                dag.AddPath(parent, child);
            }
            var assemblyReference = child as AssemblyNode;
            if (assemblyReference != null)
            {
                dag.AddMergePath(parent, child);
            }
        }

        private void AddProjectReferenceIntoDAG(DAG<IDependencyNode> dag, IDependencyNode p, IDependencyNode project)
        {
            if (!dag.Contains(p))
            {
                AddIntoDAG(p, dag);
            }
            dag.AddMergePath(project, p);
        }

        private bool Excluded(IDependencyNode node)
        {
            var path = node.GetPath();
            return Excluded(path);
        }

        private bool Excluded(string nodePath)
        {
            return excludes.Any(n => n == nodePath);
        }
    }
}