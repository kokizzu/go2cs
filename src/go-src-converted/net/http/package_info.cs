// go2cs code converter defines `global using` statements here for imported type
// aliases as package references are encountered via `import' statements. Exported
// type aliases that need a `global using` declaration will be loaded from the
// referenced package by parsing its 'package_info.cs' source file and reading its
// defined `GoTypeAlias` attributes.

// Package name separator "dot" used in imported type aliases is extended Unicode
// character '\uA4F8' which is a valid character in a C# identifier name. This is
// used to simulate Go's package level type aliases since C# does not yet support
// importing type aliases at a namespace level.

// <ImportedTypeAliases>
global using netꓸAddr = go.net_package.ΔAddr;
global using netꓸError = go.net_package.ΔError;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using runtimeꓸError = go.runtime_package.ΔError;
global using textprotoꓸError = go.net.textproto_package.ΔError;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
global using tlsꓸConnectionState = go.crypto.tls_package.ΔConnectionState;
global using urlꓸError = go.net.url_package.ΔError;
using bufio = go.bufio_package;
using httptrace = go.net.http.httptrace_package;
using tls = go.crypto.tls_package;
// </ImportedTypeAliases>

using go;
using static go.net.http_package;

// For encountered type alias declarations, e.g., `type Table = map[string]int`,
// go2cs code converter will generate a `global using` statement for the alias in
// the converted source, e.g.: `global using Table = go.map<go.@string, nint>;`.
// Although scope of `global using` is available to all files in the project, all
// converted Go code for the project targets the same package, so `global using`
// statements will effectively have package level scope.

// Additionally, `GoTypeAlias` attributes will be generated here for exported type
// aliases. This allows the type alias to be imported and used from other packages
// when referenced.

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Cookie", "ΔCookie")]
[assembly: GoTypeAlias("Handler", "ΔHandler")]
[assembly: GoTypeAlias("Header", "ΔHeader")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

// <InterfaceImplementations>
[assembly: GoImplement<Dir, FileSystem>]
[assembly: GoImplement<File, io_package.ReadSeeker>]
[assembly: GoImplement<HandlerFunc, ΔHandler>]
[assembly: GoImplement<MaxBytesError, error>(Pointer = true)]
[assembly: GoImplement<ProtocolError, error>(Pointer = true)]
[assembly: GoImplement<ResponseWriter, io_package.Writer>]
[assembly: GoImplement<ServeMux, ΔHandler>(Pointer = true)]
[assembly: GoImplement<Transport, RoundTripper>(Pointer = true)]
[assembly: GoImplement<Write_r1, io_package.Closer>(Promoted = true)]
[assembly: GoImplement<Write_r1, io_package.ReadCloser>]
[assembly: GoImplement<Write_r1, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<body, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<bodyEOFSignal, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<bodyEOFSignal, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bodyLocked, io_package.Reader>]
[assembly: GoImplement<bufioFlushWriter, io_package.Writer>]
[assembly: GoImplement<bufio_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bufio_package.Writer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<byteReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<cancelTimerBody, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<checkConnErrorWriter, io_package.Writer>]
[assembly: GoImplement<chunkWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<connReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<countingWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<dirEntryDirs, anyDirs>]
[assembly: GoImplement<errorReader, io_package.Reader>]
[assembly: GoImplement<exactSig, sniffSig>(Pointer = true)]
[assembly: GoImplement<expectContinueReader, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<fakeLocker, sync_package.Locker>]
[assembly: GoImplement<fileHandler, ΔHandler>(Pointer = true)]
[assembly: GoImplement<fileInfoDirs, anyDirs>]
[assembly: GoImplement<fileTransport, RoundTripper>]
[assembly: GoImplement<finishAsyncByteRead, io_package.Reader>]
[assembly: GoImplement<globalOptionsHandler, ΔHandler>]
[assembly: GoImplement<go.net.http.internal_package.FlushAfterChunkWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<go.net.url_package.ΔError, error>(Pointer = true)]
[assembly: GoImplement<gzipReader, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<htmlSig, sniffSig>]
[assembly: GoImplement<http2ConnectionError, error>]
[assembly: GoImplement<http2ContinuationFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2ContinuationFrame, http2headersOrContinuation>(Pointer = true)]
[assembly: GoImplement<http2DataFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2GoAwayError, error>]
[assembly: GoImplement<http2GoAwayFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2HeadersFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2HeadersFrame, http2headersOrContinuation>(Pointer = true)]
[assembly: GoImplement<http2MetaHeadersFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2PingFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2PriorityFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2PushPromiseFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2RSTStreamFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2SettingsFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2StreamError, error>]
[assembly: GoImplement<http2StreamError, http2writeFramer>]
[assembly: GoImplement<http2Transport, RoundTripper>(Pointer = true)]
[assembly: GoImplement<http2Transport, h2Transport>(Pointer = true)]
[assembly: GoImplement<http2UnknownFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2WindowUpdateFrame, http2Frame>(Pointer = true)]
[assembly: GoImplement<http2bufferedWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<http2chunkWriter, io_package.Writer>]
[assembly: GoImplement<http2clientConnPool, http2ClientConnPool>(Pointer = true)]
[assembly: GoImplement<http2clientConnPool, http2clientConnPoolIdleCloser>(Pointer = true)]
[assembly: GoImplement<http2connError, error>]
[assembly: GoImplement<http2dataBuffer, http2pipeBuffer>(Pointer = true)]
[assembly: GoImplement<http2duplicatePseudoHeaderError, error>]
[assembly: GoImplement<http2erringRoundTripper, RoundTripper>]
[assembly: GoImplement<http2flushFrameWriter, http2writeFramer>]
[assembly: GoImplement<http2goAwayFlowError, error>]
[assembly: GoImplement<http2gzipReader, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<http2handlerPanicRST, http2writeFramer>]
[assembly: GoImplement<http2headerFieldNameError, error>]
[assembly: GoImplement<http2headerFieldValueError, error>]
[assembly: GoImplement<http2httpError, error>(Pointer = true)]
[assembly: GoImplement<http2missingBody, io_package.ReadCloser>]
[assembly: GoImplement<http2noBodyReader, io_package.ReadCloser>]
[assembly: GoImplement<http2noCachedConnError, error>]
[assembly: GoImplement<http2noDialClientConnPool, http2clientConnPoolIdleCloser>]
[assembly: GoImplement<http2noDialH2RoundTripper, RoundTripper>]
[assembly: GoImplement<http2priorityWriteScheduler, http2WriteScheduler>(Pointer = true)]
[assembly: GoImplement<http2pseudoHeaderError, error>]
[assembly: GoImplement<http2randomWriteScheduler, http2WriteScheduler>(Pointer = true)]
[assembly: GoImplement<http2requestBody, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<http2responseWriter, CloseNotifier>(Pointer = true)]
[assembly: GoImplement<http2responseWriter, Flusher>(Pointer = true)]
[assembly: GoImplement<http2responseWriter, Pusher>(Pointer = true)]
[assembly: GoImplement<http2responseWriter, ResponseWriter>(Pointer = true)]
[assembly: GoImplement<http2responseWriter, http2stringWriter>(Pointer = true)]
[assembly: GoImplement<http2roundRobinWriteScheduler, http2WriteScheduler>(Pointer = true)]
[assembly: GoImplement<http2serverConn, http2writeContext>(Pointer = true)]
[assembly: GoImplement<http2sortPriorityNodeSiblings, sort_package.Interface>]
[assembly: GoImplement<http2sorter, sort_package.Interface>(Pointer = true)]
[assembly: GoImplement<http2stickyErrWriter, io_package.Writer>]
[assembly: GoImplement<http2timeTimer, http2timer>]
[assembly: GoImplement<http2transportResponseBody, io_package.ReadCloser>]
[assembly: GoImplement<http2write100ContinueHeadersFrame, http2writeFramer>]
[assembly: GoImplement<http2writeData, http2writeFramer>(Pointer = true)]
[assembly: GoImplement<http2writeGoAway, http2writeFramer>(Pointer = true)]
[assembly: GoImplement<http2writePingAck, http2writeFramer>]
[assembly: GoImplement<http2writePushPromise, http2writeFramer>(Pointer = true)]
[assembly: GoImplement<http2writeResHeaders, http2writeFramer>(Pointer = true)]
[assembly: GoImplement<http2writeSettings, http2writeFramer>]
[assembly: GoImplement<http2writeSettingsAck, http2writeFramer>]
[assembly: GoImplement<http2writeWindowUpdate, http2writeFramer>]
[assembly: GoImplement<initALPNRequest, ΔHandler>]
[assembly: GoImplement<ioFS, FileSystem>]
[assembly: GoImplement<ioFile, File>]
[assembly: GoImplement<io_package.PipeReader, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<io_package.PipeReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<io_package.PipeWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<io_package.ReadWriteCloser, io_package.ReadCloser>]
[assembly: GoImplement<loggingConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<loggingConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<maskedSig, sniffSig>(Pointer = true)]
[assembly: GoImplement<maxBytesReader, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<mp4Sig, sniffSig>]
[assembly: GoImplement<net_package.Conn, io_package.ReadWriteCloser>]
[assembly: GoImplement<net_package.Conn, io_package.ReadWriter>]
[assembly: GoImplement<net_package.Conn, io_package.Reader>]
[assembly: GoImplement<net_package.Conn, io_package.Writer>]
[assembly: GoImplement<net_package.TCPConn, closeWriter>(Pointer = true)]
[assembly: GoImplement<noBody, io_package.ReadCloser>]
[assembly: GoImplement<noBody, io_package.WriterTo>]
[assembly: GoImplement<nothingWrittenError, error>(Promoted = true)]
[assembly: GoImplement<nothingWrittenError, error>]
[assembly: GoImplement<onceCloseListener, net_package.Listener>(Pointer = true)]
[assembly: GoImplement<onceCloseListener, net_package.Listener>(Promoted = true)]
[assembly: GoImplement<os_package.File, File>(Pointer = true)]
[assembly: GoImplement<persistConn, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<persistConnWriter, io_package.ReaderFrom>(Pointer = true)]
[assembly: GoImplement<persistConnWriter, io_package.Writer>]
[assembly: GoImplement<populateResponse, ResponseWriter>(Pointer = true)]
[assembly: GoImplement<readTrackingBody, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<readTrackingBody, io_package.ReadCloser>(Promoted = true)]
[assembly: GoImplement<readWriteCloserBody, io_package.ReadWriteCloser>(Pointer = true)]
[assembly: GoImplement<readWriteCloserBody, io_package.ReadWriteCloser>(Promoted = true)]
[assembly: GoImplement<redirectHandler, ΔHandler>(Pointer = true)]
[assembly: GoImplement<requestBodyReadError, error>(Promoted = true)]
[assembly: GoImplement<requestBodyReadError, error>]
[assembly: GoImplement<response, ResponseWriter>(Pointer = true)]
[assembly: GoImplement<response, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<rᴛ1, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<rᴛ1, io_package.Reader>]
[assembly: GoImplement<rᴛ1, io_package.WriterTo>(Promoted = true)]
[assembly: GoImplement<socksAddr, net_package.ΔAddr>(Pointer = true)]
[assembly: GoImplement<socksConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<socksConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<statusError, error>]
[assembly: GoImplement<stringWriter, io_package.StringWriter>]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<sync_package.Mutex, sync_package.Locker>(Pointer = true)]
[assembly: GoImplement<textSig, sniffSig>]
[assembly: GoImplement<timeoutError, error>(Pointer = true)]
[assembly: GoImplement<timeoutHandler, ΔHandler>(Pointer = true)]
[assembly: GoImplement<timeoutWriter, Pusher>(Pointer = true)]
[assembly: GoImplement<timeoutWriter, ResponseWriter>(Pointer = true)]
[assembly: GoImplement<tlsHandshakeTimeoutError, error>]
[assembly: GoImplement<transportReadFromServerError, error>]
[assembly: GoImplement<unsupportedTEError, error>(Pointer = true)]
[assembly: GoImplement<writerOnly, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<writerOnly, io_package.Writer>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<Request, ж<Request>>(Indirect = true)]
[assembly: GoImplicitConv<bufio.Writer, ж<bufio.Writer>>(Indirect = true)]
[assembly: GoImplicitConv<http2ClientConn, ж<http2ClientConn>>(Indirect = true)]
[assembly: GoImplicitConv<http2MetaHeadersFrame, ж<http2MetaHeadersFrame>>(Indirect = true)]
[assembly: GoImplicitConv<http2Server, ж<http2Server>>(Indirect = true)]
[assembly: GoImplicitConv<http2writeData, ж<http2writeData>>]
[assembly: GoImplicitConv<http2writeResHeaders, ж<http2writeResHeaders>>(Indirect = true)]
[assembly: GoImplicitConv<httptrace.ClientTrace, ж<httptrace.ClientTrace>>]
[assembly: GoImplicitConv<tls.Conn, ж<tls.Conn>>(Indirect = true)]
// </ImplicitConversions>

namespace go.net;

[GoPackage("http")]
public static partial class http_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface anyDirs {}
    internal partial interface closeWriter {}
    internal partial interface erringRoundTripper {}
    internal partial interface h2Transport {}
    internal partial interface http2ConfigureServer_baseContexter {}
    internal partial interface http2Frame {}
    internal partial interface http2clientConnPoolIdleCloser {}
    internal partial interface http2connectionStater {}
    internal partial interface http2h1ServerKeepAlivesDisabled_I {}
    internal partial interface http2headersEnder {}
    internal partial interface http2headersOrContinuation {}
    internal partial interface http2isNoCachedConnError_type {}
    internal partial interface http2pipeBuffer {}
    internal partial interface http2streamEnder {}
    internal partial interface http2stringWriter {}
    internal partial interface http2synctestGroupInterface {}
    internal partial interface http2timerᴛ1 {}
    internal partial interface http2writeContext {}
    internal partial interface http2writeFramer {}
    internal partial interface rwUnwrapper {}
    internal partial interface setRequestCancel_canceler {}
    internal partial interface sniffSig {}
    internal partial struct body {}
    internal partial struct bodyEOFSignal {}
    internal partial struct bodyLocked {}
    internal partial struct bufioFlushWriter {}
    internal partial struct byteReader {}
    internal partial struct cancelTimerBody {}
    internal partial struct checkConnErrorWriter {}
    internal partial struct chunkWriter {}
    internal partial struct condResult {}
    internal partial struct conn {}
    internal partial struct connLRU {}
    internal partial struct connOrError {}
    internal partial struct connReader {}
    internal partial struct connectMethod {}
    internal partial struct connectMethodKey {}
    internal partial struct countingWriter {}
    internal partial struct dirEntryDirs {}
    internal partial struct entry<K, V> {}
    internal partial struct errorReader {}
    internal partial struct exactSig {}
    internal partial struct expectContinueReader {}
    internal partial struct extraHeader {}
    internal partial struct fakeLocker {}
    internal partial struct fileHandler {}
    internal partial struct fileInfoDirs {}
    internal partial struct fileTransport {}
    internal partial struct finishAsyncByteRead {}
    internal partial struct globalOptionsHandler {}
    internal partial struct gzipReader {}
    internal partial struct headerSorter {}
    internal partial struct htmlSig {}
    internal partial struct http2ConnectionError {}
    internal partial struct http2ContinuationFrame {}
    internal partial struct http2DataFrame {}
    internal partial struct http2FrameHeader {}
    internal partial struct http2Framer {}
    internal partial struct http2GoAwayError {}
    internal partial struct http2GoAwayFrame {}
    internal partial struct http2HeadersFrame {}
    internal partial struct http2HeadersFrameParam {}
    internal partial struct http2MetaHeadersFrame {}
    internal partial struct http2PingFrame {}
    internal partial struct http2PriorityFrame {}
    internal partial struct http2PriorityWriteSchedulerConfig {}
    internal partial struct http2PushPromiseFrame {}
    internal partial struct http2PushPromiseParam {}
    internal partial struct http2RSTStreamFrame {}
    internal partial struct http2RoundTripOpt {}
    internal partial struct http2ServeConnOpts {}
    internal partial struct http2Server {}
    internal partial struct http2Setting {}
    internal partial struct http2SettingsFrame {}
    internal partial struct http2StreamError {}
    internal partial struct http2Transport {}
    internal partial struct http2UnknownFrame {}
    internal partial struct http2WindowUpdateFrame {}
    internal partial struct http2addConnCall {}
    internal partial struct http2bodyReadMsg {}
    internal partial struct http2bufferedWriter {}
    internal partial struct http2chunkWriter {}
    internal partial struct http2clientConnIdleState {}
    internal partial struct http2clientConnPool {}
    internal partial struct http2clientConnReadLoop {}
    internal partial struct http2clientStream {}
    internal partial struct http2closeWaiter {}
    internal partial struct http2connError {}
    internal partial struct http2dataBuffer {}
    internal partial struct http2dialCall {}
    internal partial struct http2duplicatePseudoHeaderError {}
    internal partial struct http2erringRoundTripper {}
    internal partial struct http2errorReader {}
    internal partial struct http2flushFrameWriter {}
    internal partial struct http2frameCache {}
    internal partial struct http2frameWriteResult {}
    internal partial struct http2goAwayFlowError {}
    internal partial struct http2goroutineLock {}
    internal partial struct http2gzipReader {}
    internal partial struct http2handlerPanicRST {}
    internal partial struct http2headerFieldNameError {}
    internal partial struct http2headerFieldValueError {}
    internal partial struct http2httpError {}
    internal partial struct http2incomparable {}
    internal partial struct http2inflow {}
    internal partial struct http2missingBody {}
    internal partial struct http2noBodyReader {}
    internal partial struct http2noCachedConnError {}
    internal partial struct http2noDialClientConnPool {}
    internal partial struct http2noDialH2RoundTripper {}
    internal partial struct http2outflow {}
    internal partial struct http2pipe {}
    internal partial struct http2priorityNode {}
    internal partial struct http2priorityNodeState {}
    internal partial struct http2priorityWriteScheduler {}
    internal partial struct http2pseudoHeaderError {}
    internal partial struct http2randomWriteScheduler {}
    internal partial struct http2readFrameResult {}
    internal partial struct http2requestBody {}
    internal partial struct http2requestParam {}
    internal partial struct http2resAndError {}
    internal partial struct http2responseWriter {}
    internal partial struct http2responseWriterState {}
    internal partial struct http2roundRobinWriteScheduler {}
    internal partial struct http2serverConn {}
    internal partial struct http2serverInternalState {}
    internal partial struct http2serverMessage {}
    internal partial struct http2sortPriorityNodeSiblings {}
    internal partial struct http2sorter {}
    internal partial struct http2startPushRequest {}
    internal partial struct http2stickyErrWriter {}
    internal partial struct http2stream {}
    internal partial struct http2streamState {}
    internal partial struct http2timeTimer {}
    internal partial struct http2transportResponseBody {}
    internal partial struct http2transportTestHooks {}
    internal partial struct http2unstartedHandler {}
    internal partial struct http2write100ContinueHeadersFrame {}
    internal partial struct http2writeData {}
    internal partial struct http2writeGoAway {}
    internal partial struct http2writePingAck {}
    internal partial struct http2writePushPromise {}
    internal partial struct http2writeQueue {}
    internal partial struct http2writeQueuePool {}
    internal partial struct http2writeResHeaders {}
    internal partial struct http2writeSettings {}
    internal partial struct http2writeSettingsAck {}
    internal partial struct http2writeWindowUpdate {}
    internal partial struct httpRange {}
    internal partial struct incomparable {}
    internal partial struct initALPNRequest {}
    internal partial struct ioFS {}
    internal partial struct ioFile {}
    internal partial struct keyValues {}
    internal partial struct loggingConn {}
    internal partial struct mapping<K, V> {}
    internal partial struct maskedSig {}
    internal partial struct maxBytesReader {}
    internal partial struct mp4Sig {}
    internal partial struct muxEntry {}
    internal partial struct nothingWrittenError {}
    internal partial struct onceCloseListener {}
    internal partial struct pattern {}
    internal partial struct persistConn {}
    internal partial struct persistConnWriter {}
    internal partial struct populateResponse {}
    internal partial struct readTrackingBody {}
    internal partial struct readWriteCloserBody {}
    internal partial struct redirectHandler {}
    internal partial struct relationship {}
    internal partial struct requestAndChan {}
    internal partial struct requestBodyReadError {}
    internal partial struct response {}
    internal partial struct responseAndError {}
    internal partial struct routingIndex {}
    internal partial struct routingIndexKey {}
    internal partial struct routingNode {}
    internal partial struct rᴛ1 {}
    internal partial struct segment {}
    internal partial struct serveMux121 {}
    internal partial struct serverHandler {}
    internal partial struct socksAddr {}
    internal partial struct socksCommand {}
    internal partial struct socksConn {}
    internal partial struct socksDialer {}
    internal partial struct socksReply {}
    internal partial struct socksUsernamePassword {}
    internal partial struct statusError {}
    internal partial struct stringWriter {}
    internal partial struct tLogKey {}
    internal partial struct textSig {}
    internal partial struct timeoutError {}
    internal partial struct timeoutHandler {}
    internal partial struct timeoutWriter {}
    internal partial struct tlsHandshakeTimeoutError {}
    internal partial struct transferReader {}
    internal partial struct transferWriter {}
    internal partial struct transportReadFromServerError {}
    internal partial struct transportRequest {}
    internal partial struct unsupportedTEError {}
    internal partial struct wantConn {}
    internal partial struct wantConnQueue {}
    internal partial struct writerOnly {}
    public partial interface CloseIdleConnections_closeIdler {}
    public partial interface CloseNotifier {}
    public partial interface CookieJar {}
    public partial interface EnableFullDuplex_type {}
    public partial interface File {}
    public partial interface FileSystem {}
    public partial interface Flush_type {}
    public partial interface Flusher {}
    public partial interface Hijacker {}
    public partial interface Pusher {}
    public partial interface Read_requestTooLarger {}
    public partial interface ResponseWriter {}
    public partial interface RoundTripper {}
    public partial interface SetReadDeadline_type {}
    public partial interface SetWriteDeadline_type {}
    public partial interface http2ClientConnPool {}
    public partial interface http2WriteScheduler {}
    public partial interface ΔHandler {}
    public partial struct Client {}
    public partial struct ConnState {}
    public partial struct Dir {}
    public partial struct MaxBytesError {}
    public partial struct ProtocolError {}
    public partial struct PushOptions {}
    public partial struct Request {}
    public partial struct Response {}
    public partial struct ResponseController {}
    public partial struct SameSite {}
    public partial struct ServeMux {}
    public partial struct Server {}
    public partial struct Transport {}
    public partial struct Write_r1 {}
    public partial struct contextKey {}
    public partial struct http2ClientConn {}
    public partial struct http2ClientConnState {}
    public partial struct http2ErrCode {}
    public partial struct http2Flags {}
    public partial struct http2FrameType {}
    public partial struct http2FrameWriteRequest {}
    public partial struct http2OpenStreamOptions {}
    public partial struct http2PriorityParam {}
    public partial struct http2SettingID {}
    public partial struct noBody {}
    public partial struct readResult {}
    public partial struct socksAuthMethod {}
    public partial struct ΔCookie {}
    public partial struct ΔHeader {}
    public partial struct ΔwriteRequest {}
    // </TypeAccessibility>
}
