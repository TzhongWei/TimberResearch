using System.Collections.Generic;
using System.Linq;
using System;
using Graph;
using System.Collections;
using Rhino.Geometry;
using System.Drawing;
using System.Linq.Expressions;
using System.Windows.Forms.VisualStyles;
using Rhino.Render;
using Block;
using Rhino.Collections;

namespace ConstraintDOF
{
    public enum DOFDirection
    {
        Px,
        Nx,
        Py,
        Ny,
        Pz,
        Nz,
        Fix,
    }
    public class Constraint
    {
        public Transform XForm { get; set; } = Rhino.Geometry.Transform.Identity;
        public bool IsFullyLocked
        {
            get
            {
                if (this.ConstraintMatrix.Count == 0)
                    this.SetConstraintMatrix();
                return this.ConstraintMatrix[0].IsFix;
            }
        } //If a block is locked, it must have only one DOF which is named DOF lock. 
        private NodeBase _node { get; set; }
        /// <summary>
        /// This block or system's connecting faces and their direction
        /// </summary>
        public List<DOF> ConnectFaceAndDir { get; private set; } = new List<DOF>();
        /// <summary>
        /// Get the constraint direction
        /// </summary>
        public List<DOF> ConstraintMatrix { get; private set; } = new List<DOF>();
        public NodeBase Node
        {
            get
            {
                var TempNode = _node.Duplicate();
                TempNode.Transform(XForm);
                return TempNode;
            }
        }
        /// <summary>
        /// Null Object
        /// </summary>
        public Constraint() { }
        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="constraint"></param>
        public Constraint(Constraint constraint)
        {
            XForm = constraint.XForm.Clone();
            this._node = constraint._node.Duplicate();
            ConnectFaceAndDir = constraint.ConnectFaceAndDir.Select(x => x.Duplicate()).ToList();
            ConstraintMatrix = constraint.ConstraintMatrix.Select(x => x.Duplicate()).ToList();
        }
        #region SetConstraint
        /// <summary>
        /// Evaluates the degrees of freedom (DOFs) between two individual blocks in a voxel-based system,
        /// by performing a rigid body collision test. It calculates which directions are constrained
        /// due to physical contact between voxels of the reference and target blocks.
        /// For each contact direction detected on the target, the corresponding reversed direction is
        /// applied to the reference, ensuring both blocks reflect mutual constraint.
        /// 
        /// The resulting DOFs are saved in each block's constraint object, which includes both
        /// directional vectors and anchor positions for possible or blocked movements.
        /// </summary>
        /// <param name="reference">The fixed reference block to test against.</param>
        /// <param name="target">The target block whose DOFs are being evaluated.</param>
        /// <param name="RefConstraint">The resulting constraint object for the reference block.</param>
        /// <param name="TarConstraint">The resulting constraint object for the target block.</param>
        public static void EvaluateDOFSingle(IsSingleNode reference, IsSingleNode target,
        out Constraint RefConstraint, out Constraint TarConstraint)
        {
            RefConstraint = new Constraint();
            TarConstraint = new Constraint();
            RefConstraint._node = reference as NodeBase;
            TarConstraint._node = target as NodeBase;
            ///Rigid body Collision Test
            var TargetTestDir = target.TestDirection();
            var BSize = reference.blockBase.Size;

            var RefNodes = reference.blockBase.GetBlockGraphNode();

            foreach (var TestPerNode in TargetTestDir)
            {
                var ContactDirs = new List<Vector3d>();
                foreach (var dir in TestPerNode.Value)
                {
                    bool hasNeighbor = RefNodes.Any(np => np.DistanceTo(TestPerNode.Key + BSize * dir) < 0.01);
                    if (hasNeighbor)
                        ContactDirs.Add(dir);
                }
                if (ContactDirs.Count > 0)
                {
                    foreach (var dir in ContactDirs)
                    {
                        var Anchor = TestPerNode.Key + BSize / 2 * dir;
                        var DOFSet = DOF.CreateDOF(Anchor, dir);
                        var DOFSetR = DOF.CreateReverse(DOFSet);
                        TarConstraint.ConnectFaceAndDir.Add(DOFSet);
                        RefConstraint.ConnectFaceAndDir.Add(DOFSetR);
                    }
                }
            }
            TarConstraint.SetConstraintMatrix();
            RefConstraint.SetConstraintMatrix();


            ///Internalised in the node
            (reference as NodeBase).constraint = RefConstraint;
            (target as NodeBase).constraint = RefConstraint;
        }
        public static void EvaluateDOF(NodeBase reference, NodeBase target, out Constraint RefConstraint, out Constraint TarConstraint)
        {
            RefConstraint = new Constraint();
            TarConstraint = new Constraint();
            RefConstraint._node = reference;
            TarConstraint._node = target;

            //Rigid body Collision Test
            var TargetTestDir = target.TestDirection();
            var BSize = reference.BlockSize;

            var RefNodes = reference.GetBlocks().SelectMany(x => x.GetBlockGraphNode());

            foreach (var TestDirPerNode in TargetTestDir)
            {
                var PointTesters = TestDirPerNode.Value.Select(x => TestDirPerNode.Key + x * BSize).ToList();

                //Use RTree Find neighbour
                var EnumInt = RTree.Point3dClosestPoints(PointTesters, RefNodes, 0.001);

                foreach (var IntList in EnumInt)
                {
                    if (IntList.Length == 0) continue;
                    else
                    {
                        //Each neighbour is only pointed by one direction
                        var dir = TestDirPerNode.Value.ToList()[IntList.First()];
                        var Anchor = TestDirPerNode.Key + BSize / 2 * dir;
                        var DOFSet = DOF.CreateDOF(Anchor, dir);
                        var DOFSetR = DOF.CreateReverse(DOFSet);
                        TarConstraint.ConnectFaceAndDir.Add(DOFSet);
                        RefConstraint.ConnectFaceAndDir.Add(DOFSetR);
                    }
                }
            }
            TarConstraint.SetConstraintMatrix();
            RefConstraint.SetConstraintMatrix();

            ///Internalised in the node
            (reference as NodeBase).constraint = RefConstraint;
            (target as NodeBase).constraint = RefConstraint;
        }
        /// <summary>
        /// This component stores all possible DOFs for all elements within a hierarchical assembly group (SGNode).
        /// It first evaluates the geometric DOF vectors of the parent group itself, and then recursively collects
        /// the constraints of all leaf child nodes using deep search. 
        /// WARNING: This function is computationally expensive because it evaluates every individual block's constraints
        /// by testing rigid body motion directions against their neighbors.
        /// </summary>
        /// <param name="Node">A NodeBase that implements HasSubNodes and serves as the root of a subassembly.</param>
        /// <param name="childrenNodes">The list of all leaf child nodes within the hierarchy.</param>
        /// <param name="childrenConstraints">The corresponding list of DOF constraints for each leaf node.</param>
        public static void EvaluateSGNodeDOFVecters(HasSubNodes Node, out List<NodeBase> childrenNodes, out List<Constraint> childrenConstraints)
        {
            var Parent = Node as NodeBase;
            Parent.constraint = new Constraint();
            foreach (var NodesDir in Parent.TestDirection())
            {
                Parent.constraint.ConnectFaceAndDir.AddRange(
                    NodesDir.Value.Select(x => DOF.CreateDOF(NodesDir.Key + x * Parent.BlockSize / 2, x))
                );
            }
            Parent.constraint.GetAllElementsConstraint(-1, out childrenNodes, out childrenConstraints);
        }
        /// <summary>
        /// Generate a constraint for the specified child node by evaluating its local interaction with sibling blocks,
        /// then combine with external constraints from the parent node.
        /// This method performs two main tasks:
        /// 1. It constructs a temporary group (SGNode) of all sibling blocks excluding the target child,
        ///    and evaluates the DOF between this reference group and the child node.
        /// 2. It merges any relevant global constraints from the parent node (e.g., system-level contact conditions)
        ///    that spatially influence the child’s geometry, ensuring the child constraint reflects both local
        ///    and external influences.
        /// </summary>
        /// <param name="Index">The index of the target child node within the parent’s Children list.</param>
        /// <param name="SubNode">The output node reference corresponding to the evaluated child.</param>
        /// <returns>The computed constraint for the specified child node.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is out of range of the parent’s child list.</exception>
        public Constraint GetElementConstraint(int Index, out NodeBase SubNode)
        {
            if (this._node is HasSubNodes NodeSub)
            {
                if (Index < 0 || Index >= NodeSub.Children.Count)
                    throw new IndexOutOfRangeException();
                var target = NodeSub.Children[Index];
                var Reference = new List<NodeBase>();
                for (int i = 0; i < NodeSub.Children.Count; i++)
                {
                    if (i != Index)
                        Reference.Add(NodeSub.Children[i]);
                }

                var TempRefSGNode = new SGNode(-1, Reference); //Non extendable node (graph)
                Constraint.EvaluateDOF(TempRefSGNode, target, out _, out var tarConstraint);

                // Merge external constraints from the parent node into the child's constraint set.
                // This ensures that any boundary conditions or external connections applied to the parent
                // (e.g., contact with other tuples or global system constraints) are inherited by the selected child.
                // Without this, the child would only reflect internal constraints (from sibling blocks),
                // and miss the influence of global conditions applied to the parent system.
                // Filter global constraints to only those impacting this child block
                var childNodes = target.GetBlocks()
                                       .SelectMany(b => b.GetBlockGraphNode())
                                       .ToHashSet(); // faster lookups if nodes are unique

                double threshold = target.BlockSize / 2 + 0.1;

                var relevantGlobalDOFs = this.ConnectFaceAndDir
                    .Where(globalDOF => childNodes.Any(node => node.DistanceTo(globalDOF.GetAnchor()) < threshold))
                    .ToList();

                tarConstraint.ConnectFaceAndDir.AddRange(relevantGlobalDOFs);
                tarConstraint.SetConstraintMatrix();

                target.constraint = tarConstraint;
                SubNode = target;
                return tarConstraint;
            }
            else
            {
                SubNode = null;
                return null;
            }
        }
        private void SetConstraintMatrix()
        {
            ///Initialise
            this.ConstraintMatrix = new List<DOF>();

            var BlockCentre = this.Node.GetCentroid();

            var DOFDirBags = new HashSet<DOFDirection>(this.ConnectFaceAndDir.Select(x => x.DOFVector));
            if (this.ConnectFaceAndDir.Count == 0)
            {
                // No connecting face is regarded as free rigid body
                this.ConstraintMatrix = SETFreeDir(0);
                return;
            }

            bool px = DOFDirBags.Contains(DOFDirection.Px);
            bool nx = DOFDirBags.Contains(DOFDirection.Nx);
            bool py = DOFDirBags.Contains(DOFDirection.Py);
            bool ny = DOFDirBags.Contains(DOFDirection.Ny);
            bool pz = DOFDirBags.Contains(DOFDirection.Pz);
            bool nz = DOFDirBags.Contains(DOFDirection.Nz);

            if (!px && !nx) this.ConstraintMatrix.AddRange(SETFreeDir(1)); //No +-X constraint
            if (!py && !ny) this.ConstraintMatrix.AddRange(SETFreeDir(2)); //No +-Y constraint
            if (!pz && !nz) this.ConstraintMatrix.AddRange(SETFreeDir(3)); //No +-Z constraint

            // If +X face is blocked (has contact) but −X is free → motion allowed toward −X
            if (px && !nx) this.ConstraintMatrix.Add(DOF.CreateDOF(BlockCentre, -Vector3d.XAxis));

            // If −X face is blocked but +X is free → motion allowed toward +X
            else if (!px && nx) this.ConstraintMatrix.Add(DOF.CreateDOF(BlockCentre, Vector3d.XAxis));

            // If +Y face is blocked but −Y is free → motion allowed toward −Y
            if (py && !ny) this.ConstraintMatrix.Add(DOF.CreateDOF(BlockCentre, -Vector3d.YAxis));

            // If −Y face is blocked but +Y is free → motion allowed toward +Y
            else if (!py && ny) this.ConstraintMatrix.Add(DOF.CreateDOF(BlockCentre, Vector3d.YAxis));

            // If +Z face is blocked but −Z is free → motion allowed toward −Z
            if (pz && !nz) this.ConstraintMatrix.Add(DOF.CreateDOF(BlockCentre, -Vector3d.ZAxis));

            // If −Z face is blocked but +Z is free → motion allowed toward +Z
            else if (!pz && nz) this.ConstraintMatrix.Add(DOF.CreateDOF(BlockCentre, Vector3d.ZAxis));

            // If all 6 directions are blocked → node is fully locked
            if (px && nx && py && ny && pz && nz)
                this.ConstraintMatrix.Add(DOF.CreateDOF(BlockCentre));  // Fixed lock

            List<DOF> SETFreeDir(int Dir = 0)
            {
                var DOFFreeBag = new List<DOF>();
                switch (Dir)
                {
                    case 0:
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, Vector3d.XAxis));
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, -Vector3d.XAxis));
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, Vector3d.YAxis));
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, -Vector3d.YAxis));
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, Vector3d.ZAxis));
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, -Vector3d.ZAxis));
                        return DOFFreeBag;
                    case 1:
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, Vector3d.XAxis));
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, -Vector3d.XAxis));
                        return DOFFreeBag;
                    case 2:
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, Vector3d.YAxis));
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, -Vector3d.YAxis));
                        return DOFFreeBag;
                    case 3:
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, Vector3d.ZAxis));
                        DOFFreeBag.Add(DOF.CreateDOF(BlockCentre, -Vector3d.ZAxis));
                        return DOFFreeBag;
                    default:
                        throw new ArgumentException("Invalid direction specifier.");
                }
            }
        }
        #endregion
        #region Node_Constraint_Area
        private void DFS_LocalConst(Constraint current, int depth, List<NodeBase> DFSChildrenNodes, List<Constraint> DFSChildrenConstraints)
        {
            if (!(current._node is HasSubNodes parent)) return;

            if (depth == 1)
            {
                for (int i = 0; i < parent.Children.Count; i++)
                {
                    var constraint = current.GetElementConstraint(i, out var subNode);
                    DFSChildrenNodes.Add(subNode);
                    DFSChildrenConstraints.Add(constraint);
                }
            }
            else
            {
                for (int i = 0; i < parent.Children.Count; i++)
                {
                    var child = parent.Children[i];
                    if (child is HasSubNodes)
                    {
                        var subConstraint = new Constraint { XForm = Rhino.Geometry.Transform.Identity, _node = child };
                        DFS_LocalConst(subConstraint, depth - 1, DFSChildrenNodes, DFSChildrenConstraints);
                    }
                }
            }
        }
        private void DFS_All(Constraint current, List<NodeBase> DFSChildrenNodes, List<Constraint> DFSChildrenConstraint)
        {
            // Recursively traverse all subnodes, collecting constraints for leaf nodes only
            if (!(current._node is HasSubNodes parent))
                return;
            for (int i = 0; i < parent.Children.Count; i++)
            {
                // For each child, get its constraint and subNode via GetElementConstraint
                var constraint = current.GetElementConstraint(i, out var subNode);
                if (subNode is HasSubNodes)
                {
                    // If the subNode has children, recurse into it
                    var subConstraint = new Constraint { XForm = Rhino.Geometry.Transform.Identity, _node = subNode };
                    DFS_All(subConstraint, DFSChildrenNodes, DFSChildrenConstraint);
                }
                else
                {
                    // If subNode is a leaf, collect it and its constraint
                    DFSChildrenNodes.Add(subNode);
                    DFSChildrenConstraint.Add(constraint);
                }
            }
        }
        /// <summary>
        /// Recursively collects the constraints of all subnodes within a composite node structure.
        /// If Level is -1, performs a full depth-first traversal to gather constraints from all leaf nodes.
        /// Otherwise, retrieves constraints up to the specified depth level, where 1 means direct children only,
        /// 2 means children and their children, and so on.
        /// </summary>
        /// <param name="Level">
        /// The depth level for traversal. Use -1 for full traversal to all leaf nodes.
        /// </param>
        /// <param name="ChildrenNodes">
        /// The output list of all discovered child nodes (either leaf nodes or at the specified level).
        /// </param>
        /// <param name="ChildrenConstraints">
        /// The corresponding output list of DOF constraints for each node in <paramref name="ChildrenNodes"/>.
        /// </param>
        public void GetAllElementsConstraint(int Level, out List<NodeBase> ChildrenNodes, out List<Constraint> ChildrenConstraints)
        {
            ChildrenNodes = new List<NodeBase>();
            ChildrenConstraints = new List<Constraint>();
            if (Level == -1) // Search until all nodes are single node (leaf nodes)
            {
                // Helper for DFS traversal to collect all leaf constraints
                DFS_All(this, ChildrenNodes, ChildrenConstraints);
            }
            else
            {
                DFS_LocalConst(this, Level, ChildrenNodes, ChildrenConstraints);
            }
        }
        #endregion
        public List<BrepFace> GetConstraintFaces()
        {
            var FinalFaces = new List<BrepFace>();
            for (int i = 0; i < this.ConnectFaceAndDir.Count; i++)
            {
                var CONDIR = this.ConnectFaceAndDir[i];
                if (this.ConstraintMatrix.Any(x => x.DOFVector == CONDIR.DOFVector))
                {
                    FinalFaces.Add(this.GetConnectFace(i));
                }
            }
            return FinalFaces;
        }
        public List<BrepFace> GetConnectFaces()
        {
            var FinalFaces = new List<BrepFace>();
            for (int i = 0; i < this.ConnectFaceAndDir.Count; i++)
            {
                FinalFaces.Add(this.GetConnectFace(i));
            }
            return FinalFaces;
        }
        public BrepFace GetConnectFace(int Index)
        {
            var BSize = this.Node.BlockSize;

            if (Index > this.ConnectFaceAndDir.Count)
                throw new IndexOutOfRangeException();

            var DOFCon = this.ConnectFaceAndDir[Index];
            var RecInt = new Interval(-BSize / 2, BSize / 2);
            var Boundary = new Rectangle3d(Plane.CreateFromNormal(DOFCon.GetAnchor(), DOFCon.GetVector3d()), RecInt, RecInt).ToPolyline().ToPolylineCurve();
            var planarBreps = Brep.CreatePlanarBreps(Boundary, 0.1);
            if (planarBreps == null || planarBreps.Length == 0) throw new Exception("Connect Face miss some data or internal error");
            var Face = planarBreps.First().Faces[0];
            Face.Transform(this.XForm);
            return Face;
        }
        public Constraint Transform(Rhino.Geometry.Transform XForm)
        {
            this.XForm *= XForm;
            foreach (var DOF in this.ConnectFaceAndDir)
            {
                DOF.XForm = this.XForm;
            }
            foreach (var ConstDOF in this.ConstraintMatrix)
            {
                ConstDOF.XForm = this.XForm;
            }
            return this;
        }
    }
}