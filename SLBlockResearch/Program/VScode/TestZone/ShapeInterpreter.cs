using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Rhino.Geometry;
using Block;
using System.Security.Cryptography.X509Certificates;

namespace Grammar
{
    public sealed class ShapeInterpreter
    {
        public BlockList<IBlockBase> blocklist {get; private set;}
        public ShapeContext shapeContext {get; private set;}
        public string cmd;
        public string Sentence;
        public HashSet<Token> tokens;
        public ShapeInterpreter(params IBlockBase[] blocks)
        {
            blocklist = new BlockList<IBlockBase>();
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
            if(this.shapeContext.UserSetting.ContainsKey(Key))
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
            if(this.tokens.Count == 0) return false;
            var StringTokenList = this.Sentence.Split(' ').ToList();
            int Index = 0;
            foreach(var T in StringTokenList)
            {
                if(this.tokens.Select(t => t.Name).ToList().Contains(T))
                {
                    var SelectT = this.tokens.Where(t => t.Name == T).First();
                    var Content = shapeContext as IShapeContext;
                    SelectT.Action(ref Content, null);
                    this.cmd = $"Run {T} \n";
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
            return true;
        }
    }
}