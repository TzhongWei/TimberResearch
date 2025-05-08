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
    public class SLBlockPair : GH_GeometricGoo<Plane>, IBlockBase
    {
        #region Property
        ///<summary>
        /// Gets or sets the transformation matrix applied to the SLBlock.
        ///</summary>
        public Transform XForm { get; private set; }
        ///<summary>
        /// The size dimension of each voxel within the SLBlock.
        ///</summary>
        public double Size { get; }
        ///<summary>
        /// The transformation for the voxel blocks
        ///</summary>
        ///<param name="_transforms">transformation forms the SL block.</param>
        ///<returns>The transformation matrix list.</returns>
        public BlockAttribute attribute { get; private set; }
        private readonly List<Transform> _transforms;
        private static List<Transform> SetUpTS(double Size)
        {
            var _T = new List<Transform>();
            var VoxelLoc = new List<Vector3d>{
                new Vector3d(Size * 3 / 2, Size / 2, -Size / 2),
                new Vector3d(Size * 3 / 2, Size / 2, -Size*3 / 2),
                new Vector3d(Size / 2, Size / 2, 3*-Size / 2),
                new Vector3d(-Size / 2, Size / 2, -Size * 3 / 2),
                new Vector3d(-Size / 2, -Size / 2, -Size *3 / 2),
                new Vector3d(-Size / 2, -Size / 2,-Size / 2),
                new Vector3d(-Size * 3 / 2, -Size / 2, -Size / 2),
                new Vector3d(-Size * 3 / 2, -Size / 2, Size / 2)
            };
            var Translation = VoxelLoc.Select(x => Rhino.Geometry.Transform.Translation(x)).ToList();
            _T.AddRange(Translation.Select(x => x.Clone()));
            Translation = Translation.Select(x => Rhino.Geometry.Transform.Rotation(Math.PI, Vector3d.YAxis, Point3d.Origin) * x).ToList();
            _T.AddRange(Translation);
            return _T;
        }
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
            this.attribute = new BlockAttribute();
            this._transforms = SetUpTS(Size);
            this.SetIdenifier(this.TypeName);
        }
        public SLBlockPair(SLBlockPair block)
        {
            this.Size = block.Size;
            this.XForm = block.XForm.Clone();
            this.attribute = new BlockAttribute(this.attribute);
            this._transforms = SetUpTS(Size);
            this.SetIdenifier(block.attribute.Identifier);
        }
        public void SetIdenifier(string ID)
        {
            this.attribute.Identifier = ID;
        }
        public void SetColor(Color colour)
        {
            this.attribute.BlockColor = colour;
        }
        public IBlockBase DuplicateBlock()
        {
            var NewSL = new SLBlockPair(this);
            return NewSL;
        }
        public BlockList<SLBlock> DuplicateBlocks(PairOption option = PairOption.Chain)
        {
            var NewSLTop = new SLBlock(this.Size);
            var NewSLBot = new SLBlock(this.Size);

            NewSLTop.Transform(this.XForm);
            var Reverse = new Rhino.Geometry.Transform(XForm);
            Reverse = Reverse * Rhino.Geometry.Transform.Rotation(Math.PI, Vector3d.YAxis, Point3d.Origin);
            NewSLBot.Transform(Reverse);

            switch (option)
            {
                case PairOption.Chain:
                    return new BlockList<SLBlock> { NewSLTop, NewSLBot };
                case PairOption.Top:
                    return new BlockList<SLBlock> { NewSLTop };
                case PairOption.Bottom:
                    return new BlockList<SLBlock> { NewSLBot };
                default:
                    return new BlockList<SLBlock> { NewSLTop, NewSLBot };
            }
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
        public List<Box> GetBlocks() => this.GetBlocks(PairOption.Chain);
        public Brep GetUnionBlock()
        {
            var B = Brep.CreateBooleanUnion(this.GetBlocks().Select(x => x.ToBrep()), 0.1)[0];
            B.MergeCoplanarFaces(1);
            return B;
        }
        public List<Box> GetBlocks(PairOption options)
        {
            var Voxel = new Voxel(Size);
            var Boxes = GetTransforms(options).Select(t =>
                    {
                        var V = (Voxel)Voxel.DuplicateBlock();
                        V.Transform(XForm);
                        V.Transform(t);
                        return V.GetBlocks()[0];
                    }
                    ).ToList();
            return Boxes;
        }
        public List<Point3d> GetBlockGraphNode() => this.GetBlockGraphNode(PairOption.Chain);
        public List<Point3d> GetBlockGraphNode(PairOption options)
        {
            return GetTransforms(options).Select(t =>
            {
                var Pt = Point3d.Origin;
                Pt.Transform(t);
                Pt.Transform(XForm);
                return Pt;
            }).ToList();
        }
        public List<Curve> GetBlockGraphEdge() => this.GetBlockGraphEdge(PairOption.Chain);
        public List<Curve> GetBlockGraphEdge(PairOption option)
        {
            if (option == PairOption.Chain)
            {
                var NodesBot = this.GetBlockGraphNode(PairOption.Bottom);
                var NodesTop = this.GetBlockGraphNode(PairOption.Top);
                var Edge = new PolylineCurve(NodesBot).DuplicateSegments().ToList();
                Edge.AddRange(new PolylineCurve(NodesTop).DuplicateSegments());
                return Edge;

            }
            else
            {
                var Nodes = this.GetBlockGraphNode(option);
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
                foreach (var b in this.GetBlocks())
                    box.Union(b.BoundingBox);
                return box;
            }
        }

        public BoundingBox ClippingBox => this.Boundingbox;
        public override BoundingBox GetBoundingBox(Transform xform)
        {
            var boxes = this.GetBlocks();
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
            return (SLBlockPair)this.DuplicateBlock();
        }
        public override IGH_GeometricGoo Transform(Transform xform)
        {
            this.XForm *= xform;
            return this;
        }
        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            var points = this.GetBlockGraphNode().Select(p => xmorph.MorphPoint(p)).ToList();
            var copy = new SLBlockPair(this.Size);
            return copy;
        }
        public IGH_GeometricGoo ApplyWorldTransform(Transform world)
        {
            XForm = world * XForm;
            return this;
        }
        public override bool CastFrom(object source)
        {
            if (source == null) return false;
            if (source is Plane PL)
            {
                this.XForm = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, PL);
                return true;
            }
            if (source is GH_Plane ghPL)
            {
                this.XForm = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, ghPL.Value);
                return true;
            }
            if (source is Point3d pt)
            {
                this.XForm = Rhino.Geometry.Transform.Translation(new Vector3d(pt));
                return true;
            }

            if (source is GH_Point ghPt)
            {
                this.XForm = Rhino.Geometry.Transform.Translation(new Vector3d(ghPt.Value));
                return true;
            }

            Point3d point = Point3d.Unset;
            if (GH_Convert.ToPoint3d(source, ref point, GH_Conversion.Both))
            {
                this.XForm = Rhino.Geometry.Transform.Translation(new Vector3d(point));
                return true;
            }
            var plane = Plane.WorldXY;
            if (GH_Convert.ToPlane(source, ref plane, GH_Conversion.Both))
            {
                this.XForm = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, plane);
                return true;
            }
            return false;
        }
        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            foreach (var Pt in this.GetBlockGraphNode())
                args.Pipeline.DrawPoint(Pt, args.Color);

            var PL = Plane.WorldXY; PL.Transform(this.XForm);
            args.Pipeline.DrawPoint(PL.Origin, Rhino.Display.PointStyle.Pin, 3, Color.Blue);

            args.Pipeline.DrawArrow(new Line(PL.Origin, PL.Origin + PL.XAxis * 0.2 * Size), Color.Red);
            args.Pipeline.DrawArrow(new Line(PL.Origin, PL.Origin + PL.YAxis * 0.2 * Size), Color.Green);
            args.Pipeline.DrawArrow(new Line(PL.Origin, PL.Origin + PL.ZAxis * 0.2 * Size), Color.Yellow);

            foreach (var Crv in this.GetBlockGraphEdge())
            {
                args.Pipeline.DrawCurve(Crv, args.Color, 4);
            }
        }
        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            if (this.attribute.BlockColor == Color.Transparent)
            {
                foreach (var b in this.GetBlocks(PairOption.Top))
                {
                    var mesh = Mesh.CreateFromBox(b, 1, 1, 1);
                    var material = new Rhino.Display.DisplayMaterial(System.Drawing.Color.Yellow, 0.8);
                    args.Pipeline.DrawMeshShaded(mesh, material);
                }
                foreach (var b in this.GetBlocks(PairOption.Bottom))
                {
                    var mesh = Mesh.CreateFromBox(b, 1, 1, 1);
                    var material = new Rhino.Display.DisplayMaterial(System.Drawing.Color.Blue, 0.8);
                    args.Pipeline.DrawMeshShaded(mesh, material);
                }
            }
            else
            {
                foreach (var b in this.GetBlocks())
                {
                    var mesh = Mesh.CreateFromBox(b, 1, 1, 1);
                    var material = new Rhino.Display.DisplayMaterial(attribute.BlockColor, 0.8);
                    args.Pipeline.DrawMeshShaded(mesh, material);
                }
            }
        }

        //Override
        public override object ScriptVariable()
        => this;
        public override Plane Value
        {
            get
            {
                var Pt = Plane.WorldXY;
                Pt.Transform(XForm);
                return Pt;
            }
            set
            {
                this.XForm = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, value);
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
                target = (TQ)(object)new GH_Point(Value.Origin);
                return true;
            }
            if (typeof(TQ).IsAssignableFrom(typeof(Plane)))
            {
                Value.Transform(this.XForm);
                target = (TQ)(object)Value;
                return true;
            }
            target = default(TQ);
            return false;
        }
        public override bool Equals(object obj)
        {
            if (obj is SLBlockPair other)
            {
                return this.XForm.Equals(other.XForm) && this.Size.Equals(other.Size) && this.attribute.Identifier == other.attribute.Identifier;
            }
            return false;
        }
        public override int GetHashCode() => base.GetHashCode();
    }
}