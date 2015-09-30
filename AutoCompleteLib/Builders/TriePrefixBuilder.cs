/*============================================================
Class:  TriePrefixBuilder

 * Назначение: Реализация построителя префиксного словаря на основе TrieTopN.
 *              Более детальное описание см. в описании TrieTopN.
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace AutoCompleteLib.Builders
{

    /// <summary>
    /// Реализация построителя префиксного словаря на основе TrieTopN.
    /// </summary>
    public class TriePrefixBuilder : PrefixBuilder
    {
        Trie<WordItem> trie = null; 
        protected override EnumError AddNewDictItem(String pword, int pCnt)
        {
            if (trie == null)
            {
                trie=new Trie<WordItem>(base.topNWords, ('z' - 'a') + 1);
            }

            WordItem wi = new WordItem(pword,pCnt);
            trie.Add(pword, wi);

            return EnumError.NoError;
        }
        
        public override List<string> GetPrefixWords(String prefix)
        {
            List<string> list = new List<string>();
            if (trie == null)
            {
                return list;
            }
            List<WordItem> words = trie.GetWordsForKey(prefix, 10);            

#if OUTINPREFIX            
            //для отладки можно выводить входной префикс
            list.Add("<" + prefix + ">");
#endif
            if (words != null)
            {
                foreach (WordItem wi in words)
                {
                    list.Add(wi.word);
                }
                return list;
            }
            return list;
        }
    }

}
