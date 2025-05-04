using System;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Block;
using Graph;

namespace Grammar
{
        public sealed class IDToken : Token, IsIDToken
        {
            public NodeBase TokenNode {get; private set;}
            public override string Name {get;}
            public string Identifier {get;}
            public IDToken(string name, string identifier)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                this.Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            }
            public override bool Equals(IToken<IShapeContext> other)
            => other != null && other.Name == Name;
            public override bool Action(ref IShapeContext context, params object[] args)
            {
                if(context.blocks.Select(x => x.attribute.Identifier).ToList().Contains(this.Identifier))
                {
                    var GetBlock = context.blocks.Where(b => b.attribute.Identifier == this.Identifier).First().DuplicateBlock();
                    GetBlock.Transform(context.PointerTS);
                    (args.First() as BlockList<IBlockBase>).Add(GetBlock);
                    this.TokenNode = new SNode(GetBlock, context.GetNodeID());

                    return true;
                }
                else
                    return false;
            }
            public bool SetSNode(ref IShapeContext context, SGraph sGraph, params object[] args)
            {
                if(this.TokenNode.ID == 0)
                    sGraph.AddNode(this.TokenNode);
                else
                    sGraph.AddNode(this.TokenNode, context.PointerNode.ID);
                context.PointerNode = this.TokenNode;
                return true;
            }
        }
}