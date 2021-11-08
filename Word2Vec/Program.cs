using System;
using System.Collections.Generic;
using System.Linq;
using Word2vec.Tools;
using WordNet;

namespace Word2Vec
{
    class Program
    {

        public static void OutputClosestWords(Vocabulary vocabulary, Representation representation, double tolerance = 0.5)
        {
            var nearbyWords = representation.GetClosestFrom(vocabulary.Words.Where(x => x != representation), 20)
                .Where(x => x.DistanceValue > tolerance).ToList();

            if (nearbyWords.Any())
            {
                Console.WriteLine(representation.WordOrNull + ":");
                Console.WriteLine("   " + string.Join(", ", nearbyWords.Select(x=>x.Representation.WordOrNull)));
                Console.WriteLine();
            }
            

        }

        public static void OutputAllWords(Vocabulary vocabulary)
        {
            foreach (var word in vocabulary.Words)
            {
                OutputClosestWords(vocabulary, word);
            }
        }

        public static Vocabulary CreateVocabulary()
        {
            Console.WriteLine("Reading the model...");

            //Set your w2v bin file path here:
            var path1 = @"C:\Users\wainw\Downloads\GoogleNews-vectors-negative300.bin";
            //var path2 = @"C:\Users\wainw\Downloads\wiki-news-300d-1M.vec";
            var vocabulary = new Word2VecBinaryReader().Read(path1);
            //For w2v text sampling file use:
            //var vocabulary = new Word2VecTextReader().Read(path);

            Console.WriteLine("vectors file: " + path1);
            Console.WriteLine("vocabulary size: " + vocabulary.Words.Length);
            Console.WriteLine("w2v vector dimensions count: " + vocabulary.VectorDimensionsCount);

            return vocabulary;
        }

        static void Main(string[] args)
        {

            var vocabulary = CreateVocabulary();

            var dictionary = new WordNet.WordNetEngine();
            var nouns = dictionary.IndexDictionary[PartOfSpeech.Noun];

            foreach (var indexEntry in nouns.GetAll())
            {
                if (indexEntry.Word.All(char.IsLetter))
                {
                    if (vocabulary.ContainsWord(indexEntry.Word))
                    {
                        var rep = vocabulary[indexEntry.Word];
                        OutputClosestWords(vocabulary, rep);
                    }
                }
                
                
            }



            OutputAllWords(vocabulary);

            //      Console.WriteLine();

            //      int count = 7;

            //      #region distance

            //      Console.WriteLine("top "+count+" closest to \""+ boy+"\" words:");
            //      var closest = vocabulary.Distance(boy, count);

            //      // Is simmilar to:
            //      // var closest = vocabulary[boy].GetClosestFrom(vocabulary.Words.Where(w => w != vocabulary[boy]), count);

            //      foreach (var neightboor in closest)
            //          Console.WriteLine(neightboor.Representation.WordOrNull + "\t\t" + neightboor.DistanceValue);
            //      #endregion

            //      Console.WriteLine();

            //      #region analogy
            //      Console.WriteLine("\""+girl+"\" relates to \""+boy+"\" as \""+woman+"\" relates to ..."); 
            //      var analogies = vocabulary.Analogy(girl, boy, woman, count);
            //      foreach (var neightboor in analogies)
            //          Console.WriteLine(neightboor.Representation.WordOrNull + "\t\t" + neightboor.DistanceValue);
            //      #endregion

            //      Console.WriteLine();

            //      #region addition
            //      Console.WriteLine("\""+girl+"\" + \""+boy+"\" = ...");
            //      var additionRepresentation = vocabulary[girl].Add(vocabulary[boy]);
            //      var closestAdditions = vocabulary.Distance(additionRepresentation, count);
            //      foreach (var neightboor in closestAdditions)
            //           Console.WriteLine(neightboor.Representation.WordOrNull + "\t\t" + neightboor.DistanceValue);
            //      #endregion

            //      Console.WriteLine();

            //      #region subtraction
            //Console.WriteLine("\""+girl+"\" - \""+boy+"\" = ...");
            //      var subtractionRepresentation = vocabulary[girl].Substract(vocabulary[boy]);
            //      var closestSubtractions = vocabulary.Distance(subtractionRepresentation, count);
            //      foreach (var neightboor in closestSubtractions)
            //          Console.WriteLine(neightboor.Representation.WordOrNull + "\t\t" + neightboor.DistanceValue);
            //      #endregion

            //      Console.WriteLine("Press any key to continue...");
            //      Console.ReadKey();
        }
    }
}
