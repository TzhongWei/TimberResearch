using Block;
namespace Grammar
{
    public sealed class PushToken : Token
    {
        public override string Name => "[";
        public PushToken(){}
        public override bool Action(ref IShapeContext context, params object[] args)
        {
            context.StackTS.Push(context.PointerTS);
            context.StackNode.Push(context.PointerNode);
            return true;
        }

        public override bool Equals(IToken<IShapeContext> other)
                    => other != null && other.Name == Name;
    }
}