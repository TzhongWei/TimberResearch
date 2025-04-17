using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Rhino.Geometry;


namespace Block
{
    public class BlockList<T> : IList<T> where T : IBlockBase
    {
        private List<T> _blockList = new List<T>();

        public BlockList() { }

        public T this[int index]
        {
            get => _blockList[index];
            set => _blockList[index] = value;
        }

        public int Count => _blockList.Count;
        public bool IsReadOnly => false;

        public void Add(T item) => _blockList.Add(item);
        public void Clear() => _blockList.Clear();
        public bool Contains(T item) => _blockList.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _blockList.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _blockList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _blockList.GetEnumerator();
        public int IndexOf(T item) => _blockList.IndexOf(item);
        public void Insert(int index, T item) => _blockList.Insert(index, item);
        public bool Remove(T item) => _blockList.Remove(item);
        public void RemoveAt(int index) => _blockList.RemoveAt(index);


    }
}