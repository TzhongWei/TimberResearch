using Block;
namespace Grammar
{
    public sealed class PopToken : Token
    {
        public override string Name => "]";

        public override bool Action(ref IShapeContext context, params object[] args)
        {
            context.PointerTS = context.StackTS.Pop();
            context.PointerNode = context.StackNode.Pop();
            return true;
        }
        public PopToken(){}
        public override bool Equals(IToken<IShapeContext> other)
                    => other != null && other.Name == Name;
    }
}