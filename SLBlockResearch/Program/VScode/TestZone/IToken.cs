using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using System.Text;
using Block;
using System.Net.Mime;
using Graph;


namespace Grammar
{
    public interface IToken<T> : IEquatable<IToken<T>> where T : class, IShapeContext
    {
        string Name {get;}
        bool Action(ref T context, params object[] args);
    }
    public abstract class Token: IToken<IShapeContext>
    {
        public abstract string Name {get;}

        public abstract bool Action(ref IShapeContext context, params object[] args);

        public virtual bool Equals(IToken<IShapeContext> other) => other != null && other.Name == Name;
    }
    public interface IsTransformToken
    {
        
    }
    public interface IsIDToken
    {
        NodeBase TokenNode {get;}
        bool SetSNode(ref IShapeContext context, SGraph sGraph, params object[] args);
    }
}