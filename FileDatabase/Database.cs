using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileDatabase
{

public sealed class Database<T, TKey> : IDisposable
    where TKey :  IComparable<TKey>
    where T : class
{
    public Database(
        byte[] data,
        Encoding encoding,
        Func<string, TKey> getKeyFromStringFunc,
        Func<string, T> deserializeFunc)
    {
        _getKeyFromStringFunc = getKeyFromStringFunc;
        _deserializeFunc      = deserializeFunc;
        _myStreamReader       = new MyStreamReader(new MemoryStream(data), encoding);
        _dictionary           = new ConcurrentDictionary<TKey, T?>();
    }

    private readonly Func<string, TKey> _getKeyFromStringFunc;
    private readonly Func<string, T> _deserializeFunc;
    private readonly MyStreamReader _myStreamReader;

    private readonly object _myLock = new();

    private readonly ConcurrentDictionary<TKey, T?> _dictionary;

    private readonly SortedList<TKey, (long start, long end)> _list = new();

    public IEnumerable<T> GetAll()
    {
        lock (_myLock)
        {
            _myStreamReader.DiscardBufferedData();
            _myStreamReader.BaseStream.Seek(0, SeekOrigin.Begin);

            while (true)
            {
                var line = _myStreamReader.ReadLine();

                if (line is null)
                    break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var t = _deserializeFunc(line);

                yield return t;
            }
        }
    }

    public T? this[TKey key] => _dictionary.GetOrAdd(key, SearchDb);

    private T? SearchDb(TKey key)
    {
        lock (_myLock)
        {
            var lowerIndex = _list.Keys.BinarySearch(key);

            long lowerSeek;
            long upperSeek;

            if (lowerIndex < 0)
            {
                lowerSeek = 0;

                upperSeek = _list.Any()
                    ? _list.Values.First().start
                    : _myStreamReader.BaseStream.Length - 1;
            }
            else
            {
                if (_list.Keys[lowerIndex].CompareTo(key) == 0) //This is already present
                {
                    var seek = _list.Values[lowerIndex].start;
                    _myStreamReader.DiscardBufferedData();
                    _myStreamReader.BaseStream.Seek(seek, SeekOrigin.Begin);

                    var line      = _myStreamReader.ReadLine()!;
                    var newEntity = _deserializeFunc(line);
                    return newEntity;
                }

                lowerSeek = _list.Values[lowerIndex].end;
                var nextIndex = lowerIndex + 1;

                if (nextIndex >= _list.Count)
                    upperSeek =
                        _myStreamReader.BaseStream.Length
                      - 1; //This is beyond the last element in the db
                else
                    upperSeek = _list.Values[nextIndex].start;
            }

            var r = EntityBetween(lowerSeek, upperSeek);

            return r;

            T? EntityBetween(long lowerSeek1, long upperSeek1)
            {
                while (true)
                {
                    if (lowerSeek1 >= upperSeek1) //This element is not in the dictionary
                        return null;

                    var (line, newStartSeek, newEndSeek) = LineBetween(
                        _myStreamReader,
                        lowerSeek1,
                        upperSeek1
                    );

                    var newKey = _getKeyFromStringFunc(line);

                    _list.TryAdd(newKey, (newStartSeek, newEndSeek));

                    var comparison = key.CompareTo(newKey);

                    if (comparison == 0)
                    {
                        var newEntity = _deserializeFunc(line);
                        return newEntity;
                    }

                    if (comparison < 0)
                        //our key is before this element
                        upperSeek1 = newStartSeek;
                    else
                        //our key is after this element
                        lowerSeek1 = newEndSeek;
                }
            }

            static (string line, long startSeek, long endSeek) LineBetween(
                MyStreamReader streamReader,
                long lowerSeek,
                long upperSeek)
            {
                var meanSeek = (lowerSeek + upperSeek) / 2;

                streamReader.DiscardBufferedData();
                streamReader.BaseStream.Seek(meanSeek, SeekOrigin.Begin);

                var _                 = streamReader.ReadLine()!;
                var nextLineStartSeek = streamReader.GetActualPosition();

                if (nextLineStartSeek >= upperSeek)
                {
                    //we've gone past - just get the first line between these two
                    streamReader.DiscardBufferedData();
                    streamReader.BaseStream.Seek(lowerSeek, SeekOrigin.Begin);
                    nextLineStartSeek = lowerSeek;
                }

                var line            = streamReader.ReadLine()!;
                var nextLineEndSeek = streamReader.GetActualPosition();

                return (line, nextLineStartSeek, nextLineEndSeek);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose() => _myStreamReader.Dispose();
}

}
