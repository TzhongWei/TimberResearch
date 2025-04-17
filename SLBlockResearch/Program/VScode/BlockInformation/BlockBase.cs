using System.Collections.Generic;
using Rhino.Geometry;

namespace Block
{
    public interface IBlockBase
    {
        Transform XForm {get;}
        IBlockBase DuplicateBlock();
        public List<Box> GetBlocks();
        public List<Point> GetBlockGraphNode();
        public List<Curve> GetBlockGraphEdge();
    }
}