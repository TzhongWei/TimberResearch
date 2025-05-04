using Graph;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using System.Drawing;
using System.Runtime.InteropServices;
using Eto;

namespace Graph.Goo
{
    public class SGraphGoo : GH_GeometricGoo<SGraph>, IGH_PreviewData
    {
        public int DisplayThickness = 1;
        private double _arrowsize = 10;
        public double ArrowSize {get => _arrowsize; set {if(value <= 0) _arrowsize = 1; else _arrowsize = value;}}
        public bool ShowDirectedGraph = false;
        public bool ShowInsertDirection = false;
        public SGraphGoo(SGraph sGraph)
        {
            ((GH_Goo<SGraph>)(object)this).Value = sGraph;
        }
        public override BoundingBox Boundingbox
        {
            get
            {
                if (this.Value == null)
                {
                    return BoundingBox.Empty;
                }
                else
                {
                    var Pts = this.Value.Nodes.Select(x => x.Value.GetCentroid()).ToArray();
                    return new BoundingBox(Pts);
                }
            }
        }

        public override string TypeName => "SGraph";

        public override string TypeDescription => "Structural Graph of Interlocking Blocks";

        public BoundingBox ClippingBox => ((GH_GeometricGoo<SGraph>)this).Boundingbox;

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            ///No Mesh is display
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (((GH_Goo<SGraph>)(object)this).Value == null)
            {
                return;
            }
            if (((GH_Goo<SGraph>)(object)this).Value.Edges.Count > 0)
            {
                if (this.ShowDirectedGraph)
                {
                    foreach (LineCurve Crv in ((GH_Goo<SGraph>)(object)this).Value.Edges.Select(x => x.GetEdge(this.Value.XForm)))
                    {
                        args.Pipeline.DrawArrow(Crv.Line, Color.LightPink, ArrowSize, 1);
                    }
                }
                else
                    foreach (LineCurve Crv in ((GH_Goo<SGraph>)(object)this).Value.Edges.Select(x => x.GetEdge(this.Value.XForm)))
                        args.Pipeline.DrawCurve(Crv, Color.Turquoise, args.Thickness + DisplayThickness);

                foreach (var Node in ((GH_Goo<SGraph>)(object)this).Value.Nodes.Values)
                {
                    args.Pipeline.DrawPoint(Node.GetCentroid(), Rhino.Display.PointStyle.Triangle, 3, Color.IndianRed);
                }
            }

        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new SGraphGoo(new SGraph(((GH_Goo<SGraph>)(object)this).Value));
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            var BBox = new Box(this.Boundingbox);
            BBox.Transform(xform);
            return BBox.BoundingBox;
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            // Morphing not implemented for SGraph; return duplicate
            return DuplicateGeometry();
        }

        public override string ToString()
        {
            return $"SGraph with {((GH_Goo<SGraph>)(object)this).Value?.Nodes.Count ?? 0} nodes and {((GH_Goo<SGraph>)(object)this).Value?.Edges.Count ?? 0} edges.";
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            // Transformation not implemented; return duplicate
            this.Value.Transform(xform);
            return this;
        }
        public override bool CastTo<Q>(out Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(SGraph)))
            {
                target = (Q)(object)this.Value;
                return true;
            }
            else
            {
                target = default(Q);
                return false;
            }
        }
        public override bool CastFrom(object source)
        {
            if (source == null)
                return false;
            if (typeof(SGraph).IsAssignableFrom(source.GetType()))
            {
                ((GH_Goo<SGraph>)(object)this).Value = source as SGraph;
                return true;
            }
            return false;
        }
    }
}