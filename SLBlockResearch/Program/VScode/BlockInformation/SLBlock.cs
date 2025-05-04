using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;
using System.Drawing;
using Grasshopper.Kernel.Types;
using GH_IO;
using GH_IO.Serialization;
using System.Diagnostics.Tracing;
using Rhino.Render.Fields;

namespace Block
{
    ///<summary>
    /// Represents an SLBlock, a custom geometric object composed of transformed voxel blocks, supporting Grasshopper previews.
    ///</summary>
    public class SLBlock : GH_GeometricGoo<Plane>, IBlockBase
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
        
        public BlockAttribute attribute { get; private set; }
        private readonly List<Transform> _transforms;
        private static List<Transform> SetUpTS(double Size)
        {
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
            return VoxelLoc.Select(x => Rhino.Geometry.Transform.Translation(x)).ToList();
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

        public SLBlock(double Size = 1)
        {
            this.Size = Size;
            this.XForm = Rhino.Geometry.Transform.Identity;
            this.attribute = new BlockAttribute();
            this._transforms = SetUpTS(Size);
            this.SetIdenifier(this.TypeName);
        }
        public SLBlock(SLBlock block)
        {
            this.Size = block.Size;
            this.XForm = block.XForm.Clone();
            this.attribute = new BlockAttribute(block.attribute);
            this._transforms = SetUpTS(Size);
            this.SetIdenifier(block.attribute.Identifier);
        }
        public IBlockBase DuplicateBlock()
        {
            var NewSL = new SLBlock(this);
            return NewSL;
        }
        public void SetIdenifier(string ID)
        {
            this.attribute.Identifier = ID;
        }
        public void SetColor(Color colour)
        {
            this.attribute.BlockColor = colour;
        }
        #region GetGeometry
        public List<Box> GetBlocks()
        {
            var Voxel = new Voxel(Size);
            var Boxes = this._transforms.Select(t =>
            {
                var V = (Voxel)Voxel.DuplicateBlock();
                V.Transform(XForm);
                V.Transform(t);
                return V.GetBlocks()[0];
            }
            );

            return Boxes.ToList();
        }
        public Brep GetUnionBlock()
        {
            var B = Brep.CreateBooleanUnion(this.GetBlocks().Select(x => x.ToBrep()), 0.1)[0];
            B.MergeCoplanarFaces(1);
            return B;
        }
        public List<Point3d> GetBlockGraphNode()
        {

            var Nodes = this._transforms.Select(t =>
            {
                var Pt = Point3d.Origin;
                Pt.Transform(t);
                Pt.Transform(XForm);
                return Pt;
            });
            return Nodes.ToList();
        }
        public List<Curve> GetBlockGraphEdge()
        {
            var Nodes = this.GetBlockGraphNode();
            var PolyCrv = new PolylineCurve(Nodes);
            return PolyCrv.DuplicateSegments().ToList();
        }
        #endregion GetGeometry

        public override string ToString()
        {
            var Location = Point3d.Origin;
            Location.Transform(this.XForm);
            return $"SLBlock \n Location : {Location.ToString()}, Size : {Size}";
        }
        public override string TypeName => "SLBlock";
        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return (SLBlock)this.DuplicateBlock();
        }
        public override BoundingBox Boundingbox
        {
            get
            {
                var boxes = this.GetBlocks();
                BoundingBox box = BoundingBox.Empty;
                foreach (var b in boxes)
                    box.Union(b.BoundingBox);
                return box;
            }
        }

        public BoundingBox ClippingBox => this.Boundingbox;
        public override string TypeDescription => this.ToString();
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
        public override IGH_GeometricGoo Transform(Transform xform)
        {
            this.XForm *= xform;
            return this;
        }
        public IGH_GeometricGoo ApplyWorldTransform(Transform world)
        {
            XForm = world * XForm;
            return this;
        }
        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            var nodes = this.GetBlockGraphNode().Select(xmorph.MorphPoint).ToList();
            var newBlock = new SLBlock(this.Size);
            return newBlock;
        }
        public override bool CastFrom(object source)
        {
            if (source == null) return false;
            if(source is Plane PL)
            {
                this.XForm = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, PL);
                return true;
            }
            if(source is GH_Plane ghPL)
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
            if(GH_Convert.ToPlane(source, ref plane, GH_Conversion.Both))
            {
                this.XForm = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, plane);
                return true;
            }
            return false;
        }
        #region Preview
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
            foreach (var b in this.GetBlocks())
            {
                var mesh = Mesh.CreateFromBox(b, 2, 2, 2);
                if (attribute.BlockColor == Color.Transparent)
                {
                    var Display = new Rhino.Display.DisplayMaterial(Color.Green, 0.8);
                    args.Pipeline.DrawMeshShaded(mesh, Display);
                }
                else
                {
                    var Display = new Rhino.Display.DisplayMaterial(attribute.BlockColor, 0.8);
                    args.Pipeline.DrawMeshShaded(mesh, Display);
                }

            }
        }
        #endregion Preview
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
            if(typeof(TQ).IsAssignableFrom(typeof(Plane)))
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
            if (obj is SLBlock other)
            {
                return this.XForm.Equals(other.XForm) && this.Size.Equals(other.Size) && this.attribute.Identifier == other.attribute.Identifier;
            }
            return false;
        }
        public override int GetHashCode() => base.GetHashCode();
    }
}
