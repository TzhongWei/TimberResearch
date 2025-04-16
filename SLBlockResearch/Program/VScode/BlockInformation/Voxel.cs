using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace Block
{
    public class Voxel
    {
        //Property
        public Transform Xform;
        public double Size;

        //Function
        public Box VoxelDisplay() 
        {
            var B = new Box(
            Plane.WorldXY,
            new Interval(-Size/2, Size/2),
            new Interval(-Size/2, Size/2),
            new Interval(-Size/2, Size/2)
            );
            B.Transform (Xform);
            return B;
        }
        public Voxel(double Size)
        {
            this.Size = Size;
            this.Xform = Rhino.Geometry.Transform.Identity ;
        }
        public Voxel(Voxel V) 
        {
            this.Size = V.Size;
            this.Xform = V.Xform;
        }
        public void Transform(Transform T)
        {   
            this.Xform = T;
        }
        public Voxel DuplicateVoxel() => new Voxel(this);
        public override bool Equals(object obj)
        {
            if(obj is Voxel V)
            {
                return this.Xform.Equals(V.Xform) && this.Size == V.Size;
            }
            else return false;
        }
        public override int GetHashCode() => base.GetHashCode();
    }
}