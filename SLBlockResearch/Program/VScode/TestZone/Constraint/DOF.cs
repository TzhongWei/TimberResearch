using System.Collections.Generic;
using System.Linq;
using System;
using Rhino.Geometry;
using System.CodeDom;
using System.Collections;

namespace ConstraintDOF
{
    public class DOF
    {
        public Transform XForm {get; set;} = Rhino.Geometry.Transform.Identity;
        private Point3d Anchor = Point3d.Origin;
        public DOFDirection DOFVector {get; private set;} = DOFDirection.Fix;
        public bool IsFix => DOFVector == DOFDirection.Fix;
        private DOF(Point3d Anchor, DOFDirection DOFVector)
        {
            this.DOFVector = DOFVector;
            this.Anchor = Anchor;
        }
        public DOF Duplicate()
        {
            var NewDOF = new DOF(this.Anchor, this.DOFVector);
            NewDOF.XForm = this.XForm.Clone();
            return NewDOF;
        }
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
        public DOF Transform(Rhino.Geometry.Transform XForm)
        {
            this.XForm *= XForm;
            return this;
        }
        public override string ToString()
         => $"Degree of Freedon on {this.GetVector3d()}";
        public Point3d GetAnchor()
        {
            var TAnchor = new Point3d(Anchor);
            TAnchor.Transform(this.XForm);
            return TAnchor;
        }
        public Vector3d GetVector3d()
        {
            Vector3d vec3d;
            switch (this.DOFVector)
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
            vec3d.Transform(this.XForm);
            return vec3d;
        }
    }
}