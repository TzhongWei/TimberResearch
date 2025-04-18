using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Block
{
    public class Voxel: GH_GeometricGoo<Plane>, IBlockBase
    {
        //Property
        public Transform XForm {get; private set;}
        public double Size {get;}
        public BlockAttribute attribute {get; private set;}

        public BoundingBox ClippingBox => this.Boundingbox;

        public override BoundingBox Boundingbox => this.GetBlocks()[0].BoundingBox;

        public override string TypeName => "Voxel";

        public override string TypeDescription => "A Voxel Box";

        //Function
        public List<Box> GetBlocks()
        {
            var B = new Box(
            Plane.WorldXY,
            new Interval(-Size/2, Size/2),
            new Interval(-Size/2, Size/2),
            new Interval(-Size/2, Size/2)
            );
            B.Transform (XForm);
            return new List<Box>{ B };
        }
        public Brep GetUnionBlock() => GetBlocks()[0].ToBrep();
        public List<Point3d> GetBlockGraphNode()
        {
            var Pt = Point3d.Origin;
            Pt.Transform(XForm);
            return new List<Point3d>{Pt};
        }
        public List<Curve> GetBlockGraphEdge() => null;
        public Voxel(double Size)
        {
            this.Size = Size;
            this.XForm = Rhino.Geometry.Transform.Identity ;
            this.attribute = new BlockAttribute();
        }
        public Voxel(Voxel V) 
        {
            this.Size = V.Size;
            this.XForm = V.XForm;
            this.attribute = new BlockAttribute(this.attribute);
        }
        public override IGH_GeometricGoo Transform(Transform xform)
        {   
            this.XForm *= xform;
            return this;
        }
        public IBlockBase DuplicateBlock() => new Voxel(this);
        public override bool Equals(object obj)
        {
            if(obj is Voxel V)
            {
                return this.XForm.Equals(V.XForm) && this.Size == V.Size;
            }
            else return false;
        }
        public override int GetHashCode() => base.GetHashCode();

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            var Box = this.GetBlocks()[0];
            args.Pipeline.DrawMeshWires(Mesh.CreateFromBox(Box,1,1,1), args.Color);
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            var Box = this.GetBlocks()[0];
            args.Pipeline.DrawMeshShaded(Mesh.CreateFromBox(Box,1,1,1), args.Material);
        }

        public override IGH_GeometricGoo DuplicateGeometry()
            => new Voxel(this);


        public override BoundingBox GetBoundingBox(Transform xform)
        {
           // Get the box representing this voxel, duplicate it, apply the extra transform,
           // and return the resulting bounding box.
           var box = GetBlocks()[0];          // already contains the voxel's internal transform
           var dup = new Box(box);
           dup.Transform(xform);
           return dup.BoundingBox;
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            var nodes = this.GetBlockGraphNode().Select(xmorph.MorphPoint).ToList();
            var newBlock = new Voxel(this.Size);
            return newBlock;
        }

        public override string ToString()
        {
            var Location = Point3d.Origin;
            Location.Transform(this.XForm);
            return $"Voxel \n Location : {Location.ToString()}, Size : {Size}";
        }
    }
}