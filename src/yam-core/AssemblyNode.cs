using System;

namespace Yam.Core
{
    public class AssemblyNode : IDependencyNode, IEquatable<AssemblyNode>
    {
        public string FullPath { get; set; }

        public bool Equals(AssemblyNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.FullPath, FullPath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (AssemblyNode)) return false;
            return Equals((AssemblyNode) obj);
        }

        public override int GetHashCode()
        {
            return (FullPath != null ? FullPath.GetHashCode() : 0);
        }

        public static bool operator ==(AssemblyNode left, AssemblyNode right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AssemblyNode left, AssemblyNode right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return FullPath;
        }
    }
}