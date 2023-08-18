using ContestConsoleApp;
using Newtonsoft.Json;
using System.Collections;
using System.Text.RegularExpressions;

namespace ContestTestsRunner
{
    public class TestRunner
    {
        [Fact]
        public void TestSetsExist()
        {
            var testData = new TestData();
            Assert.NotEmpty(testData);
        }

        [Theory]
        [ClassData(typeof(TestData))]
        public void Execute_ReturnsRightResult(string setFileName, string expectedFileName)
        {
            Assert.True(File.Exists(setFileName), $"Test set file not found: {setFileName}");
            Assert.True(File.Exists(expectedFileName), $"Expected results file not found: {expectedFileName}");

            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(memoryStream))
            using (var actualReader = new StreamReader(memoryStream))
            {
                using (var setReader = File.OpenText(setFileName))
                {
                    var executor = new Executor(setReader, writer);
                    executor.Execute();
                }

                writer.Flush();
                memoryStream.Position = 0;

                using (var expectedReader = File.OpenText(expectedFileName))
                {
                    string? expectedLine = null;
                    string? actualLine = null;
                    int lineNumber = 1;

                    while (!expectedReader.EndOfStream
                        && !actualReader.EndOfStream
                        && (expectedLine = expectedReader.ReadLine()) == (actualLine = actualReader.ReadLine()))
                        lineNumber++;

                    Assert.Equal($"Line {lineNumber}: {expectedLine}", $"Line {lineNumber}: {actualLine}");
                    Assert.True(expectedReader.EndOfStream, "Expected results file has more data than executor received");
                    Assert.True(actualReader.EndOfStream, "Executor received more data than expected results file contains");
                }
            }
        }
    }

    public class TestData : IEnumerable<object[]>
    {
        readonly Config _config;

        public TestData()
        {
            _config = ReadConfig();
        }

        Config ReadConfig()
        {
            using (var reader = new StreamReader("config.json"))
            {
                return JsonConvert.DeserializeObject<Config>(reader.ReadToEnd());
            }
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            var regex = new Regex(@"\\\d*$");
            return Directory.EnumerateFiles(_config.testSetsFolder)
                .Where(fileName => regex.IsMatch(fileName))
                .Select(fileName => new object[] { fileName, fileName + ".a" })
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    record class Config(string testSetsFolder);
}
