using System.Collections.Generic;
using Rhino.Geometry;
using Block;

namespace Grammar
{
    public interface IShapeContext
    {
        HashSet<IBlockBase> blocks {get;}
        Transform PointerTS {get;set;}
        Stack<Transform> StackTS {get;}
        Dictionary<string, object> UserSetting {get;set;}
        ShapeInterpreter shapeInterpreter {get;set;}
    }
    public class ShapeContext : IShapeContext
    {
        public HashSet<IBlockBase> blocks {get;}
        public Transform PointerTS {get; set;}
        public Stack<Transform> StackTS {get; private set;}
        public Dictionary<string, object> UserSetting {get;set;}
        public ShapeInterpreter shapeInterpreter {get;set;}
        public Dictionary<int, (string, Token)> PassToken;
        private ShapeContext()
        {
            UserSetting = new Dictionary<string, object>();
            PointerTS = Transform.Identity;
            StackTS = new Stack<Transform>();
            PassToken = new Dictionary<int,(string, Token)>();
        }
        public ShapeContext(ShapeInterpreter interpreter, params IBlockBase[] blocks):this()
        {
            this.shapeInterpreter = interpreter;
            this.blocks = new HashSet<IBlockBase>();
            foreach (var block in blocks)
                 this.blocks.Add(block);
        }
    }
}