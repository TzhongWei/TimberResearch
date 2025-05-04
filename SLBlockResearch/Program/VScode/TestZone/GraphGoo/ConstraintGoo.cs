using System.Collections.Generic;
using System.Linq;
using System;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ConstraintDOF;
using System.Drawing;
using Block;

namespace ConstraintDOF.Goo
{
    public class ConstraintGoo : GH_GeometricGoo<Constraint>, IGH_PreviewData
    {
        private Constraint _constraint => Value;
        public double Size = 10;
        public bool ShowAllConnectingFace = true;
        public override BoundingBox Boundingbox
        {
            get
            {
                var Boxes = this._constraint.Node.GetBlocks();
                var BBox = new BoundingBox();
                foreach (var B in Boxes)
                {
                    BBox.Union(B.Boundingbox);
                }
                return BBox;
            }
        }

        public override string TypeName => "Constraint Model";

        public override string TypeDescription => "Represents a geometric constraint with directional degrees of freedom.";

        public BoundingBox ClippingBox => this.Boundingbox;

        public ConstraintGoo(Constraint constraint)
        {
            this.Value = constraint;
        }
        public ConstraintGoo(ConstraintGoo constraintGoo)
        {
            this.Value = new Constraint(constraintGoo._constraint);
            this.ShowAllConnectingFace = constraintGoo.ShowAllConnectingFace;
            this.Size = constraintGoo.Size;
        }
        public override IGH_GeometricGoo DuplicateGeometry()
        => new ConstraintGoo(this);

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            var BBox = this.Boundingbox;
            BBox.Transform(xform);
            return BBox;
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            this.Value.Transform(xform);
            return this;
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
            => DuplicateGeometry();


        public override string ToString()
        {
            return $"Connect with {this._constraint.ConnectFaceAndDir.Count} Faces, and has {this._constraint.ConstraintMatrix.Count} DOF(s)";
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (ShowAllConnectingFace)
            {
                var ConDOFs = this._constraint.ConnectFaceAndDir;
                foreach (var ConDOF in ConDOFs)
                {
                    var Anchor = ConDOF.GetAnchor();
                    var Dir = ConDOF.GetVector3d();
                    if (Dir != new Vector3d())
                    {
                        var Arrow = new Line(Anchor, Anchor + Dir * this._constraint.Node.BlockSize);
                        switch (ConDOF.DOFVector)
                        {
                            case DOFDirection.Px:
                                args.Pipeline.DrawArrow(Arrow, Color.Yellow, Size, 1);
                                break;
                            case DOFDirection.Nx:
                                args.Pipeline.DrawArrow(Arrow, Color.Yellow, Size, 1);
                                break;
                            case DOFDirection.Py:
                                args.Pipeline.DrawArrow(Arrow, Color.Green, Size, 1);
                                break;
                            case DOFDirection.Ny:
                                args.Pipeline.DrawArrow(Arrow, Color.Green, Size, 1);
                                break;
                            case DOFDirection.Pz:
                                args.Pipeline.DrawArrow(Arrow, Color.Blue, Size, 1);
                                break;
                            case DOFDirection.Nz:
                                args.Pipeline.DrawArrow(Arrow, Color.Blue, Size, 1);
                                break;
                        }
                    }
                }
            }
            else
            {
                var ConstDOFs = this._constraint.ConstraintMatrix;
                if (ConstDOFs.Count == 0) return;
                if (this._constraint.IsFullyLocked)
                {
                    args.Pipeline.DrawPoint(ConstDOFs[0].GetAnchor(), Rhino.Display.PointStyle.X, (float)Size / 10, Color.DarkGray);
                }
                foreach (var ConstDof in ConstDOFs)
                {
                    var Anchor = ConstDof.GetAnchor();
                    var Dir = ConstDof.GetVector3d();
                    var Arrow = new Line(Anchor, Anchor + Dir * this._constraint.Node.BlockSize * 1.5);
                    switch (ConstDof.DOFVector)
                    {
                        case DOFDirection.Px:
                            args.Pipeline.DrawArrow(Arrow, Color.Yellow, Size, 1);
                            break;
                        case DOFDirection.Nx:
                            args.Pipeline.DrawArrow(Arrow, Color.Yellow, Size, 1);
                            break;
                        case DOFDirection.Py:
                            args.Pipeline.DrawArrow(Arrow, Color.Green, Size, 1);
                            break;
                        case DOFDirection.Ny:
                            args.Pipeline.DrawArrow(Arrow, Color.Green, Size, 1);
                            break;
                        case DOFDirection.Pz:
                            args.Pipeline.DrawArrow(Arrow, Color.Blue, Size, 1);
                            break;
                        case DOFDirection.Nz:
                            args.Pipeline.DrawArrow(Arrow, Color.Blue, Size, 1);
                            break;
                    }
                }
            }
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            if (ShowAllConnectingFace)
            {
                var DOFfaces = this._constraint.GetConnectFaces();
                var BSize = this._constraint.Node.GetBlocks().First().Size;

                var RecInt = new Interval(-BSize / 2, BSize / 2);
                var Meshesfaces = this._constraint.ConnectFaceAndDir.Select
                (
                    x => Mesh.CreateFromPlane(
                    Plane.CreateFromNormal(
                        x.GetAnchor(),
                        x.GetVector3d()),
                        RecInt, RecInt, 2, 2)
                        );
                foreach (var mesh in Meshesfaces)
                {
                    args.Pipeline.DrawMeshShaded(mesh, args.Material);
                    args.Pipeline.DrawMeshWires(mesh, Color.Black);
                }
                var Blocks = this._constraint.Node.GetBlocks();
                var Voxs = new List<Mesh>();
                foreach (var B in Blocks)
                    Voxs.AddRange(B.GetBlocks().Select(x => Mesh.CreateFromBox(x, 1, 1, 1)));

                foreach (var V in Voxs)
                {
                    var Display = new Rhino.Display.DisplayMaterial(Color.LightGray, 0.8);
                    args.Pipeline.DrawMeshShaded(V, Display);
                }
            }
            else
            {
                var Display = new Rhino.Display.DisplayMaterial(this.Value.IsFullyLocked ? 
                Color.LightGray : Color.LightGreen, 0.8);
                var Blocks = this._constraint.Node.GetBlocks();
                var Voxs = new List<Mesh>();
                foreach (var B in Blocks)
                    Voxs.AddRange(B.GetBlocks().Select(x => Mesh.CreateFromBox(x, 1, 1, 1)));

                foreach (var V in Voxs)
                {
                    args.Pipeline.DrawMeshShaded(V, Display);
                }
            }
        }
    }
}