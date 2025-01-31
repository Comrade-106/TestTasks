using System.Collections.Generic;
using System.Linq;

namespace TestTasks.VowelCounting
{
    public class StringProcessor
    {
        public (char symbol, int count)[] GetCharCount(string veryLongString, char[] countedChars) // Mean execution time is 20 us
        {
            var charCountDictionary = new Dictionary<char, int>(countedChars.Length);
            foreach (char ch in countedChars)
            {
                charCountDictionary[ch] = 0;
            }

            foreach (char c in veryLongString)
            {
                if (charCountDictionary.ContainsKey(c))
                {
                    charCountDictionary[c]++;
                }
            }

            return charCountDictionary.ToTupleArray();
        }

        public (char symbol, int count)[] GetCharCountWithLinq(string veryLongString, char[] countedChars) // Mean execution time is 30 us
        {
            return countedChars
                .Select(ch => (ch, veryLongString.Count(c => c == ch)))
                .ToArray();
        }
    }
}
