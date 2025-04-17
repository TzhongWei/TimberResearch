using Rhino.Geometry;

namespace Block
{
    public interface IBlockBase
    {
        Transform XForm{get; set;}
        double Size {get; set;}
    }
}