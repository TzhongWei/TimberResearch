using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Block;
using ConstraintDOF;
using Rhino.Geometry;

namespace Graph
{
    public class SNode : NodeBase, IsSingleNode
    {
        private IBlockBase _blockBase { get; }

        public IBlockBase blockBase
        {
            get
            {
                var Dup = (IBlockBase) this._blockBase.DuplicateBlock();
                if (Dup == null) return null;
                Dup.ApplyWorldTransform(XForm);
                return Dup;
            }
        }
        public override bool IsValid => _blockBase != null && this.ID >= 0;
        private SNode() : base()
        { }
        /// <summary>
        /// Initial Node
        /// </summary>
        /// <param name="blockbase"></param>
        public SNode(IBlockBase blockbase) : this()
        {
            this.ID = 0;
            this._blockBase = blockbase;
        }
        public SNode(IBlockBase blockbase, int id) : this(blockbase)
        {
            this.ID = id;
        }
        public override NodeBase Duplicate()
        {
            var newThis = new SNode(this._blockBase.DuplicateBlock());
            newThis.ID = this.ID;
            newThis.XForm = this.XForm.Clone();
            return newThis;
        }
        public override Point3d GetCentroid()
        {
            var XForm = this.blockBase.XForm;
            var Pt = Point3d.Origin;
            Pt.Transform(XForm);
            return Pt;
        }
        public override string ToString()
         => $"SNode {ID} : Parent ID {this.Parent.ID}";

        public override Dictionary<Point3d, List<Vector3d>> TestDirection()
        {
            var voxelNodes = this.blockBase.GetBlockGraphNode();
            var voxelEdges = this.blockBase.GetBlockGraphEdge();

            var testVec = new Dictionary<Point3d, List<Vector3d>>();
            var directions = new List<Vector3d>
            {
                new Vector3d(1, 0, 0),
                new Vector3d(-1, 0, 0),
                new Vector3d(0, 1, 0),
                new Vector3d(0, -1, 0),
                new Vector3d(0, 0, 1),
                new Vector3d(0, 0, -1)
            };

            foreach (var node in voxelNodes)
            {
                var connectedEdges = voxelEdges
                    .Where(e => e.PointAtStart == node || e.PointAtEnd == node)
                    .ToList();
                connectedEdges = connectedEdges.Select(e =>
                {
                    if (e.PointAtStart == node) return e;
                    else return new LineCurve(e.PointAtEnd, e.PointAtStart);
                }).ToList();

                var invalidVecs = new HashSet<Vector3d>(
                    connectedEdges.Select(e => e.TangentAt(0.5))
                                  .Select(t => directions.FirstOrDefault(d => d * t == 1))
                                  .Where(v => v != default)
                );

                var validVecs = directions.Where(v => !invalidVecs.Contains(v)).ToList();
                testVec[node] = validVecs;
            }
            return testVec;
        }
        public override List<IBlockBase> GetBlocks() => new List<IBlockBase> { this.blockBase };
    }
}