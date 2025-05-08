using System;
using System.Collections.Generic;
using System.Linq;
using Block;
using Grammar;
using Graph.Goo;
using Rhino.Geometry;

namespace Graph
{
    public class SGraph: NodeBase, HasSubNodes
    {
        public Dictionary<int, NodeBase> Nodes { get; private set; }
        public List<NodeBase> Children => this.Nodes.Values.ToList();
        public bool HasStructed {get; private set;}
        public SGraph()
        {
            this.ID = -1;
            Nodes = new Dictionary<int, NodeBase>();
            Edges = new List<SEdge>();
            HasStructed = false;
        }
        public SGraph(SGraph sGraph)
        {
            this.ID = -1;
            var Dup = sGraph.Duplicate() as SGraph;
            this.Nodes = Dup.Nodes;
            this.Edges = Dup.Edges;
            this.HasStructed = sGraph.HasStructed;
        }
        public override NodeBase Duplicate()
        {
            var NewThis = new SGraph();
            NewThis.ID = -1;
            NewThis.Nodes = this.Nodes.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Duplicate()
            );
            NewThis.Edges = this.Edges.Select(x => x.Duplicate()).ToList();
            NewThis.XForm = this.XForm.Clone();
            NewThis.HasStructed = this.HasStructed;
            return NewThis;
        }
        public SGraph AddNode(NodeBase Node)
        {
            this.Nodes.Add(Node.ID, Node);
            if(!this.HasStructed) this.HasStructed = true;
            return this;
        }
        public SGraph GetSubNodes()
        {
            var DupSGraph = new SGraph();

            void AddChildrenRecursive(NodeBase parent)
            {
                if (parent is HasSubNodes hasSub)
                {
                    foreach (var child in hasSub.Children)
                    {
                        if (!DupSGraph.Nodes.ContainsKey(child.ID))
                        {
                            AddChildrenRecursive(child);
                        }
                    }
                }

                    DupSGraph.AddNode(parent);
                    foreach (var Edge in parent.Edges)
                    {
                        if (!DupSGraph.Edges.Contains(Edge))
                            DupSGraph.Edges.Add(Edge);
                    }
                
            }

            foreach (var node in this.Nodes.Values)
            {
                AddChildrenRecursive(node);
            }

            return DupSGraph;
        }
        public SGraph AddNode(NodeBase Node, int ParentIndex)
        {
            var Parent = this.Nodes.ContainsKey(ParentIndex) ? this.Nodes[ParentIndex] : null;
            if (Parent == null) throw new Exception($"Parent index {ParentIndex} isn't found");
            Parent.AddNextNode(Node, out var Edge);
            this.AddNode(Node);
            this.Edges.Add(Edge);
            if(!this.HasStructed) this.HasStructed = true;
            return this;
        }
        public List<GeometryBase> BakeGraph()
        {
            var List = new List<GeometryBase>();
            List.AddRange(this.Nodes.Select(x => new Point(x.Value.GetCentroid())));
            List.AddRange(this.Edges.Select(x => x.GetEdge(this.XForm)));
            return List;
        }
        public override Point3d GetCentroid()
        {
            var Pt = Point3d.Origin;
            foreach(var Node in this.Children)
            {
                Pt += Node.GetCentroid() / this.Children.Count;
            }
            return Pt;
        }
        public SGNode ToSGNode()
        {
            var MaxID = -1;
            foreach(var Node in this.Nodes)
            {
                if(Node.Value.ID > MaxID)
                   MaxID = Node.Value.ID;
            }
            var id = (this.ID >= 0) ? this.ID : MaxID + 1;
            return new SGNode(id, this.Children);
        }
        public SGraphGoo DisplayNodeGraph => new SGraphGoo(this);
        public override string ToString()
        => $"Graph ID {ID}";
        public override Dictionary<Point3d, List<Vector3d>> TestDirection()
        => this.ToSGNode().TestDirection();
        public override List<IBlockBase> GetBlocks()
        {
            var blockList = new List<IBlockBase>();
            blockList.AddRange(RecursiveGetBlocks(this));

            List<IBlockBase> RecursiveGetBlocks(HasSubNodes node)
            {
                var subBlocks = new List<IBlockBase>();

                foreach (var child in node.Children)
                {
                    if (child is HasSubNodes hasSub)
                        subBlocks.AddRange(RecursiveGetBlocks(hasSub));
                    else
                        subBlocks.AddRange(child.GetBlocks()); 
                }
                return subBlocks;
            }
            return blockList;

        }
    }
}