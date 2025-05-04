using Grasshopper.Kernel;
using Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Block;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;

namespace Graph.Goo
{
    public class TestNodeGoo : GH_GeometricGoo<NodeBase>, IGH_PreviewData
    {
        public double Size = 10;
        private NodeBase _node;
        public override NodeBase Value { get => _node; set => _node = value is NodeBase ? value : null; }
        public BoundingBox ClippingBox => this.Boundingbox;

        public override BoundingBox Boundingbox 
        {
            get
            {
                var Blocks = _node.GetBlocks();
                var Vox = new List<BoundingBox>();
                foreach (var Block in Blocks)
                   Vox.AddRange(Blocks.Select(b => b.Boundingbox));
                var BBox = new BoundingBox();
                foreach (var Block in Vox)
                {
                    BBox.Union(Block);
                }
                return BBox;
            }
        }

        public override string TypeName => "TestNode";

        public override string TypeDescription => "Wrapper for testing NodeBase structures with geometry.";

        public TestNodeGoo(NodeBase node)
        {
            _node = node;
        }
        public TestNodeGoo(IBlockBase blockBase)
        {
            _node = new SNode(blockBase);
        }
        public TestNodeGoo(IEnumerable<IBlockBase> Blocks)
        {
            var Nodes = Blocks.Select(x => new SNode(x));
            _node = new SGNode(1, Nodes);
        }
        public TestNodeGoo(TestNodeGoo testNodeGoo)
        {
            this._node = testNodeGoo._node.Duplicate();
            this.Size = testNodeGoo.Size;
        }
        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            var Blocks = _node.GetBlocks();
            var Voxs = new List<Mesh>();
            foreach (var B in Blocks)
                Voxs.AddRange(B.GetBlocks().Select(x => Mesh.CreateFromBox(x, 1, 1, 1)));

            foreach (var V in Voxs)
            {
                var Display = new Rhino.Display.DisplayMaterial(Color.LightGray, 0.8);
                args.Pipeline.DrawMeshShaded(V, Display);
            }
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            var _nodeTDir = _node.TestDirection();
            var BSize = this._node.BlockSize / 2;

            foreach (var Kvp in _nodeTDir)
            {
                foreach (var DOF in Kvp.Value)
                {
                    var ContactPt = Kvp.Key + DOF * BSize;
                    var DirLine = new Line(ContactPt, ContactPt + DOF * BSize);
                    if (Math.Abs(DOF * Vector3d.XAxis) == 1)
                    {
                        args.Pipeline.DrawArrow(DirLine, Color.Yellow, Size, 1);
                    }
                    else if (Math.Abs(DOF * Vector3d.YAxis) == 1)
                    {
                        args.Pipeline.DrawArrow(DirLine, Color.Green, Size, 1);
                    }
                    else
                    {
                        args.Pipeline.DrawArrow(DirLine, Color.Blue, Size, 1);
                    }
                }
            }
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new TestNodeGoo(this);
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            var BBox = new Box(this.Boundingbox).BoundingBox;
            BBox.Transform(xform);
            return BBox;
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            return DuplicateGeometry();
        }

        public override string ToString()
        {
            return $"TestNodeGoo with {_node?.GetBlocks().Count ?? 0} blocks.";
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            this.Value.Transform(xform);
            return this;
        }
    }
}