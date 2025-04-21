using System;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Block;

namespace Grammar
{
        public sealed class IDToken : Token
        {
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
                    context.shapeInterpreter.blocklist.Add(GetBlock);
                    return true;
                }
                else
                    return false;
            }
        }
}