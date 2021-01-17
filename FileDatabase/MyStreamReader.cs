//This is a copy of https://github.com/microsoft/referencesource/blob/master/mscorlib/system/io/streamreader.cs with some visibility / performance changes

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;
using System.IO;
#pragma warning disable 8625
#pragma warning disable 8618

namespace FileDatabase
{

// This class implements a TextReader for reading characters to a Stream.
// This is designed for character input in a particular Encoding,
// whereas the Stream class is designed for byte input and output.
//
[Serializable]
public class MyStreamReader : TextReader
{

    //Numbers chosen by performance testing
    public const int DefaultBufferSize = 1024;
    public const int DefaultFileStreamBufferSize = 2048;
    public const int MinBufferSize = 256;

    private Stream _stream;
    private Encoding _encoding;
    private Decoder _decoder;
    private byte[] _byteBuffer;
    private char[] _charBuffer;
    private byte[] _preamble; // Encoding's preamble, which identifies this encoding.
    private int _charPos;

    private int _charLen;

    // Record the number of valid bytes in the byteBuffer, for a few checks.
    private int _byteLen;

    // This is used only for preamble detection
    private int _bytePos;

    // This is the maximum number of chars we can get from one call to
    // ReadBuffer.  Used so ReadBuffer can tell when to copy data into
    // a user's char[] directly, instead of our internal char[].
    private int _maxCharsPerBuffer;

    // We will support looking for byte order marks in the stream and trying
    // to decide what the encoding might be from the byte order marks, IF they
    // exist.  But that's all we'll do.
    private bool _detectEncoding;

    // Whether we must still check for the encoding's given preamble at the
    // beginning of this file.
    private bool _checkPreamble;

    // Whether the stream is most likely not going to give us back as much
    // data as we want the next time we call it.  We must do the computation
    // before we do any byte order mark handling and save the result.  Note
    // that we need this to allow users to handle streams used for an
    // interactive protocol, where they block waiting for the remote end
    // to send a response, like logging in on a Unix machine.
    private bool _isBlocked;

    // The intent of this field is to leave open the underlying stream when
    // disposing of this StreamReader.  A name like _leaveOpen is better,
    // but this type is serializable, and this field's name was _closable.
    private bool _closable; // Whether to close the underlying stream.

#if FEATURE_ASYNC_IO
        // We don't guarantee thread safety on StreamReader, but we should at
        // least prevent users from trying to read anything while an Async
        // read from the same thread is in progress.
        [NonSerialized]
        private volatile Task _asyncReadTask;

        private void CheckAsyncTaskInProgress()
        {
            // We are not locking the access to _asyncReadTask because this is not meant to guarantee thread safety.
            // We are simply trying to deter calling any Read APIs while an async Read from the same thread is in progress.

            Task t = _asyncReadTask;

            if (t != null && !t.IsCompleted)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsyncIOInProgress"));
        }
#endif

    // StreamReader by default will ignore illegal UTF8 characters. We don't want to
    // throw here because we want to be able to read ill-formed data without choking.
    // The high level goal is to be tolerant of encoding errors when we read and very strict
    // when we write. Hence, default StreamWriter encoding will throw on error.

    internal MyStreamReader() { }

    public MyStreamReader(Stream stream)
        : this(stream, true) { }

    public MyStreamReader(Stream stream, bool detectEncodingFromByteOrderMarks)
        : this(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks,
            DefaultBufferSize,
            false
        ) { }

    public MyStreamReader(Stream stream, Encoding encoding)
        : this(stream, encoding, true, DefaultBufferSize, false) { }

    public MyStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
        : this(stream, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize, false) { }

    // Creates a new StreamReader for the given stream.  The
    // character encoding is set by encoding and the buffer size,
    // in number of 16-bit characters, is set by bufferSize.
    //
    // Note that detectEncodingFromByteOrderMarks is a very
    // loose attempt at detecting the encoding by looking at the first
    // 3 bytes of the stream.  It will recognize UTF-8, little endian
    // unicode, and big endian unicode text, but that's it.  If neither
    // of those three match, it will use the Encoding you provided.
    //
    public MyStreamReader(
        Stream stream,
        Encoding encoding,
        bool detectEncodingFromByteOrderMarks,
        int bufferSize)
        : this(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false) { }

    public MyStreamReader(
        Stream stream,
        Encoding encoding,
        bool detectEncodingFromByteOrderMarks,
        int bufferSize,
        bool leaveOpen)
    {
        if (stream == null || encoding == null)
            throw new ArgumentNullException((stream == null ? "stream" : "encoding"));

        if (!stream.CanRead)
            throw new ArgumentException("Argument_StreamNotReadable");

        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize) );

        Contract.EndContractBlock();

        Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen);
    }

#if FEATURE_LEGACYNETCF
        [System.Security.SecuritySafeCritical]
#endif // FEATURE_LEGACYNETCF
    [ResourceExposure(ResourceScope.Machine)]
    [ResourceConsumption(ResourceScope.Machine)]
    public MyStreamReader(string path)
        : this(path, true)
    {
#if FEATURE_LEGACYNETCF
            if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                System.Reflection.Assembly callingAssembly =
 System.Reflection.Assembly.GetCallingAssembly();
                if(callingAssembly != null && !callingAssembly.IsProfileAssembly) {
                    string caller = new System.Diagnostics.StackFrame(1).GetMethod().FullName;
                    string callee = System.Reflection.MethodBase.GetCurrentMethod().FullName;
                    throw new MethodAccessException(String.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        Environment.GetResourceString("Arg_MethodAccessException_WithCaller"),
                        caller,
                        callee));
                }
            }
#endif // FEATURE_LEGACYNETCF
    }

    [ResourceExposure(ResourceScope.Machine)]
    [ResourceConsumption(ResourceScope.Machine)]
    public MyStreamReader(string path, bool detectEncodingFromByteOrderMarks)
        : this(path, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize) { }

    [ResourceExposure(ResourceScope.Machine)]
    [ResourceConsumption(ResourceScope.Machine)]
    public MyStreamReader(string path, Encoding encoding)
        : this(path, encoding, true, DefaultBufferSize) { }

    [ResourceExposure(ResourceScope.Machine)]
    [ResourceConsumption(ResourceScope.Machine)]
    public MyStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
        : this(path, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize) { }


    [System.Security.SecurityCritical]
    [ResourceExposure(ResourceScope.Machine)]
    [ResourceConsumption(ResourceScope.Machine)]
    internal MyStreamReader(
        string path,
        Encoding encoding,
        bool detectEncodingFromByteOrderMarks,
        int bufferSize)
    {
        // Don't open a Stream before checking for invalid arguments,
        // or we'll create a FileStream on disk and we won't close it until
        // the finalizer runs, causing problems for applications.
        if (path == null || encoding == null)
            throw new ArgumentNullException((path == null ? "path" : "encoding"));

        if (path.Length == 0)
            throw new ArgumentException("Argument_EmptyPath");

        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize), "ArgumentOutOfRange_NeedPosNum");

        Contract.EndContractBlock();

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            DefaultFileStreamBufferSize,
            FileOptions.RandomAccess //this is a change
        );

        Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false);
    }

        /// <summary>
        /// Gets the actual position
        /// </summary>
        /// <returns></returns>
        public long GetActualPosition()
        {
            //https://stackoverflow.com/questions/5404267/streamreader-and-seeking

            var numBytesLeft =CurrentEncoding.GetByteCount(_charBuffer, _charPos, _charLen - _charPos);

            // For variable-byte encodings, deal with partial chars at the end of the buffer
            var numFragments = 0;

            if (_byteLen <= 0 || CurrentEncoding.IsSingleByte)
                return BaseStream.Position - numBytesLeft - numFragments;

            switch (CurrentEncoding.CodePage)
            {
                // UTF-8
                case 65001:
                    {
                        byte byteCountMask = 0;

                        while (_byteBuffer[_byteLen - numFragments - 1] >> 6 == 2
                        ) // if the byte is "10xx xxxx", it's a continuation-byte
                            byteCountMask |=
                                (byte)(1 << ++numFragments); // count bytes & build the "complete char" mask

                        if (_byteBuffer[_byteLen - numFragments - 1] >> 6 == 3
                        ) // if the byte is "11xx xxxx", it starts a multi-byte char.
                            byteCountMask |=
                                (byte)(1 << ++numFragments); // count bytes & build the "complete char" mask

                        // see if we found as many bytes as the leading-byte says to expect
                        if (numFragments > 1 && _byteBuffer[_byteLen - numFragments] >> (7 - numFragments)
                         == byteCountMask)
                            numFragments = 0; // no partial-char in the byte-buffer to account for

                        break;
                    }
                // UTF-16LE
                case 1200:
                    {
                        if (_byteBuffer[_byteLen - 1] >= 0xd8) // high-surrogate
                            numFragments = 2;                // account for the partial character

                        break;
                    }
                // UTF-16BE
                case 1201:
                    {
                        if (_byteBuffer[_byteLen - 2] >= 0xd8) // high-surrogate
                            numFragments = 2;                // account for the partial character

                        break;
                    }
            }

            return BaseStream.Position - numBytesLeft - numFragments;
        }



        private void Init(
        Stream stream,
        Encoding encoding,
        bool detectEncodingFromByteOrderMarks,
        int bufferSize,
        bool leaveOpen)
    {
        this._stream   = stream;
        this._encoding = encoding;
        _decoder       = encoding.GetDecoder();

        if (bufferSize < MinBufferSize)
            bufferSize = MinBufferSize;

        _byteBuffer         = new byte[bufferSize];
        _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
        _charBuffer         = new char[_maxCharsPerBuffer];
        _byteLen            = 0;
        _bytePos            = 0;
        _detectEncoding    = detectEncodingFromByteOrderMarks;
        _preamble          = encoding.GetPreamble();
        _checkPreamble     = (_preamble.Length > 0);
        _isBlocked         = false;
        _closable          = !leaveOpen;
    }

    // Init used by NullStreamReader, to delay load encoding
    internal void Init(Stream stream)
    {
        _stream = stream;
        _closable   = true;
    }

    public override void Close() => Dispose(true);

    protected override void Dispose(bool disposing)
    {
        // Dispose of our resources if this StreamReader is closable.
        // Note that Console.In should be left open.
        try
        {
            // Note that Stream.Close() can potentially throw here. So we need to
            // ensure cleaning up internal resources, inside the finally block.
            if (!LeaveOpen && disposing && (_stream != null))
                _stream.Close();
        }
        finally
        {
            if (!LeaveOpen && (_stream != null))
            {
                _stream     = null;
                _encoding   = null;
                _decoder    = null;
                _byteBuffer = null;
                _charBuffer = null;
                _charPos    = 0;
                _charLen    = 0;
                base.Dispose(disposing);
            }
        }
    }

    public virtual Encoding CurrentEncoding => _encoding;

    public virtual Stream BaseStream => _stream;

    internal bool LeaveOpen => !_closable;

    // DiscardBufferedData tells StreamReader to throw away its internal
    // buffer contents.  This is useful if the user needs to seek on the
    // underlying stream to a known location then wants the StreamReader
    // to start reading from this new point.  This method should be called
    // very sparingly, if ever, since it can lead to very poor performance.
    // However, it may be the only way of handling some scenarios where
    // users need to re-read the contents of a StreamReader a second time.
    public void DiscardBufferedData()
    {
#if FEATURE_ASYNC_IO
            CheckAsyncTaskInProgress();
#endif

        _byteLen = 0;
        _charLen = 0;
        _charPos = 0;

        // in general we'd like to have an invariant that encoding isn't null. However,
        // for startup improvements for NullStreamReader, we want to delay load encoding.
        if (_encoding != null!)
        {
            _decoder = _encoding.GetDecoder();
        }

        _isBlocked = false;
    }



    public bool EndOfStream
    {
        get
        {
            if (_stream == null)
                throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
                CheckAsyncTaskInProgress();
#endif

            if (_charPos < _charLen)
                return false;

            // This may block on pipes!
            var numRead = ReadBuffer();
            return numRead == 0;
        }
    }

    [Pure]
    public override int Peek()
    {
        if (_stream == null)
            throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
            CheckAsyncTaskInProgress();
#endif

        if (_charPos == _charLen)
        {
            if (_isBlocked || ReadBuffer() == 0)
                return -1;
        }

        return _charBuffer[_charPos];
    }

    public override int Read()
    {
        if (_stream == null)
            throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
            CheckAsyncTaskInProgress();
#endif

        if (_charPos == _charLen)
        {
            if (ReadBuffer() == 0)
                return -1;
        }

        int result = _charBuffer[_charPos];
        _charPos++;
        return result;
    }

    public override int Read([In, Out] char[] buffer, int index, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(
                nameof(buffer)
            );

        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException(
                (index < 0 ? "index" : "count"),"ArgumentOutOfRange_NeedNonNegNum"
            );

        if (buffer.Length - index < count)
            throw new ArgumentException("Argument_InvalidOffLen");

        Contract.EndContractBlock();

        if (_stream == null)
            throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
            CheckAsyncTaskInProgress();
#endif

        var charsRead = 0;
        // As a perf optimization, if we had exactly one buffer's worth of
        // data read in, let's try writing directly to the user's buffer.
        var readToUserBuffer = false;

        while (count > 0)
        {
            var n = _charLen - _charPos;

            if (n == 0)
                n = ReadBuffer(buffer, index + charsRead, count, out readToUserBuffer);

            if (n == 0)
                break; // We're at EOF

            if (n > count)
                n = count;

            if (!readToUserBuffer)
            {
                Buffer.BlockCopy(
                    _charBuffer,
                    _charPos * 2,
                    buffer,
                    (index + charsRead) * 2,
                    n * 2
                );

                _charPos += n;
            }

            charsRead += n;
            count     -= n;

            // This function shouldn't block for an indefinite amount of time,
            // or reading from a network stream won't work right.  If we got
            // fewer bytes than we requested, then we want to break right here.
            if (_isBlocked)
                break;
        }

        return charsRead;
    }

    public override string ReadToEnd()
    {
        if (_stream == null)
            throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
            CheckAsyncTaskInProgress();
#endif

        // Call ReadBuffer, then pull data out of charBuffer.
        StringBuilder sb = new StringBuilder(_charLen - _charPos);

        do
        {
            sb.Append(_charBuffer, _charPos, _charLen - _charPos);
            _charPos = _charLen; // Note we consumed these characters
            ReadBuffer();
        } while (_charLen > 0);

        return sb.ToString();
    }

    public override int ReadBlock([In, Out] char[] buffer, int index, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(
                nameof(buffer)
            );

        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException(
                (index < 0 ? "index" : "count"),"ArgumentOutOfRange_NeedNonNegNum"
            );

        if (buffer.Length - index < count)
            throw new ArgumentException("Argument_InvalidOffLen");

        Contract.EndContractBlock();

        if (_stream == null)
            throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
            CheckAsyncTaskInProgress();
#endif

        return base.ReadBlock(buffer, index, count);
    }

    // Trims n bytes from the front of the buffer.
    private void CompressBuffer(int n)
    {
        Contract.Assert(
            _byteLen >= n,
            "CompressBuffer was called with a number of bytes greater than the current buffer length.  Are two threads using this StreamReader at the same time?"
        );

        Buffer.BlockCopy(_byteBuffer, n, _byteBuffer, 0, _byteLen - n); //TODO use internalblockcopy
        _byteLen -= n;
    }

    private void DetectEncoding()
    {
        if (_byteLen < 2)
            return;

        _detectEncoding = false;
        var changedEncoding = false;

        if (_byteBuffer[0] == 0xFE && _byteBuffer[1] == 0xFF)
        {
            // Big Endian Unicode

            _encoding = new UnicodeEncoding(true, true);
            CompressBuffer(2);
            changedEncoding = true;
        }

        else if (_byteBuffer[0] == 0xFF && _byteBuffer[1] == 0xFE)
        {
            // Little Endian Unicode, or possibly little endian UTF32
            if (_byteLen < 4 || _byteBuffer[2] != 0 || _byteBuffer[3] != 0)
            {
                _encoding = new UnicodeEncoding(false, true);
                CompressBuffer(2);
                changedEncoding = true;
            }
#if FEATURE_UTF32
                else {
                    encoding = new UTF32Encoding(false, true);
                    CompressBuffer(4);
                changedEncoding = true;
            }
#endif
        }

        else if (_byteLen >= 3 && _byteBuffer[0] == 0xEF && _byteBuffer[1] == 0xBB
              && _byteBuffer[2] == 0xBF)
        {
            // UTF-8
            _encoding = Encoding.UTF8;
            CompressBuffer(3);
            changedEncoding = true;
        }
#if FEATURE_UTF32
            else if (byteLen >= 4 && byteBuffer[0] == 0 && byteBuffer[1] == 0 &&
                     byteBuffer[2] == 0xFE && byteBuffer[3] == 0xFF) {
                // Big Endian UTF32
                encoding = new UTF32Encoding(true, true);
                CompressBuffer(4);
                changedEncoding = true;
            }
#endif
        else if (_byteLen == 2)
            _detectEncoding = true;
        // Note: in the future, if we change this algorithm significantly,
        // we can support checking for the preamble of the given encoding.

        if (changedEncoding)
        {
            _decoder            = _encoding.GetDecoder();
            _maxCharsPerBuffer = _encoding.GetMaxCharCount(_byteBuffer.Length);
            _charBuffer         = new char[_maxCharsPerBuffer];
        }
    }

    // Trims the preamble bytes from the byteBuffer. This routine can be called multiple times
    // and we will buffer the bytes read until the preamble is matched or we determine that
    // there is no match. If there is no match, every byte read previously will be available
    // for further consumption. If there is a match, we will compress the buffer for the
    // leading preamble bytes
    private bool IsPreamble()
    {
        if (!_checkPreamble)
            return _checkPreamble;

        Contract.Assert(
            _bytePos <= _preamble.Length,
            "_compressPreamble was called with the current bytePos greater than the preamble buffer length.  Are two threads using this StreamReader at the same time?"
        );

        var len = (_byteLen >= (_preamble.Length))
            ? (_preamble.Length - _bytePos)
            : (_byteLen - _bytePos);

        for (var i = 0; i < len; i++, _bytePos++)
        {
            if (_byteBuffer[_bytePos] != _preamble[_bytePos])
            {
                _bytePos        = 0;
                _checkPreamble = false;
                break;
            }
        }

        Contract.Assert(
            _bytePos <= _preamble.Length,
            "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?"
        );

        if (_checkPreamble)
        {
            if (_bytePos == _preamble.Length)
            {
                // We have a match
                CompressBuffer(_preamble.Length);
                _bytePos         = 0;
                _checkPreamble  = false;
                _detectEncoding = false;
            }
        }

        return _checkPreamble;
    }

    internal virtual int ReadBuffer()
    {
        _charLen = 0;
        _charPos = 0;

        if (!_checkPreamble)
            _byteLen = 0;

        do
        {
            if (_checkPreamble)
            {
                Contract.Assert(
                    _bytePos <= _preamble.Length,
                    "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?"
                );

                var len = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);

                Contract.Assert(
                    len >= 0,
                    "Stream.Read returned a negative number!  This is a bug in your stream class."
                );

                if (len == 0)
                {
                    // EOF but we might have buffered bytes from previous
                    // attempt to detect preamble that needs to be decoded now
                    if (_byteLen > 0)
                    {
                        _charLen += _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, _charLen);
                        // Need to zero out the byteLen after we consume these bytes so that we don't keep infinitely hitting this code path
                        _bytePos = _byteLen = 0;
                    }

                    return _charLen;
                }

                _byteLen += len;
            }
            else
            {
                Contract.Assert(
                    _bytePos == 0,
                    "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?"
                );

                _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);

                Contract.Assert(
                    _byteLen >= 0,
                    "Stream.Read returned a negative number!  This is a bug in your stream class."
                );

                if (_byteLen == 0) // We're at EOF
                    return _charLen;
            }

            // _isBlocked == whether we read fewer bytes than we asked for.
            // Note we must check it here because CompressBuffer or
            // DetectEncoding will change byteLen.
            _isBlocked = (_byteLen < _byteBuffer.Length);

            // Check for preamble before detect encoding. This is not to override the
            // user supplied Encoding for the one we implicitly detect. The user could
            // customize the encoding which we will loose, such as ThrowOnError on UTF8
            if (IsPreamble())
                continue;

            // If we're supposed to detect the encoding and haven't done so yet,
            // do it.  Note this may need to be called more than once.
            if (_detectEncoding && _byteLen >= 2)
                DetectEncoding();

            _charLen += _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, _charLen);
        } while (_charLen == 0);

        //Console.WriteLine("ReadBuffer called.  chars: "+charLen);
        return _charLen;
    }

    // This version has a perf optimization to decode data DIRECTLY into the
    // user's buffer, bypassing StreamReader's own buffer.
    // This gives a > 20% perf improvement for our encodings across the board,
    // but only when asking for at least the number of characters that one
    // buffer's worth of bytes could produce.
    // This optimization, if run, will break SwitchEncoding, so we must not do
    // this on the first call to ReadBuffer.
    private int ReadBuffer(
        char[] userBuffer,
        int userOffset,
        int desiredChars,
        out bool readToUserBuffer)
    {
        _charLen = 0;
        _charPos = 0;

        if (!_checkPreamble)
            _byteLen = 0;

        var charsRead = 0;

        // As a perf optimization, we can decode characters DIRECTLY into a
        // user's char[].  We absolutely must not write more characters
        // into the user's buffer than they asked for.  Calculating
        // encoding.GetMaxCharCount(byteLen) each time is potentially very
        // expensive - instead, cache the number of chars a full buffer's
        // worth of data may produce.  Yes, this makes the perf optimization
        // less aggressive, in that all reads that asked for fewer than AND
        // returned fewer than _maxCharsPerBuffer chars won't get the user
        // buffer optimization.  This affects reads where the end of the
        // Stream comes in the middle somewhere, and when you ask for
        // fewer chars than your buffer could produce.
        readToUserBuffer = desiredChars >= _maxCharsPerBuffer;

        do
        {
            Contract.Assert(charsRead == 0);

            if (_checkPreamble)
            {
                Contract.Assert(
                    _bytePos <= _preamble.Length,
                    "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?"
                );

                var len = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);

                Contract.Assert(
                    len >= 0,
                    "Stream.Read returned a negative number!  This is a bug in your stream class."
                );

                if (len == 0)
                {
                    // EOF but we might have buffered bytes from previous
                    // attempt to detect preamble that needs to be decoded now
                    if (_byteLen > 0)
                    {
                        if (readToUserBuffer)
                        {
                            charsRead = _decoder.GetChars(
                                _byteBuffer,
                                0,
                                _byteLen,
                                userBuffer,
                                userOffset + charsRead
                            );

                            _charLen = 0; // StreamReader's buffer is empty.
                        }
                        else
                        {
                            charsRead = _decoder.GetChars(
                                _byteBuffer,
                                0,
                                _byteLen,
                                _charBuffer,
                                charsRead
                            );

                            _charLen += charsRead; // Number of chars in StreamReader's buffer.
                        }
                    }

                    return charsRead;
                }

                _byteLen += len;
            }
            else
            {
                Contract.Assert(
                    _bytePos == 0,
                    "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?"
                );

                _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);

                Contract.Assert(
                    _byteLen >= 0,
                    "Stream.Read returned a negative number!  This is a bug in your stream class."
                );

                if (_byteLen == 0) // EOF
                    break;
            }

            // _isBlocked == whether we read fewer bytes than we asked for.
            // Note we must check it here because CompressBuffer or
            // DetectEncoding will change byteLen.
            _isBlocked = (_byteLen < _byteBuffer.Length);

            // Check for preamble before detect encoding. This is not to override the
            // user supplied Encoding for the one we implicitly detect. The user could
            // customize the encoding which we will loose, such as ThrowOnError on UTF8
            // Note: we don't need to recompute readToUserBuffer optimization as IsPreamble
            // doesn't change the encoding or affect _maxCharsPerBuffer
            if (IsPreamble())
                continue;

            // On the first call to ReadBuffer, if we're supposed to detect the encoding, do it.
            if (_detectEncoding && _byteLen >= 2)
            {
                DetectEncoding();
                // DetectEncoding changes some buffer state.  Recompute this.
                readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
            }

            _charPos = 0;

            if (readToUserBuffer)
            {
                charsRead += _decoder.GetChars(
                    _byteBuffer,
                    0,
                    _byteLen,
                    userBuffer,
                    userOffset + charsRead
                );

                _charLen = 0; // StreamReader's buffer is empty.
            }
            else
            {
                charsRead =  _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, charsRead);
                _charLen   += charsRead; // Number of chars in StreamReader's buffer.
            }
        } while (charsRead == 0);

        _isBlocked &= charsRead < desiredChars;

        //Console.WriteLine("ReadBuffer: charsRead: "+charsRead+"  readToUserBuffer: "+readToUserBuffer);
        return charsRead;
    }

    // Reads a line. A line is defined as a sequence of characters followed by
    // a carriage return ('\r'), a line feed ('\n'), or a carriage return
    // immediately followed by a line feed. The resulting string does not
    // contain the terminating carriage return and/or line feed. The returned
    // value is null if the end of the input stream has been reached.
    //
    public override string? ReadLine()
    {
        if (_stream == null)
            throw new Exception("Reader Closed");

        if (_charPos == _charLen)
        {
            if (ReadBuffer() == 0)
                return null;
        }

        StringBuilder? sb = null;

        do
        {
            var i = _charPos;

            do
            {
                var ch = _charBuffer[i];

                // Note the following common line feed chars:
                // \n - UNIX   \r\n - DOS   \r - Mac
                if (ch == '\r' || ch == '\n')
                {
                    string s;

                    if (sb != null)
                    {
                        sb.Append(_charBuffer, _charPos, i - _charPos);
                        s = sb.ToString();
                    }
                    else
                    {
                        s = new string(_charBuffer, _charPos, i - _charPos);
                    }

                    _charPos = i + 1;

                    if (ch == '\r' && (_charPos < _charLen || ReadBuffer() > 0))
                    {
                        if (_charBuffer[_charPos] == '\n')
                            _charPos++;
                    }

                    return s;
                }

                i++;
            } while (i < _charLen);

            i = _charLen - _charPos;

            sb ??= new StringBuilder(i + 80);

            sb.Append(_charBuffer, _charPos, i);
        } while (ReadBuffer() > 0);

        return sb.ToString();
    }
}

}
