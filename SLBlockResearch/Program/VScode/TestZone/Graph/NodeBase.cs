using System.Collections.Generic;
using Block;
using ConstraintDOF;
using Graph.Goo;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Graph
{
    public interface INode
    {
        Dictionary<Point3d, List<Vector3d>> TestDirection();
        int ID {get;}
        List<SEdge> Edges {get;}
        bool IsValid {get;}
        void Transform(Transform XForm);
    }
    public interface HasSubNodes : INode
    {
        List<NodeBase> Children {get;}
        SGraphGoo DisplayNodeGraph {get;}
    }
    public interface IsSingleNode : INode
    {
        IBlockBase blockBase {get;}
    }
    public abstract class NodeBase : INode
    {
        public Transform XForm {get; set;} = Rhino.Geometry.Transform.Identity;
        public double BlockSize => this.GetBlocks()[0].Size;
        public List<NodeBase> Neighbours {get; protected set;}
        public NodeBase Parent {get; private set;}
        public int ID {get; protected set;} = -1;
        public List<SEdge> Edges {get; protected set;}
        public Constraint constraint;
        /// <summary>
        /// If IsValid is false, the node is a toppest level of node and extendable node (graph). 
        /// </summary>
        public virtual bool IsValid => this.ID >=0;
        public NodeBase()
        {
            this.Neighbours = new List<NodeBase>();
            this.Parent = null;
            this.Edges = new List<SEdge>();
        }
        public abstract Point3d GetCentroid();
        
        public virtual void SetParent(NodeBase node)
            => this.Parent = node;
        public virtual NodeBase AddNextNode(NodeBase node, out SEdge newEdge)
        {
            node.Parent = this;
            newEdge = new SEdge(this, node);
            this.Edges.Add(newEdge);
            node.Edges.Add(newEdge);
            node.Neighbours.Add(this);
            this.Neighbours.Add(node);
            return this;
        }
        public override abstract string ToString();
        public override bool Equals(object obj)
        => obj is NodeBase node && this.ID == node.ID;
        public override int GetHashCode()
        => base.GetHashCode();
        public abstract Dictionary<Point3d, List<Vector3d>> TestDirection();
        public abstract List<IBlockBase> GetBlocks();
        public virtual void Transform(Rhino.Geometry.Transform xform)
        {
            this.XForm *= xform;
        }
        public abstract NodeBase Duplicate();
    }
}