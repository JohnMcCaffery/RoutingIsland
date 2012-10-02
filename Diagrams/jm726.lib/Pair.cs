#region Namespace imports

using System;

#endregion

namespace common {
    /// <summary>
    ///   Simple wrapper class which binds two Objects together as a pair
    /// </summary>
    /// <typeparam name = "T1">The type of the first Object</typeparam>
    /// <typeparam name = "T2">The type of the second Object</typeparam>
    [Serializable]
    public struct Pair<T1, T2> {
        private readonly T1 a;
        private readonly T2 b;

        public Pair(T1 a, T2 b) {
            this.a = a;
            this.b = b;
        }

        /// <summary>
        ///   The first item in the pair
        /// </summary>
        public T1 A {
            get { return a; }
        }

        /// <summary>
        ///   The second item in the pair
        /// </summary>
        public T2 B {
            get { return b; }
        }

        /// <inheridoc />
        public override bool Equals(object obj) {
            if (!(obj is Pair<T1, T2>)) return false;
            Pair<T1, T2> pair = (Pair<T1, T2>) obj;
            return a.Equals(pair.A) && b.Equals(pair.B);
        }

        /// <inheridoc />
        public int GetHashCode() {
            return a.GetHashCode() + b.GetHashCode();
        }

        public string ToString() {
            return ("(" + a.ToString() + ", " + b.ToString());
        }
    }
}