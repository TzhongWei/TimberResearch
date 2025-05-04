using System.Collections.Generic;
using System.Linq;
using Block;
using Rhino.Geometry;

namespace Graph
{
    public class SEdge
    {
        public bool IsDirected => true;
        public NodeBase[] Nodes;
        public NodeBase From => Nodes[0];
        public NodeBase To => Nodes[1];
        /// <summary>
        /// Set up an edge between nodes
        /// </summary>
        /// <param name="From">Parent Node </param>
        /// <param name="To">Child Node </param>
        public SEdge(NodeBase From, NodeBase To)
        {
            this.Nodes = new NodeBase[]{From, To};
        }
        public LineCurve GetEdge(Transform xForm)
        {
            var LineCr = new LineCurve(this.From.GetCentroid(), this.To.GetCentroid());
            LineCr.Transform(xForm);
            return LineCr;
        }
        public override string ToString()
         => $"SEdge From {From} to {To}";
        public override bool Equals(object obj)
        => obj is SEdge edge && edge.From == this.From && edge.To == this.To;
        public override int GetHashCode() => base.GetHashCode();
        public SEdge Duplicate()
        => new SEdge(this.From.Duplicate(), this.To.Duplicate());
    }
}