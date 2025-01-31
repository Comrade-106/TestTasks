using BenchmarkDotNet.Attributes;
using System.IO;
using TestTasks.VowelCounting;

namespace TestTasks.BenchmarkTests
{
    public class BenchmarkStringProcessor
    {
        private StringProcessor _stringProcessor;
        private string _testString;
        private char[] _countedChars;

        [GlobalSetup]
        public void Setup()
        {
            _stringProcessor = new StringProcessor();

            _testString = File.ReadAllText("./CharCounting/StringExample.txt");

            _countedChars = new char[] { 'l', 'r', 'm' };
        }

        [Benchmark]
        public (char symbol, int count)[] RunGetCharCountLinq()
        {
            return _stringProcessor.GetCharCountWithLinq(_testString, _countedChars);
        }

        [Benchmark]
        public (char symbol, int count)[] RunGetCharCountDictionary()
        {
            return _stringProcessor.GetCharCount(_testString, _countedChars);
        }
    }
}
