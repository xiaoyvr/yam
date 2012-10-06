using System.Collections.Generic;

namespace Yam.Core.Graph
{
    internal class Vertex<T>
    {
        public Vertex(T data)
        {
            Data = data;
            Outcommings = new HashSet<Vertex<T>>();
            Incommings = new HashSet<Vertex<T>>();
        }

        public bool Accessed { get; set; }

        public T Data { get; private set; }

        public HashSet<Vertex<T>> Incommings { get; private set; }

        public HashSet<Vertex<T>> Outcommings { get; private set; }

        public void AddOutcomming(Vertex<T> vertex)
        {
            if (Equals(vertex, this))
            {
                return;
            }
            Outcommings.Add(vertex);
        }
        
        public void AddIncomming(Vertex<T> vertex)
        {
            if (Equals(vertex, this))
            {
                return;
            }
            Incommings.Add(vertex);
        }

        public override string ToString()
        {
            return "Vertex, " + Data;
        }

        public bool Equals(Vertex<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Data, Data);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Vertex<T>)) return false;
            return Equals((Vertex<T>) obj);
        }

        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }
    }

    internal static class Extensions
    {
        public static IEnumerable<Vertex<T>> GetSubVertices<T>(this Vertex<T> vertex)
        {
            foreach (var outcomming in vertex.Outcommings)
            {
                if (!outcomming.Accessed)
                {
                    outcomming.Accessed = true;
                    yield return outcomming;
                    foreach (var subVertex in GetSubVertices(outcomming))
                    {
                        subVertex.Accessed = true;
                        yield return subVertex;
                    }
                }
            }
        }

        public static bool CanReach<T>(this Vertex<T> from, Vertex<T> to)
        {
            if (from.Equals(to))
            {
                return false;
            }
            foreach (var outcomming in from.Outcommings)
            {
                if (!outcomming.Accessed)
                {
                    outcomming.Accessed = true;
                    if (outcomming.Equals(to))
                    {
                        return true;
                    }
                    if (outcomming.CanReach(to))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public static bool IsEnd<T>(this Vertex<T> vertex)
        {
            return vertex.Outcommings.Count <= 0;
        }
    }
}