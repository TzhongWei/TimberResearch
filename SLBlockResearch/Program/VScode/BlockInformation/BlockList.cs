using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Rhino.Geometry;
using Rhino.Collections;
using System;
using System.Drawing;


namespace Block
{
    public class BlockList<T> : IList<T> where T : IBlockBase
    {
        //Property
        public Transform XForm {get; private set;}
        private List<T> _blockList = new List<T>();

        public BlockList() 
        {
            XForm = Rhino.Geometry.Transform.Identity;
        }
        public BlockList(IEnumerable<T> values)
        {
            this.XForm = Rhino.Geometry.Transform.Identity;
            this._blockList = values.ToList();
        }
        public BlockList(BlockList<T> values)
        {
            this.XForm = values.XForm;
            this._blockList = values._blockList;
        }
        public T this[int index]
        {
            get => _blockList[index];
            set => _blockList[index] = value;
        }

        public int Count => _blockList.Count;
        public bool IsReadOnly => false;
        /// <summary>
        /// When adding the block, the system can automatically duplcate a block instance
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item) => _blockList.Add((T) item.DuplicateBlock());
        public void AddRange(IEnumerable<T> items) => _blockList.AddRange(items);
        public void Clear() => _blockList.Clear();
        public bool Contains(T item) => _blockList.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _blockList.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _blockList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _blockList.GetEnumerator();
        public int IndexOf(T item) => _blockList.IndexOf(item);
        public void Insert(int index, T item) => _blockList.Insert(index, item);
        public bool Remove(T item) => _blockList.Remove(item);
        public void RemoveAt(int index) => _blockList.RemoveAt(index);

        public BlockList<T> Transform(Rhino.Geometry.Transform transform)
        {
            this.XForm *= transform;
            return this;
        }
        public List<Box> GetBlocks()
        {
            var Boxes = new List<Box>();
            foreach(var Block in _blockList)
            {
                Boxes.AddRange(Block.GetBlocks());
            }
            Boxes = Boxes.Select(x => {x.Transform(this.XForm); return x;}).ToList();
            return Boxes;
        }
        public List<Brep> GetUnionBlock()
        {
            var BrepList = new List<Brep>();
            foreach(var Block in _blockList)
            {
                BrepList.Add(Block.GetUnionBlock());
            }
            BrepList = BrepList.Select(x => {x.Transform(this.XForm); return x;}).ToList();
            return BrepList;
        }
        public List<Curve> GetConfigureGraphEdge(bool Direction = true)
        {
            var curves = new List<Curve>();
            var PathList = new List<Point3d>();
            foreach (var block in _blockList)
            {
                var PL = Plane.WorldXY;
                PL.Transform(block.XForm);
                PL.Transform(this.XForm);
                PathList.Add(PL.Origin);
                
                if(Direction)
                {
                    var LN = new LineCurve(PL.Origin, PL.Origin + -0.5 * block.Size * PL.ZAxis);
                    curves.Add(LN);
                }
            }
            var PLine = new PolylineCurve(PathList);
            curves.AddRange(PLine.DuplicateSegments());
            return curves;
        }
        public List<Transform> GetTransforms()
        {
            var ListT = new List<Transform>();
            foreach (var block in _blockList)
            {
                ListT.Add(block.XForm * this.XForm);
            }
            return ListT;
        }

        private Dictionary<string, object> _UserSetting = new Dictionary<string, object>();

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
        public BlockList<T> SelectBlock(params int[] SelectedID)
        {
            BlockList<T> SelectedBags = new BlockList<T>();
            foreach(var ID in SelectedID)
            {
                if(ID > this._blockList.Count)
                    throw new IndexOutOfRangeException();
            }
            for(int i = 0; i < this._blockList.Count; i++)
            {
                if(SelectedID.ToList().Contains(i))
                {
                    this._blockList[i].attribute.BlockColor = Color.Red;
                    SelectedBags._blockList.Add(this._blockList[i]);
                }
                else
                {
                    this._blockList[i].attribute.BlockColor = Color.LightGray;
                }
            }
            return SelectedBags;
        }
        public static explicit operator BlockList<T>(List<T> values) => new BlockList<T>(values);
        public static explicit operator BlockList<T>(List<object> values)
        {
            var NewList = new BlockList<T>();
            foreach(var item in values)
            {
                if(item is T)
                {
                    NewList.Add((T)item);
                }
                else
                    throw new Exception($"item is not IBlockBase");
            }
            return NewList;
        }
        public static implicit operator List<T>(BlockList<T> values) 
        {
            var ListT = values._blockList.Select(x => (T)x.ApplyWorldTransform(values.XForm)).ToList();
            return ListT;
        }
    
    }
}