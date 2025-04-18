using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using System.Text;
using Block;
using System.Net.Mime;


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

        public abstract bool Equals(IToken<IShapeContext> other);
    }
    
}