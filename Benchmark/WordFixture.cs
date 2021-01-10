using Pronunciation;
using WordNet;

namespace Benchmark
{

public class WordFixture
{
    public WordFixture()
    {
        SpellingEngine      = new SpellingEngine();
        WordNetEngine       = new WordNetEngine();
        PronunciationEngine = new PronunciationEngine();
    }

    public PronunciationEngine PronunciationEngine { get; }
    public WordNetEngine WordNetEngine { get; }

    public SpellingEngine SpellingEngine { get; }
}

}
