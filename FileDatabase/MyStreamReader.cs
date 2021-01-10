//This is a copy of https://github.com/microsoft/referencesource/blob/master/mscorlib/system/io/streamreader.cs with some visibility / performance changes

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security.Permissions;

#if FEATURE_ASYNC_IO
using System.Threading.Tasks;
#endif

namespace FileDatabase
{

// This class implements a TextReader for reading characters to a Stream.
// This is designed for character input in a particular Encoding,
// whereas the Stream class is designed for byte input and output.
//
[Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
public class MyStreamReader : TextReader
{
    // StreamReader.Null is threadsafe.
    public new static readonly MyStreamReader Null = new NullMyStreamReader();


    //Numbers chosen by perfromance testing
    public const int DefaultBufferSize = 1024;
    public const int DefaultFileStreamBufferSize = 2048;
    public const int MinBufferSize = 256;

    private Stream stream;
    private Encoding encoding;
    private Decoder decoder;
    private byte[] byteBuffer;
    private char[] charBuffer;
    private byte[] _preamble; // Encoding's preamble, which identifies this encoding.
    private int charPos;

    private int charLen;

    // Record the number of valid bytes in the byteBuffer, for a few checks.
    private int byteLen;

    // This is used only for preamble detection
    private int bytePos;

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
            throw new ArgumentOutOfRangeException("bufferSize", "ArgumentOutOfRange_NeedPosNum");

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

    [System.Security.SecuritySafeCritical]
    [ResourceExposure(ResourceScope.Machine)]
    [ResourceConsumption(ResourceScope.Machine)]
    public MyStreamReader(
        string path,
        Encoding encoding,
        bool detectEncodingFromByteOrderMarks,
        int bufferSize)
        : this(path, encoding, detectEncodingFromByteOrderMarks, bufferSize, true) { }

    [System.Security.SecurityCritical]
    [ResourceExposure(ResourceScope.Machine)]
    [ResourceConsumption(ResourceScope.Machine)]
    internal MyStreamReader(
        string path,
        Encoding encoding,
        bool detectEncodingFromByteOrderMarks,
        int bufferSize,
        bool checkHost)
    {
        // Don't open a Stream before checking for invalid arguments,
        // or we'll create a FileStream on disk and we won't close it until
        // the finalizer runs, causing problems for applications.
        if (path == null || encoding == null)
            throw new ArgumentNullException((path == null ? "path" : "encoding"));

        if (path.Length == 0)
            throw new ArgumentException("Argument_EmptyPath");

        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException("bufferSize", "ArgumentOutOfRange_NeedPosNum");

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

            // The current buffer of decoded characters

            // The index of the next char to be read from charBuffer

            // The number of decoded chars presently used in charBuffer

            // The current buffer of read bytes (byteBuffer.Length = 1024; this is critical).

            // The number of bytes read while advancing reader.BaseStream.Position to (re)fill charBuffer

            // The number of bytes the remaining chars use in the original encoding.
            var numBytesLeft =CurrentEncoding.GetByteCount(charBuffer, charPos, charLen - charPos);

            // For variable-byte encodings, deal with partial chars at the end of the buffer
            var numFragments = 0;

            if (byteLen <= 0 || CurrentEncoding.IsSingleByte)
                return BaseStream.Position - numBytesLeft - numFragments;

            switch (CurrentEncoding.CodePage)
            {
                // UTF-8
                case 65001:
                    {
                        byte byteCountMask = 0;

                        while (byteBuffer[byteLen - numFragments - 1] >> 6 == 2
                        ) // if the byte is "10xx xxxx", it's a continuation-byte
                            byteCountMask |=
                                (byte)(1 << ++numFragments); // count bytes & build the "complete char" mask

                        if (byteBuffer[byteLen - numFragments - 1] >> 6 == 3
                        ) // if the byte is "11xx xxxx", it starts a multi-byte char.
                            byteCountMask |=
                                (byte)(1 << ++numFragments); // count bytes & build the "complete char" mask

                        // see if we found as many bytes as the leading-byte says to expect
                        if (numFragments > 1 && byteBuffer[byteLen - numFragments] >> (7 - numFragments)
                         == byteCountMask)
                            numFragments = 0; // no partial-char in the byte-buffer to account for

                        break;
                    }
                // UTF-16LE
                case 1200:
                    {
                        if (byteBuffer[byteLen - 1] >= 0xd8) // high-surrogate
                            numFragments = 2;                // account for the partial character

                        break;
                    }
                // UTF-16BE
                case 1201:
                    {
                        if (byteBuffer[byteLen - 2] >= 0xd8) // high-surrogate
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
        this.stream   = stream;
        this.encoding = encoding;
        decoder       = encoding.GetDecoder();

        if (bufferSize < MinBufferSize)
            bufferSize = MinBufferSize;

        byteBuffer         = new byte[bufferSize];
        _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
        charBuffer         = new char[_maxCharsPerBuffer];
        byteLen            = 0;
        bytePos            = 0;
        _detectEncoding    = detectEncodingFromByteOrderMarks;
        _preamble          = encoding.GetPreamble();
        _checkPreamble     = (_preamble.Length > 0);
        _isBlocked         = false;
        _closable          = !leaveOpen;
    }

    // Init used by NullStreamReader, to delay load encoding
    internal void Init(Stream stream)
    {
        this.stream = stream;
        _closable   = true;
    }

    public override void Close()
    {
        Dispose(true);
    }

    protected override void Dispose(bool disposing)
    {
        // Dispose of our resources if this StreamReader is closable.
        // Note that Console.In should be left open.
        try
        {
            // Note that Stream.Close() can potentially throw here. So we need to
            // ensure cleaning up internal resources, inside the finally block.
            if (!LeaveOpen && disposing && (stream != null))
                stream.Close();
        }
        finally
        {
            if (!LeaveOpen && (stream != null))
            {
                stream     = null;
                encoding   = null;
                decoder    = null;
                byteBuffer = null;
                charBuffer = null;
                charPos    = 0;
                charLen    = 0;
                base.Dispose(disposing);
            }
        }
    }

    public virtual Encoding CurrentEncoding => encoding;

    public virtual Stream BaseStream => stream;

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

        byteLen = 0;
        charLen = 0;
        charPos = 0;

        // in general we'd like to have an invariant that encoding isn't null. However,
        // for startup improvements for NullStreamReader, we want to delay load encoding.
        if (encoding != null)
        {
            decoder = encoding.GetDecoder();
        }

        _isBlocked = false;
    }



    public bool EndOfStream
    {
        get
        {
            if (stream == null)
                throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
                CheckAsyncTaskInProgress();
#endif

            if (charPos < charLen)
                return false;

            // This may block on pipes!
            var numRead = ReadBuffer();
            return numRead == 0;
        }
    }

    [Pure]
    public override int Peek()
    {
        if (stream == null)
            throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
            CheckAsyncTaskInProgress();
#endif

        if (charPos == charLen)
        {
            if (_isBlocked || ReadBuffer() == 0)
                return -1;
        }

        return charBuffer[charPos];
    }

    public override int Read()
    {
        if (stream == null)
            throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
            CheckAsyncTaskInProgress();
#endif

        if (charPos == charLen)
        {
            if (ReadBuffer() == 0)
                return -1;
        }

        int result = charBuffer[charPos];
        charPos++;
        return result;
    }

    public override int Read([In, Out] char[] buffer, int index, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(
                "buffer","ArgumentNull_Buffer"
            );

        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException(
                (index < 0 ? "index" : "count"),"ArgumentOutOfRange_NeedNonNegNum"
            );

        if (buffer.Length - index < count)
            throw new ArgumentException("Argument_InvalidOffLen");

        Contract.EndContractBlock();

        if (stream == null)
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
            var n = charLen - charPos;

            if (n == 0)
                n = ReadBuffer(buffer, index + charsRead, count, out readToUserBuffer);

            if (n == 0)
                break; // We're at EOF

            if (n > count)
                n = count;

            if (!readToUserBuffer)
            {
                Buffer.BlockCopy(
                    charBuffer,
                    charPos * 2,
                    buffer,
                    (index + charsRead) * 2,
                    n * 2
                );

                charPos += n;
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
        if (stream == null)
            throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
            CheckAsyncTaskInProgress();
#endif

        // Call ReadBuffer, then pull data out of charBuffer.
        StringBuilder sb = new StringBuilder(charLen - charPos);

        do
        {
            sb.Append(charBuffer, charPos, charLen - charPos);
            charPos = charLen; // Note we consumed these characters
            ReadBuffer();
        } while (charLen > 0);

        return sb.ToString();
    }

    public override int ReadBlock([In, Out] char[] buffer, int index, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(
                "buffer","ArgumentNull_Buffer"
            );

        if (index < 0 || count < 0)
            throw new ArgumentOutOfRangeException(
                (index < 0 ? "index" : "count"),"ArgumentOutOfRange_NeedNonNegNum"
            );

        if (buffer.Length - index < count)
            throw new ArgumentException("Argument_InvalidOffLen");

        Contract.EndContractBlock();

        if (stream == null)
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
            byteLen >= n,
            "CompressBuffer was called with a number of bytes greater than the current buffer length.  Are two threads using this StreamReader at the same time?"
        );

        Buffer.BlockCopy(byteBuffer, n, byteBuffer, 0, byteLen - n); //TODO use internalblockcopy
        byteLen -= n;
    }

    private void DetectEncoding()
    {
        if (byteLen < 2)
            return;

        _detectEncoding = false;
        var changedEncoding = false;

        if (byteBuffer[0] == 0xFE && byteBuffer[1] == 0xFF)
        {
            // Big Endian Unicode

            encoding = new UnicodeEncoding(true, true);
            CompressBuffer(2);
            changedEncoding = true;
        }

        else if (byteBuffer[0] == 0xFF && byteBuffer[1] == 0xFE)
        {
            // Little Endian Unicode, or possibly little endian UTF32
            if (byteLen < 4 || byteBuffer[2] != 0 || byteBuffer[3] != 0)
            {
                encoding = new UnicodeEncoding(false, true);
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

        else if (byteLen >= 3 && byteBuffer[0] == 0xEF && byteBuffer[1] == 0xBB
              && byteBuffer[2] == 0xBF)
        {
            // UTF-8
            encoding = Encoding.UTF8;
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
        else if (byteLen == 2)
            _detectEncoding = true;
        // Note: in the future, if we change this algorithm significantly,
        // we can support checking for the preamble of the given encoding.

        if (changedEncoding)
        {
            decoder            = encoding.GetDecoder();
            _maxCharsPerBuffer = encoding.GetMaxCharCount(byteBuffer.Length);
            charBuffer         = new char[_maxCharsPerBuffer];
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
            bytePos <= _preamble.Length,
            "_compressPreamble was called with the current bytePos greater than the preamble buffer length.  Are two threads using this StreamReader at the same time?"
        );

        var len = (byteLen >= (_preamble.Length))
            ? (_preamble.Length - bytePos)
            : (byteLen - bytePos);

        for (var i = 0; i < len; i++, bytePos++)
        {
            if (byteBuffer[bytePos] != _preamble[bytePos])
            {
                bytePos        = 0;
                _checkPreamble = false;
                break;
            }
        }

        Contract.Assert(
            bytePos <= _preamble.Length,
            "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?"
        );

        if (_checkPreamble)
        {
            if (bytePos == _preamble.Length)
            {
                // We have a match
                CompressBuffer(_preamble.Length);
                bytePos         = 0;
                _checkPreamble  = false;
                _detectEncoding = false;
            }
        }

        return _checkPreamble;
    }

    internal virtual int ReadBuffer()
    {
        charLen = 0;
        charPos = 0;

        if (!_checkPreamble)
            byteLen = 0;

        do
        {
            if (_checkPreamble)
            {
                Contract.Assert(
                    bytePos <= _preamble.Length,
                    "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?"
                );

                var len = stream.Read(byteBuffer, bytePos, byteBuffer.Length - bytePos);

                Contract.Assert(
                    len >= 0,
                    "Stream.Read returned a negative number!  This is a bug in your stream class."
                );

                if (len == 0)
                {
                    // EOF but we might have buffered bytes from previous
                    // attempt to detect preamble that needs to be decoded now
                    if (byteLen > 0)
                    {
                        charLen += decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charLen);
                        // Need to zero out the byteLen after we consume these bytes so that we don't keep infinitely hitting this code path
                        bytePos = byteLen = 0;
                    }

                    return charLen;
                }

                byteLen += len;
            }
            else
            {
                Contract.Assert(
                    bytePos == 0,
                    "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?"
                );

                byteLen = stream.Read(byteBuffer, 0, byteBuffer.Length);

                Contract.Assert(
                    byteLen >= 0,
                    "Stream.Read returned a negative number!  This is a bug in your stream class."
                );

                if (byteLen == 0) // We're at EOF
                    return charLen;
            }

            // _isBlocked == whether we read fewer bytes than we asked for.
            // Note we must check it here because CompressBuffer or
            // DetectEncoding will change byteLen.
            _isBlocked = (byteLen < byteBuffer.Length);

            // Check for preamble before detect encoding. This is not to override the
            // user suppplied Encoding for the one we implicitly detect. The user could
            // customize the encoding which we will loose, such as ThrowOnError on UTF8
            if (IsPreamble())
                continue;

            // If we're supposed to detect the encoding and haven't done so yet,
            // do it.  Note this may need to be called more than once.
            if (_detectEncoding && byteLen >= 2)
                DetectEncoding();

            charLen += decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charLen);
        } while (charLen == 0);

        //Console.WriteLine("ReadBuffer called.  chars: "+charLen);
        return charLen;
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
        charLen = 0;
        charPos = 0;

        if (!_checkPreamble)
            byteLen = 0;

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
                    bytePos <= _preamble.Length,
                    "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?"
                );

                var len = stream.Read(byteBuffer, bytePos, byteBuffer.Length - bytePos);

                Contract.Assert(
                    len >= 0,
                    "Stream.Read returned a negative number!  This is a bug in your stream class."
                );

                if (len == 0)
                {
                    // EOF but we might have buffered bytes from previous
                    // attempt to detect preamble that needs to be decoded now
                    if (byteLen > 0)
                    {
                        if (readToUserBuffer)
                        {
                            charsRead = decoder.GetChars(
                                byteBuffer,
                                0,
                                byteLen,
                                userBuffer,
                                userOffset + charsRead
                            );

                            charLen = 0; // StreamReader's buffer is empty.
                        }
                        else
                        {
                            charsRead = decoder.GetChars(
                                byteBuffer,
                                0,
                                byteLen,
                                charBuffer,
                                charsRead
                            );

                            charLen += charsRead; // Number of chars in StreamReader's buffer.
                        }
                    }

                    return charsRead;
                }

                byteLen += len;
            }
            else
            {
                Contract.Assert(
                    bytePos == 0,
                    "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?"
                );

                byteLen = stream.Read(byteBuffer, 0, byteBuffer.Length);

                Contract.Assert(
                    byteLen >= 0,
                    "Stream.Read returned a negative number!  This is a bug in your stream class."
                );

                if (byteLen == 0) // EOF
                    break;
            }

            // _isBlocked == whether we read fewer bytes than we asked for.
            // Note we must check it here because CompressBuffer or
            // DetectEncoding will change byteLen.
            _isBlocked = (byteLen < byteBuffer.Length);

            // Check for preamble before detect encoding. This is not to override the
            // user suppplied Encoding for the one we implicitly detect. The user could
            // customize the encoding which we will loose, such as ThrowOnError on UTF8
            // Note: we don't need to recompute readToUserBuffer optimization as IsPreamble
            // doesn't change the encoding or affect _maxCharsPerBuffer
            if (IsPreamble())
                continue;

            // On the first call to ReadBuffer, if we're supposed to detect the encoding, do it.
            if (_detectEncoding && byteLen >= 2)
            {
                DetectEncoding();
                // DetectEncoding changes some buffer state.  Recompute this.
                readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
            }

            charPos = 0;

            if (readToUserBuffer)
            {
                charsRead += decoder.GetChars(
                    byteBuffer,
                    0,
                    byteLen,
                    userBuffer,
                    userOffset + charsRead
                );

                charLen = 0; // StreamReader's buffer is empty.
            }
            else
            {
                charsRead =  decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charsRead);
                charLen   += charsRead; // Number of chars in StreamReader's buffer.
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
    public override string ReadLine()
    {
        if (stream == null)
            throw new Exception("Reader Closed");

#if FEATURE_ASYNC_IO
            CheckAsyncTaskInProgress();
#endif

        if (charPos == charLen)
        {
            if (ReadBuffer() == 0)
                return null;
        }

        StringBuilder sb = null;

        do
        {
            var i = charPos;

            do
            {
                var ch = charBuffer[i];

                // Note the following common line feed chars:
                // \n - UNIX   \r\n - DOS   \r - Mac
                if (ch == '\r' || ch == '\n')
                {
                    string s;

                    if (sb != null)
                    {
                        sb.Append(charBuffer, charPos, i - charPos);
                        s = sb.ToString();
                    }
                    else
                    {
                        s = new string(charBuffer, charPos, i - charPos);
                    }

                    charPos = i + 1;

                    if (ch == '\r' && (charPos < charLen || ReadBuffer() > 0))
                    {
                        if (charBuffer[charPos] == '\n')
                            charPos++;
                    }

                    return s;
                }

                i++;
            } while (i < charLen);

            i = charLen - charPos;

            if (sb == null)
                sb = new StringBuilder(i + 80);

            sb.Append(charBuffer, charPos, i);
        } while (ReadBuffer() > 0);

        return sb.ToString();
    }

#if FEATURE_ASYNC_IO
        #region Task based Async APIs
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task<String> ReadLineAsync()
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overriden.
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (this.GetType() != typeof(StreamReader))
                return base.ReadLineAsync();

            if (stream == null)
                __Error.ReaderClosed();

            CheckAsyncTaskInProgress();

            Task<String> task = ReadLineAsyncInternal();
            _asyncReadTask = task;

            return task;
        }

        private async Task<String> ReadLineAsyncInternal()
        {
            if (CharPos_Prop == CharLen_Prop && (await ReadBufferAsync().ConfigureAwait(false)) == 0)
                return null;

            StringBuilder sb = null;

            do
            {
                char[] tmpCharBuffer = CharBuffer_Prop;
                int tmpCharLen = CharLen_Prop;
                int tmpCharPos = CharPos_Prop;
                int i = tmpCharPos;

                do
                {
                    char ch = tmpCharBuffer[i];

                    // Note the following common line feed chars:
                    // \n - UNIX   \r\n - DOS   \r - Mac
                    if (ch == '\r' || ch == '\n')
                    {
                        String s;

                        if (sb != null)
                        {
                            sb.Append(tmpCharBuffer, tmpCharPos, i - tmpCharPos);
                            s = sb.ToString();
                        }
                        else
                        {
                            s = new String(tmpCharBuffer, tmpCharPos, i - tmpCharPos);
                        }

                        CharPos_Prop = tmpCharPos = i + 1;

                        if (ch == '\r' && (tmpCharPos < tmpCharLen || (await ReadBufferAsync().ConfigureAwait(false)) > 0))
                        {
                            tmpCharPos = CharPos_Prop;
                            if (CharBuffer_Prop[tmpCharPos] == '\n')
                                CharPos_Prop = ++tmpCharPos;
                        }

                        return s;
                    }

                    i++;

                } while (i < tmpCharLen);

                i = tmpCharLen - tmpCharPos;
                if (sb == null) sb = new StringBuilder(i + 80);
                sb.Append(tmpCharBuffer, tmpCharPos, i);

            } while (await ReadBufferAsync().ConfigureAwait(false) > 0);

            return sb.ToString();
        }

        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task<String> ReadToEndAsync()
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overriden.
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (this.GetType() != typeof(StreamReader))
                return base.ReadToEndAsync();

            if (stream == null)
                __Error.ReaderClosed();

            CheckAsyncTaskInProgress();

            Task<String> task = ReadToEndAsyncInternal();
            _asyncReadTask = task;

            return task;
        }

        private async Task<String> ReadToEndAsyncInternal()
        {
            // Call ReadBuffer, then pull data out of charBuffer.
            StringBuilder sb = new StringBuilder(CharLen_Prop - CharPos_Prop);
            do
            {
                int tmpCharPos = CharPos_Prop;
                sb.Append(CharBuffer_Prop, tmpCharPos, CharLen_Prop - tmpCharPos);
                CharPos_Prop = CharLen_Prop;  // We consumed these characters
                await ReadBufferAsync().ConfigureAwait(false);
            } while (CharLen_Prop > 0);

            return sb.ToString();
        }

        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            if (buffer==null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overriden.
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (this.GetType() != typeof(StreamReader))
                return base.ReadAsync(buffer, index, count);

            if (stream == null)
                __Error.ReaderClosed();

            CheckAsyncTaskInProgress();

            Task<int> task = ReadAsyncInternal(buffer, index, count);
            _asyncReadTask = task;

            return task;
        }

        internal override async Task<int> ReadAsyncInternal(char[] buffer, int index, int count)
        {
            if (CharPos_Prop == CharLen_Prop && (await ReadBufferAsync().ConfigureAwait(false)) == 0)
                return 0;

            int charsRead = 0;

            // As a perf optimization, if we had exactly one buffer's worth of
            // data read in, let's try writing directly to the user's buffer.
            bool readToUserBuffer = false;

            Byte[] tmpByteBuffer = ByteBuffer_Prop;
            Stream tmpStream = Stream_Prop;

            while (count > 0)
            {
                // n is the cha----ters avaialbe in _charBuffer
                int n = CharLen_Prop - CharPos_Prop;

                // charBuffer is empty, let's read from the stream
                if (n == 0)
                {
                    CharLen_Prop = 0;
                    CharPos_Prop = 0;

                    if (!CheckPreamble_Prop)
                        ByteLen_Prop = 0;

                    readToUserBuffer = count >= MaxCharsPerBuffer_Prop;

                    // We loop here so that we read in enough bytes to yield at least 1 char.
                    // We break out of the loop if the stream is blocked (EOF is reached).
                    do
                    {
                        Contract.Assert(n == 0);

                        if (CheckPreamble_Prop)
                        {
                            Contract.Assert(BytePos_Prop <= Preamble_Prop.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                            int tmpBytePos = BytePos_Prop;
                            int len =
 await tmpStream.ReadAsync(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos).ConfigureAwait(false);
                            Contract.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                            if (len == 0)
                            {
                                // EOF but we might have buffered bytes from previous
                                // attempts to detect preamble that needs to be decoded now
                                if (ByteLen_Prop > 0)
                                {
                                    if (readToUserBuffer)
                                    {
                                        n =
 Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, buffer, index + charsRead);
                                        CharLen_Prop = 0;  // StreamReader's buffer is empty.
                                    }
                                    else
                                    {
                                        n =
 Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, 0);
                                        CharLen_Prop +=
 n;  // Number of chars in StreamReader's buffer.
                                    }
                                }

                                // How can part of the preamble yield any chars?
                                Contract.Assert(n == 0);

                                IsBlocked_Prop = true;
                                break;
                            }
                            else
                            {
                                ByteLen_Prop += len;
                            }
                        }
                        else
                        {
                            Contract.Assert(BytePos_Prop == 0, "_bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");

                            ByteLen_Prop =
 await tmpStream.ReadAsync(tmpByteBuffer, 0, tmpByteBuffer.Length).ConfigureAwait(false);

                            Contract.Assert(ByteLen_Prop >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                            if (ByteLen_Prop == 0)  // EOF
                            {
                                IsBlocked_Prop = true;
                                break;
                            }
                        }

                        // _isBlocked == whether we read fewer bytes than we asked for.
                        // Note we must check it here because CompressBuffer or
                        // DetectEncoding will change _byteLen.
                        IsBlocked_Prop = (ByteLen_Prop < tmpByteBuffer.Length);

                        // Check for preamble before detect encoding. This is not to override the
                        // user suppplied Encoding for the one we implicitly detect. The user could
                        // customize the encoding which we will loose, such as ThrowOnError on UTF8
                        // Note: we don't need to recompute readToUserBuffer optimization as IsPreamble
                        // doesn't change the encoding or affect _maxCharsPerBuffer
                        if (IsPreamble())
                            continue;

                        // On the first call to ReadBuffer, if we're supposed to detect the encoding, do it.
                        if (DetectEncoding_Prop && ByteLen_Prop >= 2)
                        {
                            DetectEncoding();
                            // DetectEncoding changes some buffer state.  Recompute this.
                            readToUserBuffer = count >= MaxCharsPerBuffer_Prop;
                        }

                        Contract.Assert(n == 0);

                        CharPos_Prop = 0;
                        if (readToUserBuffer)
                        {
                            n +=
 Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, buffer, index + charsRead);

                            // Why did the bytes yield no chars?
                            Contract.Assert(n > 0);

                            CharLen_Prop = 0;  // StreamReader's buffer is empty.
                        }
                        else
                        {
                            n =
 Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, 0);

                            // Why did the bytes yield no chars?
                            Contract.Assert(n > 0);

                            CharLen_Prop += n;  // Number of chars in StreamReader's buffer.
                        }

                    } while (n == 0);

                    if (n == 0) break;  // We're at EOF
                }  // if (n == 0)

                // Got more chars in charBuffer than the user requested
                if (n > count)
                    n = count;

                if (!readToUserBuffer)
                {
                    Buffer.InternalBlockCopy(CharBuffer_Prop, CharPos_Prop * 2, buffer, (index + charsRead) * 2, n * 2);
                    CharPos_Prop += n;
                }

                charsRead += n;
                count -= n;

                // This function shouldn't block for an indefinite amount of time,
                // or reading from a network stream won't work right.  If we got
                // fewer bytes than we requested, then we want to break right here.
                if (IsBlocked_Prop)
                    break;
            }  // while (count > 0)

            return charsRead;
        }

        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            if (buffer==null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overriden.
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (this.GetType() != typeof(StreamReader))
                return base.ReadBlockAsync(buffer, index, count);

            if (stream == null)
                __Error.ReaderClosed();

            CheckAsyncTaskInProgress();

            Task<int> task = base.ReadBlockAsync(buffer, index, count);
            _asyncReadTask = task;

            return task;
        }

        #region Private properties for async method performance
        // Access to instance fields of MarshalByRefObject-derived types requires special JIT helpers that check
        // if the instance operated on is remote. This is optimised for fields on this but if a method is Async
        // and is thus lifted to a state machine type, access will be slow.
        // As a workaround, we either cache instance fields in locals or use properties to access such fields.

        // See Dev11 bug #370300 for more info.

        private Int32 CharLen_Prop {
            get { return charLen; }
            set { charLen = value; }
        }

        private Int32 CharPos_Prop {
            get { return charPos; }
            set { charPos = value; }
        }

        private Int32 ByteLen_Prop {
            get { return byteLen; }
            set { byteLen = value; }
        }

        private Int32 BytePos_Prop {
            get { return bytePos; }
            set { bytePos = value; }
        }

        private Byte[] Preamble_Prop {
            get { return _preamble; }
        }

        private bool CheckPreamble_Prop {
            get { return _checkPreamble; }
        }

        private Decoder Decoder_Prop {
            get { return decoder; }
        }

        private bool DetectEncoding_Prop {
            get { return _detectEncoding; }
        }

        private Char[]  CharBuffer_Prop {
            get { return charBuffer; }
        }

        private Byte[]  ByteBuffer_Prop {
            get { return byteBuffer; }
        }

        private bool IsBlocked_Prop {
            get { return _isBlocked; }
            set { _isBlocked = value; }
        }

        private Stream Stream_Prop {
            get { return stream; }
        }

        private Int32 MaxCharsPerBuffer_Prop {
            get { return _maxCharsPerBuffer; }
        }
        #endregion Private properties for async method performance
        private async Task<int> ReadBufferAsync()
        {
            CharLen_Prop = 0;
            CharPos_Prop = 0;
            Byte[] tmpByteBuffer = ByteBuffer_Prop;
            Stream tmpStream = Stream_Prop;

            if (!CheckPreamble_Prop)
                ByteLen_Prop = 0;
            do {
                if (CheckPreamble_Prop) {
                    Contract.Assert(BytePos_Prop <= Preamble_Prop.Length, "possible bug in _compressPreamble. Are two threads using this StreamReader at the same time?");
                    int tmpBytePos = BytePos_Prop;
                    int len =
 await tmpStream.ReadAsync(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos).ConfigureAwait(false);
                    Contract.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0) {
                        // EOF but we might have buffered bytes from previous
                        // attempt to detect preamble that needs to be decoded now
                        if (ByteLen_Prop > 0)
                        {
                            CharLen_Prop +=
 Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, CharLen_Prop);
                            // Need to zero out the _byteLen after we consume these bytes so that we don't keep infinitely hitting this code path
                            BytePos_Prop = 0; ByteLen_Prop = 0;
                        }

                        return CharLen_Prop;
                    }

                    ByteLen_Prop += len;
                }
                else {
                    Contract.Assert(BytePos_Prop == 0, "_bytePos can be non zero only when we are trying to _checkPreamble. Are two threads using this StreamReader at the same time?");
                    ByteLen_Prop =
 await tmpStream.ReadAsync(tmpByteBuffer, 0, tmpByteBuffer.Length).ConfigureAwait(false);
                    Contract.Assert(ByteLen_Prop >= 0, "Stream.Read returned a negative number!  Bug in stream class.");

                    if (ByteLen_Prop == 0)  // We're at EOF
                        return CharLen_Prop;
                }

                // _isBlocked == whether we read fewer bytes than we asked for.
                // Note we must check it here because CompressBuffer or
                // DetectEncoding will change _byteLen.
                IsBlocked_Prop = (ByteLen_Prop < tmpByteBuffer.Length);

                // Check for preamble before detect encoding. This is not to override the
                // user suppplied Encoding for the one we implicitly detect. The user could
                // customize the encoding which we will loose, such as ThrowOnError on UTF8
                if (IsPreamble())
                    continue;

                // If we're supposed to detect the encoding and haven't done so yet,
                // do it.  Note this may need to be called more than once.
                if (DetectEncoding_Prop && ByteLen_Prop >= 2)
                    DetectEncoding();

                CharLen_Prop +=
 Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, CharLen_Prop);
            } while (CharLen_Prop == 0);

            return CharLen_Prop;
        }
        #endregion
#endif //FEATURE_ASYNC_IO

    // No data, class doesn't need to be serializable.
    // Note this class is threadsafe.
    private class NullMyStreamReader : MyStreamReader
    {
        // Instantiating Encoding causes unnecessary perf hit.
        internal NullMyStreamReader()
        {
            Init(Stream.Null);
        }

        public override Stream BaseStream => Stream.Null;

        public override Encoding CurrentEncoding => Encoding.Unicode;

        protected override void Dispose(bool disposing)
        {
            // Do nothing - this is essentially unclosable.
        }

        public override int Peek()
        {
            return -1;
        }

        public override int Read()
        {
            return -1;
        }

        [SuppressMessage(
            "Microsoft.Contracts",
            "CC1055"
        )] // Skip extra error checking to avoid *potential* AppCompat problems.
        public override int Read(char[] buffer, int index, int count)
        {
            return 0;
        }

        public override string ReadLine()
        {
            return null;
        }

        public override string ReadToEnd()
        {
            return string.Empty;
        }

        internal override int ReadBuffer()
        {
            return 0;
        }
    }
}

}
