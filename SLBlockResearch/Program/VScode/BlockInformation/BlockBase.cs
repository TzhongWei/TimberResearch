using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace Block
{
    public interface IBlockBase:  IGH_GeometricGoo, IGH_PreviewData
    {
        BlockAttribute attribute { get;}
        Transform XForm {get;}
        double Size {get;}
        IBlockBase DuplicateBlock();
        List<Box> GetBlocks();
        List<Point3d> GetBlockGraphNode();
        List<Curve> GetBlockGraphEdge();
        Brep GetUnionBlock();
    }
}