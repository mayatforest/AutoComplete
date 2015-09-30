/*============================================================
Class:  Trie<TValue> 

 * Назначение: Класс реализует префиксное дерево для заданного алфавита.
               Основан на реализации https://github.com/kpol/trie.
            Модификация:
 *                      1) Добавлено хранение TopN слов на основе данных функции сравнения, для каждого узла дерева.
 *                      Реализовано с помощью SortedList.
 *                      2) Т.к. по ТЗ входной алфавит - 26 англ букв, то дети хранятся в фиксированном массиве 
 *                      (вместо Dictionary), размер массива задается заранее, память выделяется по факту 
 *                      добавления новых элементов.
 *                      3) Т.к. по ТЗ нет модификации или удаления строк методы удаления элементов закомментированы 
 *                      и выдают исключения при использовании.
 * ChangeList:
 *              v0.2 +Добавлены новые методы GetAllWordsForKey
 *              v0.1 Первоначальная версия.                              
 * 
 ==========================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace AutoCompleteLib.Builders
{

    //namespace AlgoLib
    // trie based on sources from https://github.com/kpol/trie by Kirill Polishchuk
    //Модификация

    /// <summary>
    /// Implementation of trie data structure with top N words cached.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the trie.</typeparam>
    public class Trie<TValue> : IDictionary<string, TValue>
    {
        private readonly TrieNode<TValue> root;

        private int count;

        /// <summary>
        /// Initializes a new instance of the <see cref="Trie{TValue}"/>.
        /// </summary>
        /// <param name="comparer">Comparer.</param>
        public Trie(IEqualityComparer<char> comparer, int maxwordcount,int maxchildren)
        {
            root = new TrieNode<TValue>(char.MinValue, comparer,maxwordcount,maxchildren);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Trie{TValue}"/>.
        /// </summary>
        public Trie(int maxwordcount,int maxchildren)
            : this(EqualityComparer<char>.Default, maxwordcount, maxchildren)
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Trie{TValue}"/>.
        /// </summary>
        public Trie()
            : this(EqualityComparer<char>.Default,1,26)
        {
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        public ICollection<string> Keys
        {
            get
            {
                return GetAllNodes().Select(n => n.Key).ToArray();
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.List`1"/> Top N Words by key
        /// </summary>
        /// <returns>
        /// Gets an <see cref="T:System.Collections.Generic.List`1"/> Top N Words.
        /// </returns>
        /// <param name="key">The key of the element .</param>
        public TValue[] GetAllWordsForKey(String key)
        {
            TrieNode<TValue> node;
            TryGetNode(key, out node);
            if (node != null)
            {
                if (node.words == null)
                {
                    return new TValue[0];
                }
                TValue[] listwords = new TValue[node.words.Keys.Count];
                node.words.Keys.CopyTo(listwords, 0);
                return listwords;
            }
            return null;
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.List`1"/> Top N Words by key
        /// </summary>
        /// <returns>
        /// Gets an <see cref="T:System.Collections.Generic.List`1"/> Top N Words.
        /// </returns>
        /// <param name="key">The key of the element .</param>
        /// <param name="maxcount">Max count for Top N words.</param>
        public List<TValue> GetWordsForKey(String key, int maxcount)
        {
            TrieNode<TValue> node;
            TryGetNode(key, out node);
            if (node != null)
            {
                List<TValue> listwords = new List<TValue>();
                int i = 0;
                if (node.words == null) return listwords;
                foreach (TValue v in node.words.Keys)
                {

                    listwords.Add(v);
                    i++;
                    if (i > maxcount) break;
                }

                return listwords;
            }
            return null;
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>        
        public ICollection<TValue> Values
        {
            get
            {
                return GetAllNodes().Select(n => n.Value).ToArray();
            }
        }

        bool ICollection<KeyValuePair<string, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <returns>
        /// The element with the specified key.
        /// </returns>
        /// <param name="key">The key of the element to get or set.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="key"/> is not found.</exception>
        public TValue this[string key]
        {
            get
            {
                TValue value;

                if (!TryGetValue(key, out value))
                {
                    throw new KeyNotFoundException("The given charKey was not present in the trie.");
                }

                return value;
            }

            set
            {
                TrieNode<TValue> node;

                if (TryGetNode(key, out node))
                {
                    SetTerminalNode(node, value);
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        /// <summary>
        /// Adds an element with the provided charKey and value to the <see cref="Trie{TValue}"/>.
        /// </summary>
        /// <param name="key">The object to use as the charKey of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element with the same charKey already exists in the <see cref="Trie{TValue}"/>.</exception>
        public void Add(string key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            var node = root;

            foreach (var c in key)
            {
                node = node.Add(c, value);
            }

            if (node.IsTerminal)
            {
                throw new ArgumentException(string.Format("An element with the same charKey already exists: '{0}'", key), "key");
            }

            SetTerminalNode(node, value);

            count++;
        }

        /// <summary>
        /// Adds an item to the <see cref="Trie{TValue}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="Trie{TValue}"/>.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same charKey already exists in the <see cref="Trie{TValue}"/>.</exception>
        public void Add(TrieEntry<TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the <see cref="Trie{TValue}"/>.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the <see cref="Trie{TValue}"/>. The items should have unique keys.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same charKey already exists in the <see cref="Trie{TValue}"/>.</exception>
        public void AddRange(IEnumerable<TrieEntry<TValue>> collection)
        {
            foreach (var item in collection)
            {
                Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public void Clear()
        {
            root.Clear();
            count = 0;
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified charKey.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the charKey; otherwise, false.
        /// </returns>
        /// <param name="key">The charKey to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool ContainsKey(string key)
        {
            TValue value;

            return TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets items by key prefix.
        /// </summary>
        /// <param name="prefix">Key prefix.</param>
        /// <returns>Collection of <see cref="TrieEntry{TValue}"/> items which have key with specified key.</returns>
        public IEnumerable<TrieEntry<TValue>> GetByPrefix(string prefix)
        {
            var node = root;

            foreach (var item in prefix)
            {
                if (!node.TryGetNode(item, out node))
                {
                    return Enumerable.Empty<TrieEntry<TValue>>();
                }
            }

            return node.GetByPrefix();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            return GetAllNodes().Select(n => new KeyValuePair<string, TValue>(n.Key, n.Value)).GetEnumerator();
        }

        /// <summary>
        /// Removes the element with the specified charKey from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        /// <param name="key">The charKey of the element to remove.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public bool Remove(string key)
        {
            throw new NotSupportedException("Remove not implemented!");
            /*
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            TrieNode<TValue> node;

            if (!TryGetNode(key, out node))
            {
                return false;
            }

            if (!node.IsTerminal)
            {
                return false;
            }

            RemoveNode(node);

            return true;
             */ 
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool TryGetValue(string key, out TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            TrieNode<TValue> node;
            value = default(TValue);

            if (!TryGetNode(key, out node))
            {
                return false;
            }

            if (!node.IsTerminal)
            {
                return false;
            }

            value = node.Value;

            return true;
        }

        void ICollection<KeyValuePair<string, TValue>>.Add(KeyValuePair<string, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item)
        {
            TrieNode<TValue> node;

            if (!TryGetNode(item.Key, out node))
            {
                return false;
            }

            return node.IsTerminal && EqualityComparer<TValue>.Default.Equals(node.Value, item.Value);
        }

        void ICollection<KeyValuePair<string, TValue>>.CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            Array.Copy(GetAllNodes().Select(n => new KeyValuePair<string, TValue>(n.Key, n.Value)).ToArray(), 0, array, arrayIndex, Count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item)
        {
            TrieNode<TValue> node;

            if (!TryGetNode(item.Key, out node))
            {
                return false;
            }

            if (!node.IsTerminal)
            {
                return false;
            }

            if (!EqualityComparer<TValue>.Default.Equals(node.Value, item.Value))
            {
                return false;
            }

            RemoveNode(node);

            return true;
        }

        private static void SetTerminalNode(TrieNode<TValue> node, TValue value)
        {
            node.IsTerminal = true;
            node.Value = value;
        }
        
        private IEnumerable<TrieNode<TValue>> GetAllNodes()
        {
            return root.GetAllNodes();
        }
        
        private void RemoveNode(TrieNode<TValue> node)
        {
            throw new NotImplementedException("RemoveNode not implemented!");
            /*
            node.Remove();
            count--;
             */ 
        }

        private bool TryGetNode(string key, out TrieNode<TValue> node)
        {
            node = root;

            foreach (var c in key)
            {
                if (!node.TryGetNode(c, out node))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// <see cref="Trie{TValue}"/>'s node.
        /// </summary>
        private sealed class TrieNode<Tvalue>
        {
            //private readonly Dictionary<char, TrieNode<Tvalue>> children;
            private readonly TrieNode<Tvalue>[] children = null;

            private readonly IEqualityComparer<char> comparer;
            internal SortedList<Tvalue, object> words=null;
            private readonly char keyChar;
            private readonly int _maxwordcount;
            private readonly int _maxchildren;

            internal TrieNode(char keyChar, IEqualityComparer<char> comparer, int maxwordcount,int maxchildren)
            {
                this.keyChar = keyChar;
                this.comparer = comparer;
                //children = new Dictionary<char, TrieNode<Tvalue>>(comparer);
                _maxwordcount = maxwordcount;
                _maxchildren = maxchildren;
                children = new TrieNode<Tvalue>[maxchildren];
                
            }

            internal bool IsTerminal { get; set; }

            internal string Key
            {
                get
                {
                    var stack = new Stack<char>();
                    stack.Push(keyChar);

                    TrieNode<Tvalue> node = this;

                    while ((node = node.Parent).Parent != null)
                    {
                        stack.Push(node.keyChar);
                    }

                    return new string(stack.ToArray());
                }
            }

            internal TValue Value { get; set; }

            private TrieNode<Tvalue> Parent { get; set; }
            
            
            private int GetKeyCode(char key)
            {
                return (int)(key-'a');
            }

            //метод модифицирован для хранения TopN слов.
            internal TrieNode<Tvalue> Add(char key, Tvalue value)
            {
                TrieNode<Tvalue> childNode;

                //if (!children.TryGetValue(key, out childNode))
                int k=GetKeyCode(key);
                if (k<0 || k >= children.Length)
                {
                    throw new ArgumentException((String.Format("Invalid key for add! children array too small for key {0}! ", key)));
                }
                if (children[k] == null)
                {
                    childNode = new TrieNode<Tvalue>(key, comparer, _maxwordcount,_maxchildren)
                    {
                        Parent = this
                    };

                    //children.Add(key, childNode);
                    children[k] = childNode;
                }
                else
                {
                    childNode = children[k];
                }
                if (childNode.words == null)
                {
                    childNode.words = new SortedList<Tvalue, object>();
                    childNode.words.Capacity = _maxwordcount + 1;
                }

                childNode.words.Add(value, null);
                if (childNode.words.Count > _maxwordcount)
                {
                    childNode.words.RemoveAt(_maxwordcount);
                }

                return childNode;
            }

            internal void Clear()
            {
                for (int i = 0; i < children.Length; i++)
                {
                    children[i]=null;
                }
            }            
            
            internal IEnumerable<TrieNode<Tvalue>> GetAllNodes()
            {
                foreach (var child in children)
                {
                    if (child.IsTerminal)
                    {
                        yield return child;
                    }

                    foreach (var item in child.GetAllNodes())
                    {
                        if (item.IsTerminal)
                        {
                            yield return item;
                        }
                    }
                }
            }
            
            internal IEnumerable<TrieEntry<TValue>> GetByPrefix()
            {
                if (IsTerminal)
                {
                    yield return new TrieEntry<TValue>(Key, Value);
                }

                foreach (var item in children)
                {
                    
                    foreach (var element in item.GetByPrefix())
                    {
                        yield return element;
                    }
                }
            }

            internal void Remove()
            {
                throw new Exception("Not implemented!");
                /*
                IsTerminal = false;

                if (children.Count == 0 && Parent != null)
                {
                    Parent.children.Remove(keyChar);

                    if (!Parent.IsTerminal)
                    {
                        Parent.Remove();
                    }
                }
                 */ 
            }

            internal bool TryGetNode(char key, out TrieNode<Tvalue> node)
            {
                //return children.TryGetValue(key, out node);
                int k=GetKeyCode(key);
                node=null;
                if (k < 0 || k >= children.Length)
                {
                    throw new ArgumentException(String.Format("Invalid key for add! children array too small for key {0}! ",key));
                }

                if (children[k] == null)
                {
                    return false;
                }
                else
                {
                    node= children[k];
                    return true;
                }
            }
        }
    }


    //namespace AlgoLib

    /// <summary>
    /// Defines a key/value pair that can be set or retrieved from <see cref="Trie{TValue}"/>.
    /// </summary>
    public struct TrieEntry<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrieEntry{TValue}"/> structure with the specified key and value.
        /// </summary>
        /// <param name="key">The <see cref="string"/> object defined in each key/value pair.</param>
        /// <param name="value">The definition associated with key.</param>
        public TrieEntry(string key, TValue value)
            : this()
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Gets the key in the key/value pair.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets the value in the key/value pair.
        /// </summary>
        public TValue Value { get; private set; }

        /// <summary>
        /// Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> containing a fully qualified type name.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("[{0}, {1}]", Key, Value);
        }
    }

}
