/*============================================================
class:  WordItem

 * Назначение: Класс хранения элемента словаря.
 *              Содержит в себе определение функции сравнения CompareWordByCnt 
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace AutoCompleteLib.Builders
{
    /// <summary>
    /// Класс хранения элемента словаря.
    /// </summary>
    public class WordItem : IComparable<WordItem>
    {
        /// <summary>
        /// Слово.
        /// </summary>
        public readonly String word;
        
        /// <summary>
        /// Число использований слова.
        /// </summary>
        public readonly int cnt;

        /// <summary>
        /// Число вызванных сравнений при работе со всеми словами.
        /// </summary>
        public static int CompareCallCnt {get;private set;}
        
        public override string ToString()
        {
            return "[" + cnt.ToString() + "]" + word;
        }
        
        public WordItem(String pword, int pcnt)
        {
            word = pword;
            cnt = pcnt;
        }
        /// <summary>
        /// Функция сранвнения двух элементов. Если частота слов совпадает, то слова сравниваются в лексикографическом формате
        /// </summary>
        public static int CompareWordByCnt(WordItem x, WordItem y)
        {
            CompareCallCnt++;

            if (x == null || y == null)
            {
                return 0;
            }
            if (x.cnt== y.cnt) return String.Compare(x.word, y.word, false, CultureInfo.CurrentCulture);
            if (x.cnt < y.cnt) return 1;
            return -1;
        }

        #region IComparable<WordItem> Members

        public int CompareTo(WordItem other)
        {
            return CompareWordByCnt(this, other);
        }

        #endregion
    }

    public class ListOfWordItemSorted : SortedList<WordItem, WordItem>
    {

    }

    public class DictAutoCompleteSorted : Dictionary<String, ListOfWordItemSorted>
    {

    }
}
