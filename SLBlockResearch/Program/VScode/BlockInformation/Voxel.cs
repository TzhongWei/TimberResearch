using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace Block
{
    public class Voxel:IBlockBase
    {
        //Property
        public Transform XForm {get; private set;}
        public double Size {get;}
        
        //Function
        public Box VoxelDisplay() 
        {
            var B = new Box(
            Plane.WorldXY,
            new Interval(-Size/2, Size/2),
            new Interval(-Size/2, Size/2),
            new Interval(-Size/2, Size/2)
            );
            B.Transform (XForm);
            return B;
        }
        public Voxel(double Size)
        {
            this.Size = Size;
            this.XForm = Rhino.Geometry.Transform.Identity ;
        }
        public Voxel(Voxel V) 
        {
            this.Size = V.Size;
            this.XForm = V.XForm;
        }
        public void Transform(Transform T)
        {   
            this.XForm *= T;
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
    }
}