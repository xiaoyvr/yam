using System;

namespace Yam.Core
{
    public class ProjectNode : IDependencyNode, IEquatable<ProjectNode>
    {
        public Guid Id { get; set; }
        public AssemblyReference[] AssemblyReferences { get; set; }
        public ProjectReference[] ProjectReferences { get; set; }
        public AssemblyReference[] RuntimeReferences { get; set; }
        public string FullPath { get; set; }
        public string Output { get; set; }

        public bool Equals(ProjectNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Id.Equals(Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ProjectNode)) return false;
            return Equals((ProjectNode) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(ProjectNode left, ProjectNode right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ProjectNode left, ProjectNode right)
        {
            return !Equals(left, right);
        }
        public override string ToString()
        {
            return FullPath;
        }
    }
}