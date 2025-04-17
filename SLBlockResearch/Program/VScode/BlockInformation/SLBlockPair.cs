using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Rhino.UI.Controls;
using Grasshopper.Kernel;
using System.Drawing;
using Grasshopper.Kernel.Types;

namespace Block
{
    public enum PairOption
    {
        Top,
        Bottom,
        Chain
    }
    public class SLBlockPair : GH_GeometricGoo<Point3d>, IGH_PreviewData, IBlockBase
    {
        #region Property
          ///<summary>
        /// Gets or sets the transformation matrix applied to the SLBlock.
        ///</summary>
        public Transform XForm { get; set; }
        ///<summary>
        /// The size dimension of each voxel within the SLBlock.
        ///</summary>
        public double Size { get; set;}
        ///<summary>
        /// The transformation for the voxel blocks
        ///</summary>
        ///<param name="_transforms">transformation forms the SL block.</param>
        ///<returns>The transformation matrix list.</returns>
        private List<Transform> _transforms;
        #endregion Property
        ///<summary>
        /// Accesses a specific voxel within the SLBlock by its index.
        ///</summary>
        ///<param name="index">Index of the voxel to access.</param>
        ///<returns>The voxel at the specified index.</returns>
        public Voxel this[int index]
        {
            get
            {
                if (index < 0 || index >= _transforms.Count)
                    throw new IndexOutOfRangeException();

                var T = _transforms[index];
                var V = new Voxel(this.Size);
                V.Transform(T);
                return V;
            }
            set
            {
                _transforms[index] = value.XForm;
            }
        }
        public SLBlockPair(double Size)
        {
            this.Size = Size;
            this.XForm = Rhino.Geometry.Transform.Identity;
            _transforms = new List<Transform>();
            var VoxelLoc = new List<Vector3d>{
                new Vector3d(-Size * 3 / 2, -Size / 2, -Size / 2),
                new Vector3d(-Size * 3 / 2, -Size / 2, -Size*3 / 2),
                new Vector3d(-Size / 2, -Size / 2, 3*-Size / 2),
                new Vector3d(Size / 2, -Size / 2, -Size * 3 / 2),
                new Vector3d(Size / 2, Size / 2, -Size *3 / 2),
                new Vector3d(Size / 2, Size / 2,-Size / 2),
                new Vector3d(Size * 3 / 2, Size / 2, -Size / 2),
                new Vector3d(Size * 3 / 2, Size / 2, Size / 2)
            };
            var Translation = VoxelLoc.Select(x => Rhino.Geometry.Transform.Translation(x)).ToList();
            _transforms.AddRange(Translation.Select(x => new Transform(x)));
            Translation = Translation.Select(x => Rhino.Geometry.Transform.Rotation(Math.PI, Vector3d.YAxis, Point3d.Origin) * x).ToList();
            _transforms.AddRange(Translation);
        }
        public SLBlockPair(SLBlock block)
        {
            this.Size = block.Size;
            this.XForm = block.XForm;
            this._transforms = new List<Transform>();
            var VoxelLoc = new List<Vector3d>{
                new Vector3d(-Size * 3 / 2, -Size / 2, -Size / 2),
                new Vector3d(-Size * 3 / 2, -Size / 2, -Size*3 / 2),
                new Vector3d(-Size / 2, -Size / 2, 3*-Size / 2),
                new Vector3d(Size / 2, -Size / 2, -Size * 3 / 2),
                new Vector3d(Size / 2, Size / 2, -Size *3 / 2),
                new Vector3d(Size / 2, Size / 2,-Size / 2),
                new Vector3d(Size * 3 / 2, Size / 2, -Size / 2),
                new Vector3d(Size * 3 / 2, Size / 2, Size / 2)
            };
            var Translation = VoxelLoc.Select(x => Rhino.Geometry.Transform.Translation(x)).ToList();
            _transforms.AddRange(Translation.Select(x => new Transform(x)));
            Translation = Translation.Select(x => Rhino.Geometry.Transform.Rotation(Math.PI, Vector3d.YAxis, Point3d.Origin) * x).ToList();
            _transforms.AddRange(Translation);
        }

        public SLBlockPair DuplicateSLBlockPair()
        {
            var NewSL = new SLBlockPair(this.Size);
            NewSL.XForm = this.XForm;
            return NewSL;
        }
        public List<SLBlock> DuplicateSLBlocks()
        {
            var NewSLTop = new SLBlock(this.Size);
            var NewSLBot = new SLBlock(this.Size);
            
            NewSLTop.Transform(this.XForm);
            var Reverse = new Rhino.Geometry.Transform(XForm);
            Reverse = Reverse * Rhino.Geometry.Transform.Rotation(Math.PI, Vector3d.YAxis, Point3d.Origin);
            NewSLBot.Transform(Reverse);
            return new List<SLBlock>(){NewSLTop, NewSLBot};
        }
        private List<Transform> GetTransforms(PairOption option = PairOption.Chain)
        {
            switch (option)
            {
                case PairOption.Chain:
                    return this._transforms;
                case PairOption.Top:
                    return this._transforms.GetRange(8, 8);
                case PairOption.Bottom:
                    return this._transforms.GetRange(0, 8);
            }
            return null;
        }
        #region GetGeometry
        public List<Box> GetSLBLock(PairOption options = PairOption.Chain)
        {
            var Voxel = new Voxel(Size);
            var Boxes = GetTransforms(options).Select(t =>
                    {
                        var V = Voxel.DuplicateVoxel();
                        V.Transform(XForm);
                        V.Transform(t);
                        return V.VoxelDisplay();
                    }
                    ).ToList();
            return Boxes;
        }
        public List<Point3d> GetSLBlockGraphNode(PairOption options = PairOption.Chain)
        {
            return GetTransforms(options).Select(t =>
            {
                var Pt = Point3d.Origin;
                Pt.Transform(t);
                Pt.Transform(XForm);
                return Pt;
            }).ToList();
        }
        public List<Curve> GetSLBlockGraphEdge(PairOption option = PairOption.Chain)
        {
            if (option == PairOption.Chain)
            {
                var NodesBot = this.GetSLBlockGraphNode(PairOption.Bottom);
                var NodesTop = this.GetSLBlockGraphNode(PairOption.Top);
                var Edge = new PolylineCurve(NodesBot).DuplicateSegments().ToList();
                Edge.AddRange(new PolylineCurve(NodesTop).DuplicateSegments());
                return Edge;

            }
            else
            {
                var Nodes = this.GetSLBlockGraphNode(option);
                return new PolylineCurve(Nodes).DuplicateSegments().ToList();
            }
        }
        #endregion GetGeometry

        public override string ToString()
        {
            var Location = Point3d.Origin;
            Location.Transform(this.XForm);
            return $"SLBlockPair \n Location : {Location.ToString()}, Size : {Size}";
        }

        public override string TypeName => "SLBlockPair";
        public override string TypeDescription => "A pair of SLBlocks with optional bottom/top/chain configuration.";

        public override BoundingBox Boundingbox
        {
            get
            {
                BoundingBox box = BoundingBox.Empty;
                foreach (var b in this.GetSLBLock())
                    box.Union(b.BoundingBox);
                return box;
            }
        }

        public BoundingBox ClippingBox => this.Boundingbox;
        public override BoundingBox GetBoundingBox(Transform xform)
        {
            var boxes = this.GetSLBLock();
            BoundingBox box = BoundingBox.Empty;
            foreach (var b in boxes)
            {
                var transformedBox = new Box(b);
                transformedBox.Transform(xform);
                box.Union(transformedBox.BoundingBox);
            }
            return box;
        }
        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return this.DuplicateSLBlockPair();
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            this.XForm *= xform;
            return this;
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            var points = this.GetSLBlockGraphNode().Select(p => xmorph.MorphPoint(p)).ToList();
            var copy = new SLBlockPair(this.Size);
            copy._transforms = points.Select(p => Rhino.Geometry.Transform.Translation(new Vector3d(p))).ToList();
            return copy;
        }

        public override bool CastFrom(object source)
        {
            if (source == null) return false;

            if (source is Point3d pt)
            {
                this.XForm = Rhino.Geometry.Transform.Translation(new Vector3d(pt));
                Value = (Point3d)source;
                return true;
            }

            if (source is GH_Point ghPt)
            {
                this.XForm = Rhino.Geometry.Transform.Translation(new Vector3d(ghPt.Value));
                Value = (Point3d)source;
                return true;
            }

            Point3d point = Point3d.Unset;
            if (GH_Convert.ToPoint3d(source, ref point, GH_Conversion.Both))
            {
                this.XForm = Rhino.Geometry.Transform.Translation(new Vector3d(point));
                Value = (Point3d)source;
                return true;
            }

            return false;
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            foreach (var Pt in this.GetSLBlockGraphNode())
                args.Pipeline.DrawPoint(Pt, args.Color);

            var PL = Plane.WorldXY; PL.Transform(this.XForm);
            args.Pipeline.DrawPoint(PL.Origin, Rhino.Display.PointStyle.Pin, 3, Color.Blue);

            args.Pipeline.DrawArrow(new Line(PL.Origin, PL.Origin + PL.XAxis * 0.2 * Size), Color.Red);
            args.Pipeline.DrawArrow(new Line(PL.Origin, PL.Origin + PL.YAxis * 0.2 * Size), Color.Green);
            args.Pipeline.DrawArrow(new Line(PL.Origin, PL.Origin + PL.ZAxis * 0.2 * Size), Color.Yellow);

            foreach (var Crv in this.GetSLBlockGraphEdge())
            {
                args.Pipeline.DrawCurve(Crv, args.Color, 4);
            }
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            foreach (var b in this.GetSLBLock(PairOption.Top))
            {
                var mesh = Mesh.CreateFromBox(b, 2, 2, 2);
                var material = new Rhino.Display.DisplayMaterial(System.Drawing.Color.Yellow, 0.8);
                args.Pipeline.DrawMeshShaded(mesh, material);
            }
            foreach (var b in this.GetSLBLock(PairOption.Bottom))
            {
                var mesh = Mesh.CreateFromBox(b, 2, 2, 2);
                var material = new Rhino.Display.DisplayMaterial(System.Drawing.Color.Blue, 0.8);
                args.Pipeline.DrawMeshShaded(mesh, material);
            }
        }

        //Override
        public override object ScriptVariable()
        {
            return Value;
        }
        public override Point3d Value
        {
            get
            {
                var Pt = Point3d.Origin;
                Pt.Transform(XForm);
                return Pt;
            }
            set
            {
                this.XForm = Rhino.Geometry.Transform.Translation(new Vector3d(value));
            }
        }
        public override bool CastTo<TQ>(out TQ target)
        {
            if (typeof(TQ).IsAssignableFrom(typeof(Point3d)))
            {
                Value.Transform(this.XForm);
                target = (TQ)(object)Value;
                return true;
            }

            if (typeof(TQ).IsAssignableFrom(typeof(GH_Point)))
            {
                Value.Transform(this.XForm);
                target = (TQ)(object)new GH_Point(Value);
                return true;
            }

            target = default(TQ);
            return false;
        }
    }
}