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
    public class SLBlock : GH_GeometricGoo<Point3d>, IList<Voxel>, IGH_PreviewData
    {
        public Transform XForm {get; private set;}
        public double Size {get;}
        private List<Transform> _transforms;
        public int Count => 8;
        public bool IsReadOnly => false;
        

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
                _transforms[index] = value.Xform;
            }
        }
        public SLBlock(double Size = 1)
        {
            this.Size = Size;
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
            _transforms.AddRange(VoxelLoc.Select(x => Rhino.Geometry.Transform.Translation(x)));
        }
        #region IList
        public void Add(Voxel item)
        {
            _transforms.Add(item.Xform);
        }

        public void Clear()
        {
            _transforms.Clear();
        }

        public bool Contains(Voxel item)
        {
            return _transforms.Contains(item.Xform);
        }

        public void CopyTo(Voxel[] array, int arrayIndex)
        {
            for (int i = 0; i < _transforms.Count; i++)
            {
                var v = new Voxel(this.Size);
                v.Transform(_transforms[i]);
                array[arrayIndex + i] = v;
            }
        }

        public IEnumerator<Voxel> GetEnumerator()
        {
            foreach (var t in _transforms)
            {
                var v = new Voxel(this.Size);
                v.Transform(t);
                yield return v;
            }
        }

        public int IndexOf(Voxel item)
        {
            return _transforms.IndexOf(item.Xform);
        }

        public void Insert(int index, Voxel item)
        {
            _transforms.Insert(index, item.Xform);
        }

        public bool Remove(Voxel item)
        {
            return _transforms.Remove(item.Xform);
        }

        public void RemoveAt(int index)
        {
            _transforms.RemoveAt(index);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public SLBlock DuplicateSLBlock()
        {
            var NewSL = new SLBlock(this.Size);
            NewSL.XForm = this.XForm;
            return NewSL;
        }
        #endregion IList

        #region GetGeometry
        public List<Box> GetSLBlock()
        {
            var Voxel = new Voxel(Size);
            var Boxes = this._transforms.Select(t => {
                var V = Voxel.DuplicateVoxel(); 
            V.Transform(t); V.Transform(XForm); 
            return V.VoxelDisplay();});

            return Boxes.ToList();
        }
        public List<Point3d> GetSLBlockGraphNode()
        {
            
            var Nodes = this._transforms.Select(t => {
                var Pt = Point3d.Origin;
                Pt.Transform(t);
                Pt.Transform(XForm);
                return Pt;
            });

            return Nodes.ToList();
        }
        public List<Curve> GetSLBlockGraphEdge()
        {
            var Nodes = this.GetSLBlockGraphNode();
            var PolyCrv = new PolylineCurve(Nodes);
            return PolyCrv.DuplicateSegments().ToList();
        }
        #endregion GetGeometry
        public override string ToString()
        {
            var Location = Point3d.Origin;
            Location.Transform(this.XForm);
            return $"SLBlock: Location-{Location.ToString()}, Size - {Size}";
        }
        public override string TypeName => "SLBlock";
        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return this.DuplicateSLBlock();
        }
        public override BoundingBox Boundingbox
        {
            get
            {
                var boxes = this.GetSLBlock();
                BoundingBox box = BoundingBox.Empty;
                foreach (var b in boxes)
                    box.Union(b.BoundingBox);
                return box;
            }
        }

        public BoundingBox ClippingBox => throw new NotImplementedException();

        public override string TypeDescription => throw new NotImplementedException();

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            var boxes = this.GetSLBlock();
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
            var newBlock = this.DuplicateSLBlock();
            newBlock.XForm *= xform;
            return newBlock;
        }
        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            var nodes = this.GetSLBlockGraphNode().Select(xmorph.MorphPoint).ToList();
            var newBlock = new SLBlock(this.Size);
            newBlock._transforms = nodes.Select(p => Rhino.Geometry.Transform.Translation(new Vector3d(p))).ToList();
            return newBlock;
        }
        public override bool CastFrom(object source)
        {
            if (source == null) return false;

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

            return false;
        }
        #region Preview
        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            args.Pipeline.DrawPoint(Value, args.Color);
            foreach(var edge in this.GetSLBlockGraphEdge())
                args.Pipeline.DrawCurve(edge, args.Color);
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
        }
        #endregion Preview
    }
}