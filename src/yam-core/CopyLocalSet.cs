using System.Linq;

namespace Yam.Core
{
    public class CopyLocalSet
    {
        public ProjectNode Dest { get; set; }
        public IDependencyNode[] CopyLocalSources { get { return ProjectCopySource.Concat(AssemblyCopySource).ToArray(); } }

        public IDependencyNode[] RuntimeCopySources { get; set; }
        public ProjectNode[] ProjectCopySource { get; set; }
        public IDependencyNode[] AssemblyCopySource { get; set; }

        public CopyItemSet CreateCopyTaskItem(DAG<IDependencyNode> dag)
        {
            var copies = GetCopyNodes(dag);
            var copyItemProjectNodes = copies.OfType<ProjectNode>().Select(n => n.FullPath).ToArray();
            var minimizedCopies = copies.Except(CopyLocalSources.Concat(new IDependencyNode[] { Dest })).ToArray();
            var copyProjectNodes = minimizedCopies.OfType<ProjectNode>().Select(n=> n.FullPath).ToArray();
            var copyAssemblyNodes = minimizedCopies.OfType<AssemblyNode>().Select(n => n.FullPath).ToArray();

            var copyItemSet = new CopyItemSet(Dest.FullPath)
                {ItemProjects = copyItemProjectNodes, Projects = copyProjectNodes, Libs = copyAssemblyNodes};
            return copyItemSet;
        }

        private IDependencyNode[] GetCopyNodes(DAG<IDependencyNode> dag)
        {
            var compiletimeCopies = AssemblyCopySource.OfType<ProjectNode>()
                .SelectMany(assemblyNode => new []{assemblyNode}.Concat(dag.GetSubOf(assemblyNode)))
                .Concat(ProjectCopySource.SelectMany(dag.GetSubOf))
                .Distinct();
            var runtimeDependentProjectNodes = RuntimeCopySources.OfType<ProjectNode>();
            var runtimeCopies = runtimeDependentProjectNodes.SelectMany(dag.GetSubOf)
                .Concat(RuntimeCopySources)
                .Distinct();
            return compiletimeCopies.Concat(runtimeCopies).Distinct().ToArray();
        }
    }

    public class CopyItemSet
    {
        public CopyItemSet(string destProject)
        {
            DestProject = destProject;
        }

        public string DestProject { get; private set; }
        public string[] Libs { get; set; }
        public string[] Projects { get; set; }
        public string[] ItemProjects { get; set; }
    }
}