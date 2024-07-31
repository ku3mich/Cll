using Xunit.Abstractions;

namespace Cll.Tests;

public class UnitTest1(ITestOutputHelper log)
{
    [Fact]
    public void Test1()
    {
        var parser = new CllParser<Options>();
        log.WriteLine(parser.GenerateHelp("my.exe", "super program"));
    }
}