using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Rhino.Geometry;
using Block;

namespace Grammar
{
    /// <summary>
    /// A token that applies a pre‑defined transform to the interpreter context.
    /// </summary>
    /// <typeparam name="TBlock">Concrete block type implementing <see cref="IBlockBase"/>.</typeparam>
    public sealed class TransformationToken: Token
    {
        /// <inheritdoc/>
        public override string Name { get; }

        /// <summary>The transform carried by this token.</summary>
        private readonly Transform _transform;

        public TransformationToken(string name, Transform transform)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _transform = transform;
        }

        /// <inheritdoc/>
        public override bool Equals(IToken<IShapeContext> other)
            => other != null && other.Name == Name;

        /// <inheritdoc/>
        public override bool Action(ref IShapeContext context, params object[] args)
        {
            if (context is null) return false;
            // apply this token's transform
            context.PointerTS = context.PointerTS * _transform;
            return true;
        }
    }
}