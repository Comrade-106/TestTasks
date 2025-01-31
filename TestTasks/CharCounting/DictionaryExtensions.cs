
using System.Collections.Generic;

namespace TestTasks.VowelCounting
{
    public static class DictionaryExtensions
    {
        public static (char symbol, int count)[] ToTupleArray(this Dictionary<char, int> charCountDictionary)
        {
            var result = new (char symbol, int count)[charCountDictionary.Count];

            int i = 0;
            foreach (var kvp in charCountDictionary)
            {
                result[i++] = (kvp.Key, kvp.Value);
            }

            return result;
        }
    }
}
