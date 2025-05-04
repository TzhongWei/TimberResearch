using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace Block
{
    public interface IBlockBase:  IGH_GeometricGoo, IGH_PreviewData
    {
        /// <summary>
        /// The attribute setting about the block
        /// </summary>
        BlockAttribute attribute { get;}
        /// <summary>
        /// Real location
        /// </summary>
        Transform XForm {get;}
        /// <summary>
        /// The size of the block
        /// </summary>
        double Size {get;}
        IBlockBase DuplicateBlock();
        List<Box> GetBlocks();
        List<Point3d> GetBlockGraphNode();
        List<Curve> GetBlockGraphEdge();
        Brep GetUnionBlock();
        IGH_GeometricGoo ApplyWorldTransform(Transform world);
    }
}