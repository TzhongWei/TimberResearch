using System.Collections.Generic;
using Rhino.Geometry;
using Block;
using Graph;
using ConstraintDOF;

namespace Grammar
{
    public interface IShapeContext
    {
        /// <summary>
        /// Get a id for a new node. After retieving the ID, the ID will plus one automatically.
        /// </summary>
        /// <returns></returns>
        int GetNodeID();
        HashSet<IBlockBase> blocks {get;}
        Transform PointerTS {get;set;}
        Stack<Transform> StackTS {get;}
        Dictionary<string, object> UserSetting {get;set;}
        ShapeInterpreter shapeInterpreter {get;set;}
        NodeBase PointerNode {get;set;}
        Stack<NodeBase> StackNode {get;}
    }
    public class ShapeContext : IShapeContext
    {
        private int NodeID = 0;
        public int GetNodeID()
        {
            var Give = NodeID;
            NodeID++;
            return Give;
        }
        public HashSet<IBlockBase> blocks {get;}
        public Transform PointerTS {get; set;}
        public Stack<Transform> StackTS {get; private set;}
        public Dictionary<string, object> UserSetting {get;set;}
        public ShapeInterpreter shapeInterpreter {get;set;}
        public Dictionary<int, (string, Token)> PassToken;
        public NodeBase PointerNode {get;set;}
        public Stack<NodeBase> StackNode {get; private set;} = new Stack<NodeBase>();
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