//using System.Collections.Generic;
//using System.Linq;
//using BenchmarkDotNet.Attributes;
//using BenchmarkDotNet.Jobs;
//using FileDatabase;
//using Puns;

//namespace Benchmark
//{

//[SimpleJob(RuntimeMoniker.NetCoreApp50)]
//[RPlotExporter]
//public class FileDatabase
//{
//    [Params( "Fish",  "Person")]
//    public string Theme;

//    [Params(PunCategory.Artists)]
//    public PunCategory Category;

//    //[Params(256, 512, 1024, 2048, 4096)]
//    [Params( 1024, 2048)]
//    public int DefaultFileStreamBufferSize;
//    //[Params(256, 512, 1024, 2048, 4096)]
//    [Params(1024, 2048)]
//    public int DefaultBufferSize;

//    [Params(64, 128, 256)]
//    public int MinBufferSize;

//    [GlobalSetup]
//    public void Setup()
//    {
//        MyStreamReader.DefaultFileStreamBufferSize = DefaultFileStreamBufferSize;
//        MyStreamReader.DefaultBufferSize           = DefaultBufferSize;
//        MyStreamReader.MinBufferSize               = MinBufferSize;
//    }

//    [Benchmark]
//    public List<Pun> TestPunHelper()
//    {


//        var fixture = new WordFixture();

//        var synSets = fixture.WordNetEngine.GetSynSets(Theme).ToList();

//        var puns = Puns.PunHelper.GetPuns(
//                Category,
//                Theme,
//                synSets,
//                fixture.WordNetEngine,
//                fixture.PronunciationEngine,
//                fixture.SpellingEngine
//            )
//            .ToList();

//        return puns;
//    }
//}

//}