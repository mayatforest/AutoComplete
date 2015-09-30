/*============================================================
Class:  SimplePrefixBuilder

 * Назначение: Простая реализация построителя префиксного словаря.
 *              Используется Dictionary для хранения узлов и SortedList для хранения TopN слов.
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCompleteLib.Builders
{
 
    /// <summary>
    /// Простая реализация построителя префиксного словаря.
    /// </summary>
    public class SimplePrefixBuilder : PrefixBuilder
    {
        DictAutoCompleteSorted dac = new DictAutoCompleteSorted();

        protected override EnumError AddNewDictItem(String pword, int pCnt)
        {
            char[] chars = pword.ToCharArray();
            String prefix = "";
            foreach (char ch in chars)
            {
                prefix += ch;
                if (dac.ContainsKey(prefix))
                {
                    WordItem wi = new WordItem(pword,pCnt);
                    ListOfWordItemSorted lowi = dac[prefix];
                    lowi.Add(wi, null);

                    //В списке TopN слов всегда храним не более topNWords слов.
                    if (lowi.Count > base.topNWords)
                    {
                        lowi.RemoveAt(base.topNWords);
                    }
                }
                else
                {
                    ListOfWordItemSorted lowi = new ListOfWordItemSorted();
                    lowi.Capacity = base.topNWords+1;
                    WordItem wi = new WordItem(pword,pCnt);

                    lowi.Add(wi, null);
                    dac.Add(prefix, lowi);
                }
            }
            return EnumError.NoError;
        }
        public override List<string> GetPrefixWords(String prefix)
        {
            List<string> wlist = new List<string>();
            if (dac.ContainsKey(prefix))
            {
                ListOfWordItemSorted lowi = dac[prefix];

                int i = 0;
                foreach (WordItem wi in lowi.Keys)
                {
                    wlist.Add(wi.word);
                    i++;
                    if (i > base.topNWords) break;
                }
            }
            
            //если слов для префикса нет, то возвращаем пустой список
            return wlist;
        }
    }
}
