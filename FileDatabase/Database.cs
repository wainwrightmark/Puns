using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FileDatabase
{
    public sealed class Database<T, TKey> : IDisposable
        where TKey : struct, IComparable<TKey>
        where T : class
    {
        public Database(byte[] data,
            Encoding encoding,
            Func<T, TKey> getKeyFunc,
            Func<string, T> deserializeFunc) //TODO custom comparer
        {
            _getKeyFunc = getKeyFunc;
            _deserializeFunc = deserializeFunc;
            _streamReader = new StreamReader(new MemoryStream(data), encoding);
            _dictionary = new ConcurrentDictionary<TKey, T?>();
        }

        private readonly Func<T, TKey> _getKeyFunc;
        private readonly Func<string, T> _deserializeFunc;
        private readonly StreamReader _streamReader;


        private readonly object _myLock = new object();

        private readonly ConcurrentDictionary<TKey, T?> _dictionary;
        private readonly SortedList<TKey, (long start, long end)> _list = new SortedList<TKey, (long start, long end)>();

        //TODO enumerate function

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
                    upperSeek = _list.Any() ? _list.Values.First().start : _streamReader.BaseStream.Length  -1;
                }
                else
                {
                    if (_list.Keys[lowerIndex].CompareTo(key) == 0)//This is already present
                    {
                        var seek = _list.Values[lowerIndex].start;
                        _streamReader.DiscardBufferedData();
                        _streamReader.BaseStream.Seek(seek, SeekOrigin.Begin);

                        var line = _streamReader.ReadLine()!;
                        var newEntity = _deserializeFunc(line);
                        return newEntity;
                    }


                    lowerSeek = _list.Values[lowerIndex].end;
                    var nextIndex = lowerIndex + 1;

                    if (nextIndex >= _list.Count)
                        upperSeek = _streamReader.BaseStream.Length - 1; //This is beyond the last element in the db
                    else
                        upperSeek = _list.Values[nextIndex].start;
                }

                var r = GetEntityBetween(lowerSeek, upperSeek);

                return r;

                T? GetEntityBetween(long lowerSeek1, long upperSeek1)
                {
                    while (true)
                    {
                        if (lowerSeek1 >= upperSeek1) //This element is not in the dictionary
                            return null;

                        var (line, newStartSeek, newEndSeek) = GetLineBetween(_streamReader, lowerSeek1, upperSeek1);

                        var newEntity = _deserializeFunc(line);
                        var newKey = _getKeyFunc(newEntity);

                        _list.TryAdd(newKey, (newStartSeek, newEndSeek) ) ;

                        var comparison = key.CompareTo(newKey);

                        if (comparison == 0)
                            return newEntity;
                        if (comparison < 0)
                            //our key is before this element
                            upperSeek1 = newStartSeek;
                        else
                        //our key is after this element
                            lowerSeek1 = newEndSeek;
                    }
                }


                static (string line, long startSeek, long endSeek) GetLineBetween(
                    StreamReader streamReader,
                    long lowerSeek,
                    long upperSeek)
                {
                    var meanSeek = (lowerSeek + upperSeek) / 2;

                    streamReader.DiscardBufferedData();
                    streamReader.BaseStream.Seek(meanSeek, SeekOrigin.Begin);

                    var _ = streamReader.ReadLine()!;
                    var nextLineStartSeek = GetActualPosition(streamReader);

                    if (nextLineStartSeek >= upperSeek) //we've gone past - just get the first line between these two
                    {
                        streamReader.DiscardBufferedData();
                        streamReader.BaseStream.Seek(lowerSeek, SeekOrigin.Begin);
                        nextLineStartSeek = lowerSeek;
                    }

                    var line = streamReader.ReadLine()!;
                    var nextLineEndSeek = GetActualPosition(streamReader);

                    return (line, nextLineStartSeek, nextLineEndSeek);
                }
            }
        }

        private static long GetActualPosition(StreamReader reader)
        {
            //https://stackoverflow.com/questions/5404267/streamreader-and-seeking

            const BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;

            // The current buffer of decoded characters
            char[] charBuffer = (char[])reader.GetType().InvokeMember("_charBuffer", flags, null, reader, null)!;

            // The index of the next char to be read from charBuffer
            var charPos = (int)reader.GetType().InvokeMember("_charPos", flags, null, reader, null)!;

            // The number of decoded chars presently used in charBuffer
            var charLen = (int)reader.GetType().InvokeMember("_charLen", flags, null, reader, null)!;

            // The current buffer of read bytes (byteBuffer.Length = 1024; this is critical).
            byte[] byteBuffer = (byte[])reader.GetType().InvokeMember("_byteBuffer", flags, null, reader, null)!;

            // The number of bytes read while advancing reader.BaseStream.Position to (re)fill charBuffer
            var byteLen = (int)reader.GetType().InvokeMember("_byteLen", flags, null, reader, null)!;

            // The number of bytes the remaining chars use in the original encoding.
            var numBytesLeft = reader.CurrentEncoding.GetByteCount(charBuffer, charPos, charLen - charPos);

            // For variable-byte encodings, deal with partial chars at the end of the buffer
            var numFragments = 0;
            if (byteLen <= 0 || reader.CurrentEncoding.IsSingleByte)
                return reader.BaseStream.Position - numBytesLeft - numFragments;
            switch (reader.CurrentEncoding.CodePage)
            {
                // UTF-8
                case 65001:
                {
                    byte byteCountMask = 0;
                    while ((byteBuffer[byteLen - numFragments - 1] >> 6) == 2) // if the byte is "10xx xxxx", it's a continuation-byte
                        byteCountMask |= (byte)(1 << ++numFragments); // count bytes & build the "complete char" mask
                    if ((byteBuffer[byteLen - numFragments - 1] >> 6) == 3) // if the byte is "11xx xxxx", it starts a multi-byte char.
                        byteCountMask |= (byte)(1 << ++numFragments); // count bytes & build the "complete char" mask
                    // see if we found as many bytes as the leading-byte says to expect
                    if (numFragments > 1 && ((byteBuffer[byteLen - numFragments] >> 7 - numFragments) == byteCountMask))
                        numFragments = 0; // no partial-char in the byte-buffer to account for
                    break;
                }
                // UTF-16LE
                case 1200:
                {
                    if (byteBuffer[byteLen - 1] >= 0xd8) // high-surrogate
                        numFragments = 2; // account for the partial character
                    break;
                }
                // UTF-16BE
                case 1201:
                {
                    if (byteBuffer[byteLen - 2] >= 0xd8) // high-surrogate
                        numFragments = 2; // account for the partial character
                    break;
                }
            }
            return reader.BaseStream.Position - numBytesLeft - numFragments;
        }



        /// <inheritdoc />
        public void Dispose() => _streamReader.Dispose();
    }


}
