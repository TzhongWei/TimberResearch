using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Drawing;

namespace Block
{
    public class BlockAttribute
    {
        public string Identifier = "";
        public Color BlockColor;
        private Dictionary<string, object> _UserSetting {get;set;}
        public BlockAttribute()
        {
            BlockColor = Color.Transparent;
            _UserSetting = new Dictionary<string,object>();
            this.Identifier = "NotSet";
        }
        public BlockAttribute(BlockAttribute blockAttribute)
        {
            if(blockAttribute == null) 
            {
                BlockColor = Color.Transparent;
            _UserSetting = new Dictionary<string,object>();
            this.Identifier = "NotSet";
            }
            else
            {
            BlockColor =  BlockColor.IsEmpty ? Color.Transparent : blockAttribute.BlockColor;
            this.Identifier = blockAttribute.Identifier == "NotSet"? "NotSet" : blockAttribute.Identifier;
            
            this._UserSetting = new Dictionary<string, object>();
            try
            {
                this._UserSetting = new Dictionary<string, object>(blockAttribute._UserSetting);
            }
            catch{}
            }
        }
        public bool SetUserSetting(string key, object value)
        {
            if (_UserSetting.ContainsKey(key))
            {
                _UserSetting[key] = value;
                return true;
            }
            else
            {
                _UserSetting.Add(key, value);
                return true;
            }
        }

        public bool GetUserSetting(string key, out object value)
        {
            return _UserSetting.TryGetValue(key, out value);
        }
    }
}