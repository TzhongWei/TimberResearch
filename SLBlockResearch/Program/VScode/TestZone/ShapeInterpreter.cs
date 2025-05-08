using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Rhino.Geometry;
using Block;
using Graph;
using System.Security.Cryptography.X509Certificates;
using Graph.Goo;
using ConstraintDOF;

namespace Grammar
{
    public sealed class ShapeInterpreter
    {
        private BlockList<IBlockBase> _blocklist = new BlockList<IBlockBase>();
        /// <summary>
        /// Readonly it will not modify the list
        /// </summary>
        /// <returns></returns>
        public BlockList<IBlockBase> GetBlockList() => new BlockList<IBlockBase>(_blocklist);
        public ShapeContext shapeContext { get; private set; }
        public string cmd;
        public string Sentence;
        public HashSet<Token> tokens;
        private SGraph ArrangeGraph;
        public Constraint.StabilityEquation stabilityEqu = null;
        public double[] StabilityWeight;
        public SGraphGoo GetGraph() => new SGraphGoo(this.ArrangeGraph);
        public ShapeInterpreter(params IBlockBase[] blocks)
        {
            this._blocklist = new BlockList<IBlockBase>();
            this.ArrangeGraph = new SGraph();
            this.shapeContext = new ShapeContext(this, blocks);
            cmd = "";
            tokens = new HashSet<Token>();
        }
        /// <summary>Add a token instance to the interpreter’s token set.</summary>
        public bool AddToken(Token token)
        {
            bool result = false;
            if (token != null)
                result = this.tokens.Add(token);
            return result;
        }
        public void SetUserString(string Key, object Value)
        {
            if (this.shapeContext.UserSetting.ContainsKey(Key))
                this.shapeContext.UserSetting[Key] = Value;
            else
                this.shapeContext.UserSetting.Add(Key, Value);
        }
        /// <summary>Stores the input sentence to be interpreted.</summary>
        public bool SetSentence(string sentence)
        {
            this.Sentence = sentence;
            return true;
        }
        public bool Compute()
        {
            if (this.tokens.Count == 0) return false;
            var StringTokenList = this.Sentence.Split(' ').ToList();
            int Index = 0;
            var cost = 0.0;
            foreach(var T in this.tokens)
            {
                var Content = shapeContext as IShapeContext;
                if(T is IsIDToken isIDToken)
                    isIDToken.SetStabilityCost(ref Content);   //<- set up the cost for each node, and therefore each node has cost of called.
            }
            foreach (var T in StringTokenList)
            {
                if (this.tokens.Select(t => t.Name).Contains(T))
                {
                    var SelectT = this.tokens.First(t => t.Name == T);
                    var Content = shapeContext as IShapeContext;
                    this.cmd += $"Run {T} \n";

                    if (SelectT is IsIDToken IDTokenMem)
                    {
                        SelectT.Action(ref Content, this._blocklist);
                        this.cmd += "Place block \n";
                        IDTokenMem.SetSNode(ref Content, this.ArrangeGraph);
                        cost += IDTokenMem.StabilityCost;               //Adding a new cost into existing structure
                        cost += ConstraintDOF.Constraint.EvaStability(this.ArrangeGraph, 
                        true, stabilityEqu, this.StabilityWeight);      //Evaluate the existing structure stability cost
                    }
                    else
                    {
                        SelectT.Action(ref Content, null);
                    }

                    this.shapeContext.PassToken.Add(Index, (T, SelectT));
                }
                else
                {
                    this.cmd += $"{T} isn't set up in the token list";
                    return false;
                }
                Index++;
            }

            this.cmd += "Compute is finished";
            this.cmd += $"The cost of this assembly strategy is {cost}";
            return true;
        }
    
        public BlockList<IBlockBase> AssemblyDisplay(double t, Transform Xform, double PlaceScale = 4)
        {
            var BList = Constraint.AssemblyDisplay(this.ArrangeGraph, t, PlaceScale);
            BList.Transform(Xform);
            return BList;
        }
    }
}