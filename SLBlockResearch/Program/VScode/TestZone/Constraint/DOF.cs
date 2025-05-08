using System.Collections.Generic;
using System.Linq;
using System;
using Rhino.Geometry;
using System.CodeDom;
using System.Collections;
using Rhino.Input.Custom;

namespace ConstraintDOF
{
    public static class DOFUtil
    {
        public static Point3d GetDOFTranformAnchor(DOF DOFSetting, Transform transform)
        {
            var TSAnchor = new Point3d(DOFSetting.Anchor);
            TSAnchor.Transform(transform);
            return TSAnchor;
        }
        public static Vector3d GetDOFTransformVector(DOF DOFSetting, Transform transform)
        {
            Vector3d vec3d;
            switch (DOFSetting.DOFVector)
            {
                case DOFDirection.Px:
                    vec3d = Vector3d.XAxis;
                    break;
                case DOFDirection.Nx:
                    vec3d = -Vector3d.XAxis;
                    break;
                case DOFDirection.Py:
                    vec3d = Vector3d.YAxis;
                    break;
                case DOFDirection.Ny:
                    vec3d = -Vector3d.YAxis;
                    break;
                case DOFDirection.Pz:
                    vec3d = Vector3d.ZAxis;
                    break;
                case DOFDirection.Nz:
                    vec3d = -Vector3d.ZAxis;
                    break;
                case DOFDirection.Fix:
                    vec3d = new Vector3d();
                    break;
                default:
                    throw new NotImplementedException();
            }
            vec3d.Transform(transform);
            return vec3d;
        }
        public static bool IsTestDirfriction(DOF refDOF, DOF TestDOF)
        {
            if(refDOF.IsSameDirection(TestDOF) || refDOF.IsSameDirection(DOF.CreateReverse(TestDOF)) || refDOF.IsFix || TestDOF.IsFix)
                return false;
            return true;
        }
    }
    public readonly struct DOF
    {
        public Point3d Anchor { get; }
        public DOFDirection DOFVector { get; }
        public bool IsFix => DOFVector == DOFDirection.Fix;
        public static DOF Unset => new DOF(Point3d.Unset, DOFDirection.Fix);
        private DOF(Point3d Anchor, DOFDirection DOFVector)
        {
            this.DOFVector = DOFVector;
            this.Anchor = Anchor;
        }
        public DOF Duplicate()
        {
            var NewDOF = new DOF(this.Anchor, this.DOFVector);
            return NewDOF;
        }
        public static DOF CreateDOF(Point3d Anchor, DOFDirection dOFDirection)
            =>  new DOF(Anchor, dOFDirection);
        
        public static DOF CreateDOF(Point3d Anchor, Vector3d Direction = new Vector3d())
        {
            if (Direction == Vector3d.XAxis)
                return new DOF(Anchor, DOFDirection.Px);
            else if (Direction == -Vector3d.XAxis)
                return new DOF(Anchor, DOFDirection.Nx);
            else if (Direction == Vector3d.YAxis)
                return new DOF(Anchor, DOFDirection.Py);
            else if (Direction == -Vector3d.YAxis)
                return new DOF(Anchor, DOFDirection.Ny);
            else if (Direction == Vector3d.ZAxis)
                return new DOF(Anchor, DOFDirection.Pz);
            else if (Direction == -Vector3d.ZAxis)
                return new DOF(Anchor, DOFDirection.Nz);
            else if (Direction == new Vector3d())
                return new DOF(Anchor, DOFDirection.Fix);
            else
                throw new ArgumentException("Unsupported direction for DOF creation.");
        }
        public static DOF CreateReverse(DOF Value)
        {
            switch (Value.DOFVector)
            {
                case DOFDirection.Fix:
                    return new DOF(Value.Anchor, Value.DOFVector);
                case DOFDirection.Px:
                    return new DOF(Value.Anchor, DOFDirection.Nx);
                case DOFDirection.Nx:
                    return new DOF(Value.Anchor, DOFDirection.Px);
                case DOFDirection.Py:
                    return new DOF(Value.Anchor, DOFDirection.Ny);
                case DOFDirection.Ny:
                    return new DOF(Value.Anchor, DOFDirection.Py);
                case DOFDirection.Pz:
                    return new DOF(Value.Anchor, DOFDirection.Nz);
                case DOFDirection.Nz:
                    return new DOF(Value.Anchor, DOFDirection.Pz);
                default:
                    throw new ArgumentException("Invalid DOFDirection value");
            }
        }
        public override string ToString()
         => $"Degree of Freedon on {this.DOFVector.ToString()}";
        public override bool Equals(object obj) =>
        obj is DOF other && this.Anchor == other.Anchor && this.DOFVector == other.DOFVector;
        public bool IsSameDirection(DOF other) => this.DOFVector == other.DOFVector;
        public override int GetHashCode() =>
            this.Anchor.GetHashCode() + this.DOFVector.GetHashCode();
    }
}