using System.Collections.Generic;
using System;
using System.Linq;
using Rhino.Geometry;
using System.CodeDom;
using Block;
using Graph.Goo;
using System.Collections.Concurrent;

namespace Graph
{
    public class SGNode : NodeBase, HasSubNodes
    {
        /// <summary>
        /// Get the children without XForm
        /// </summary>
        public List<NodeBase> Children { get; private set; }
        public override bool IsValid => Children.Count != 0 && this.ID >= 0;
        public bool HasStructed => false;
        private SGNode() : base()
        { }
        public SGNode(int ID, IEnumerable<NodeBase> Nodes) : this()
        {
            this.ID = ID;
            this.Children = new List<NodeBase>();
            foreach (NodeBase Node in Nodes)
            {
                this.Children.Add(Node);
                Node.SetParent(this);
                Node.Neighbours.Add(this);
                var Edge = new SEdge(this, Node);
                Node.Edges.Add(Edge);
                this.Edges.Add(Edge);
            }
        }
        public override NodeBase Duplicate()
        {
            var NewThis = new SGNode(ID, this.Children.Select(x => x.Duplicate()));
            NewThis.XForm = this.XForm.Clone();
            return NewThis;
        }
        public SGraphGoo DisplayNodeGraph => new SGraphGoo(this.ToSGraph());
        public SGraph ToSGraph()
        {
            var sGraph = new SGraph();
            foreach (var child in Children)
            {
                sGraph.AddNode(child, this.ID);
            }
            return sGraph;
        }
        public override Point3d GetCentroid()
        {
            var Pt = Point3d.Origin;
            foreach (var Node in Children)
            {
                Pt += Node.GetCentroid() / Children.Count;
            }
            return Pt;
        }
        public override string ToString()
           => $"SGNode {this.ID} Contains {string.Join(", ", Children)}";
        public override Dictionary<Point3d, List<Vector3d>> TestDirection()
        {
            var nodePositions = new List<Point3d>();
            foreach (var voxel in this.GetBlocks().SelectMany(b => b.GetBlockGraphNode()))
                nodePositions.Add(voxel);

            var size = this.BlockSize;
            var testDir = new Dictionary<Point3d, List<Vector3d>>();

            foreach (var childTestDirs in this.Children.Select(x => x.TestDirection()))
            {
                foreach (var kv in childTestDirs)
                {
                    var basePt = kv.Key;
                    var dirs = kv.Value;

                    var filteredDirs = new List<Vector3d>();
                    foreach (var dir in dirs)
                    {
                        var testPt = basePt + dir * size;
                        bool hasNeighbor = nodePositions.Any(np => np.DistanceTo(testPt) < 0.01);
                        if (!hasNeighbor)
                            filteredDirs.Add(dir);
                    }
                    testDir[basePt] = filteredDirs;
                }
            }
            return testDir;
        }
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
            blockList = blockList.Select(x => (IBlockBase)x.ApplyWorldTransform(this.XForm)).ToList();
            return blockList;
        }
    }
}