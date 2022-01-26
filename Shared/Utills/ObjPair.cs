using System;
using System.Collections.Generic;

namespace Shared.Utills
{
    public class ObjPair<TKey, TValue>
    {
        public TKey Key { get; private set; }

        public TValue Value { get; private set; }

        public ObjPair(TKey key, TValue val)
        {
            Key = key;
            Value = val;
        }
    }

    public class ObjPairComparer<TKey> : IComparer<TKey>
        where TKey : IComparable<TKey>
    {
		public int Compare(TKey x, TKey y)
            => x.CompareTo(y);
	}
}
