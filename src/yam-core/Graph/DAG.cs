using System;
using System.Collections.Generic;
using System.Linq;

namespace Yam.Core.Graph
{
    internal class DAG<T>
    {
        private readonly IDictionary<T, Vertex<T>> vertices;
        // TODO: merge path should be a part of a vertex
        private readonly HashSet<Path> mergePaths;

        public DAG()
        {
            vertices = new Dictionary<T, Vertex<T>>();
            mergePaths = new HashSet<Path>();
        }

        public Vertex<T>[] GetVertices()
        {
            return vertices.Values.ToArray();
        }

        public void Add(T vData)
        {
            AddAndGetVertex(vData);
        }

        private Vertex<T> AddAndGetVertex(T vData)
        {
            if (!vertices.ContainsKey(vData))
            {
                vertices.Add(vData, new Vertex<T>(vData));                
            }
            return vertices[vData];
        }

        public void Simplify()
        {
            DoMerge();
        }

        private void DoMerge()
        {
            Path path;
            while (!Equals(path = mergePaths.FirstOrDefault(IsUnique), default(Path)))
            {
                var from = path.From;
                var to = path.To;
                UpdateMergePath(path);
                to.Outcommings.ToList().ForEach(o => {
                    o.AddIncomming(from);
                    from.AddOutcomming(o);
                });
                to.Incommings.ToList().ForEach(i => {
                    i.AddOutcomming(from);
                    from.AddIncomming(i);
                });
                Remove(to);
            }
        }

        private void UpdateMergePath(Path mergedPath)
        {
            mergePaths.Remove(mergedPath);
            var from = mergedPath.From;
            var to = mergedPath.To;
            var mergePathsFromToNode = mergePaths.Where(p => Equals(p.From, to)).ToList();
            var mergePathsToToNode = mergePaths.Where(p => Equals(p.To, to)).ToList();


            mergePathsFromToNode.ToList().ForEach(p=> {
                var hasPath = from.Outcommings.Contains(p.To);
                mergePaths.Remove(p);
                if (!hasPath)
                {
                    p.From = from;
                    mergePaths.Add(p);
                }
            });
            // invaild
            mergePathsToToNode.ToList().ForEach(p => mergePaths.Remove(p));
        }

        private bool IsUnique(Path path)
        {
            return !path.From.Outcommings.Any(o => CanReach(o, path.To));
        }

        private bool CanReach(Vertex<T> from, Vertex<T> to)
        {
            ResetAccessed();
            return from.CanReach(to);
        }

        public T[] Out()
        {
            var result = new List<T>();
            while (vertices.Count != 0)
            {
                var vertex = vertices.First(p=> p.Value.IsEnd()).Value;
                Remove(vertex);
                result.Add(vertex.Data);
            }

            return result.ToArray();
        }

        private void Remove(Vertex<T> vertex)
        {
            vertex.Incommings.ToList().ForEach(i=> i.Outcommings.Remove(vertex));
            vertex.Outcommings.ToList().ForEach(o=> o.Incommings.Remove(vertex));
            vertices.Remove(vertex.Data);
        }

        public void Remove(T t)
        {
            var vertex = GetVertex(t);
            Remove(vertex);
        }

        public void AddPath(T from, T to)
        {
            var fromVertex = AddAndGetVertex(from);
            var toVertex = AddAndGetVertex(to);
            AddPathInternal(fromVertex, toVertex);
        }

        private void AddPathInternal(Vertex<T> fromVertex, Vertex<T> toVertex)
        {
            ValidateCycle(fromVertex, toVertex);
            fromVertex.AddOutcomming(toVertex);
            toVertex.AddIncomming(fromVertex);
        }

        private void ValidateCycle(Vertex<T> from, Vertex<T> to)
        {
            if(CanReach(to, from))
            {
                throw new CyclePathException(string.Format("Add path {0} -> {1} will create a cycle path in DAG. ", from, to));
            }
        }

        public bool Contains(T vData)
        {
            return vertices.ContainsKey(vData);
        }

        public Vertex<T> Get(T data)
        {
            return vertices.ContainsKey(data) ? vertices[data] : null;
        }

        public void AddMergePath(T from, T to)
        {
            AddPath(from, to);
            mergePaths.Add(new Path {From = Get(from), To = Get(to) });
        }

        public struct Path
        {
            public Vertex<T> From { get; set; }
            public Vertex<T> To { get; set; }
        }

        public T[] GetSubOf(T root)
        {
            var vRoot = vertices[root];
            return GetSubOf(vRoot).Select(v => v.Data).ToArray();
        }

        private IEnumerable<Vertex<T>> GetSubOf(Vertex<T> root)
        {
            ResetAccessed();
            return root.GetSubVertices().Distinct();
        }

        public Vertex<T> GetVertex(T key)
        {
            return vertices.ContainsKey(key) ? vertices[key] : null;
        }

        private void ResetAccessed()
        {
            foreach (var dictionary in vertices)
            {
                dictionary.Value.Accessed = false;
            }
        }

        public void Cut(Func<Vertex<T>, bool> isEdge)
        {
            Vertex<T> vertex;
            while ((vertex = vertices.Values.FirstOrDefault(v => v.IsEnd() && !isEdge(v))) != null)
            {
                Remove(vertex);
            }  
        }
    }
}