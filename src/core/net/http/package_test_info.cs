// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.net.http_package;
global using static global::go.net.http_internal_test_package;

// <ImportedTypeAliases>
global using execꓸError = go.os.exec_package.ΔError;
global using httpꓸCookie = go.net.http_package.ΔCookie;
global using httpꓸHandler = go.net.http_package.ΔHandler;
global using httpꓸHeader = go.net.http_package.ΔHeader;
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
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
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
global using textprotoꓸError = go.net.textproto_package.ΔError;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
global using tlsꓸConnectionState = go.crypto.tls_package.ΔConnectionState;
global using tokenꓸFile = go.go.token_package.ΔFile;
global using tokenꓸPos = go.go.token_package.ΔPos;
global using tokenꓸPosition = go.go.token_package.ΔPosition;
global using urlꓸError = go.net.url_package.ΔError;
using bufio = go.bufio_package;
using testing = go.testing_package;
using Δhttp = go.net.http_package;
// </ImportedTypeAliases>

using go;
using static global::go.net.http_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Cookie", "ΔCookie")]
[assembly: GoTypeAlias("Handler", "ΔHandler")]
[assembly: GoTypeAlias("Header", "ΔHeader")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<RecordingJar, go.net.http_package.CookieJar>(Pointer = true)]
[assembly: GoImplement<TestH12_RequestContentLength_Unknown_type, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestH12_RequestContentLength_Unknown_type, io_package.Reader>]
[assembly: GoImplement<TestJar, go.net.http_package.CookieJar>(Pointer = true)]
[assembly: GoImplement<TestNewRequestContentLength_type, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestNewRequestContentLength_type, io_package.Reader>]
[assembly: GoImplement<apiHandler, go.net.http_package.ΔHandler>]
[assembly: GoImplement<blockingRemoteAddrConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<blockingRemoteAddrListener, net_package.Listener>(Pointer = true)]
[assembly: GoImplement<bodyCloser, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<bodyLimitReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<breakableConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<bufio_package.ReadWriter, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bufio_package.ReadWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bufio_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<byteAtATimeReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<byteFromChanReader, io_package.Reader>]
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.ReadSeeker>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<cancelProto, go.net.http_package.RoundTripper>]
[assembly: GoImplement<cancelableTimeoutContext, context_package.Context>(Promoted = true)]
[assembly: GoImplement<cancelableTimeoutContext, context_package.Context>]
[assembly: GoImplement<closeWriteTestConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<closerFunc, io_package.Closer>]
[assembly: GoImplement<countCloseListener, net_package.Listener>(Pointer = true)]
[assembly: GoImplement<countCloseReader, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<countCloseReader, io_package.Reader>]
[assembly: GoImplement<countedConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<countedConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<countedContext, context_package.Context>(Pointer = true)]
[assembly: GoImplement<countedContext, context_package.Context>(Promoted = true)]
[assembly: GoImplement<delayedEOFReader, io_package.Reader>]
[assembly: GoImplement<doneContext, context_package.Context>(Promoted = true)]
[assembly: GoImplement<doneContext, context_package.Context>]
[assembly: GoImplement<dotFileHidingFile, go.net.http_package.File>(Promoted = true)]
[assembly: GoImplement<dotFileHidingFile, go.net.http_package.File>]
[assembly: GoImplement<dotFileHidingFileSystem, go.net.http_package.FileSystem>(Promoted = true)]
[assembly: GoImplement<dotFileHidingFileSystem, go.net.http_package.FileSystem>]
[assembly: GoImplement<dummyAddr, net_package.ΔAddr>]
[assembly: GoImplement<eofListenerNotComparable, net_package.Listener>]
[assembly: GoImplement<eofReaderFunc, io_package.Reader>]
[assembly: GoImplement<errorListener, net_package.Listener>(Pointer = true)]
[assembly: GoImplement<fakeFS, go.net.http_package.FileSystem>(Pointer = true)]
[assembly: GoImplement<fakeFS, go.net.http_package.FileSystem>]
[assembly: GoImplement<fakeFile, go.net.http_package.File>(Pointer = true)]
[assembly: GoImplement<fakeFile, io_package.ReadSeeker>(Promoted = true)]
[assembly: GoImplement<fakeFileInfo, go.io.fs_package.FileInfo>(Pointer = true)]
[assembly: GoImplement<fileServerCleanPathDir, go.net.http_package.FileSystem>]
[assembly: GoImplement<fooProto, go.net.http_package.RoundTripper>]
[assembly: GoImplement<funcConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<funcConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<funcConn, net_package.Conn>]
[assembly: GoImplement<funcRoundTripper, go.net.http_package.RoundTripper>]
[assembly: GoImplement<funcWriter, io_package.Writer>]
[assembly: GoImplement<global::go.net.http_package.Dir, global::go.net.http_package.FileSystem>]
[assembly: GoImplement<global::go.net.http_package.HandlerFunc, global::go.net.http_package.ΔHandler>]
[assembly: GoImplement<global::go.net.http_package.ResponseWriter, io_package.Writer>]
[assembly: GoImplement<global::go.net.http_package.http2StreamError, error>]
[assembly: GoImplement<global::go.net.http_package.http2noCachedConnError, error>]
[assembly: GoImplement<global::go.net.http_package.noBody, io_package.ReadCloser>]
[assembly: GoImplement<global::go.net.http_package.nothingWrittenError, error>]
[assembly: GoImplement<global::go.net.http_package.transportReadFromServerError, error>]
[assembly: GoImplement<go.compress.gzip_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<go.compress.gzip_package.Writer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<go.crypto.tls_package.Conn, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<go.crypto.tls_package.Conn, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<go.net.http.cookiejar_package.Jar, go.net.http_package.CookieJar>(Pointer = true)]
[assembly: GoImplement<go.net.http.httptest_package.ResponseRecorder, go.net.http_package.ResponseWriter>(Pointer = true)]
[assembly: GoImplement<go.net.http_package.noBody, io_package.Reader>]
[assembly: GoImplement<go.net.http_test_package.delegateReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<go.net.http_test_package.dumpConn, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<go.net.http_test_package.dumpConn, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<go.net.http_test_package.dumpConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<go.net.http_test_package.roundTripFunc, go.net.http_package.RoundTripper>]
[assembly: GoImplement<go.testing_package.B, TBRun<go.testing_package.B>>(ConstraintProxy = true)]
[assembly: GoImplement<go.testing_package.T, TBRun<go.testing_package.T>>(ConstraintProxy = true)]
[assembly: GoImplement<gzipResponseWriter, go.net.http_package.ResponseWriter>(Promoted = true)]
[assembly: GoImplement<gzipResponseWriter, go.net.http_package.ResponseWriter>]
[assembly: GoImplement<http09Writer, go.net.http_package.ResponseWriter>(Pointer = true)]
[assembly: GoImplement<http09Writer, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<infiniteReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<io_package.ReadCloser, io_package.Reader>]
[assembly: GoImplement<io_package.WriteCloser, io_package.Writer>]
[assembly: GoImplement<issue12991FS, go.net.http_package.FileSystem>]
[assembly: GoImplement<issue12991File, go.net.http_package.File>(Promoted = true)]
[assembly: GoImplement<issue12991File, go.net.http_package.File>]
[assembly: GoImplement<issue15577Tripper, go.net.http_package.RoundTripper>]
[assembly: GoImplement<issue18239Body, io_package.Reader>]
[assembly: GoImplement<issue40382Body, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<lockedBytesBuffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<logWrites, io_package.Writer>]
[assembly: GoImplement<logWritesConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<net_package.Conn, io_package.Reader>]
[assembly: GoImplement<net_package.Conn, io_package.Writer>]
[assembly: GoImplement<neverEnding, io_package.Reader>]
[assembly: GoImplement<nilBodyRoundTripper, go.net.http_package.RoundTripper>]
[assembly: GoImplement<noteCloseConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<noteCloseConn, net_package.Conn>]
[assembly: GoImplement<oneConnListener, net_package.Listener>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.ReadSeeker>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<panicOnSeek, io_package.ReadSeeker>(Promoted = true)]
[assembly: GoImplement<panicOnSeek, io_package.ReadSeeker>]
[assembly: GoImplement<recordingTransport, go.net.http_package.RoundTripper>(Pointer = true)]
[assembly: GoImplement<repeatReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<responseWriterJustWriter, go.net.http_package.ResponseWriter>]
[assembly: GoImplement<responseWriterJustWriter, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<roundTripperWithCloseIdle, go.net.http_package.RoundTripper>]
[assembly: GoImplement<roundTripperWithoutCloseIdle, go.net.http_package.RoundTripper>]
[assembly: GoImplement<rwTestConn, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<rwTestConn, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<rwTestConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<slowTestConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<slurpResult, io_package.ReadCloser>(Promoted = true)]
[assembly: GoImplement<slurpResult, io_package.ReadCloser>]
[assembly: GoImplement<stringHandler, go.net.http_package.ΔHandler>]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.ReadSeeker>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.ReaderAt>(Pointer = true)]
[assembly: GoImplement<terrorWriter, io_package.Writer>]
[assembly: GoImplement<testCloseConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<testConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<testContentTypeWithVariousSources_readerOnly, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<testContentTypeWithVariousSources_readerOnly, io_package.Reader>]
[assembly: GoImplement<testErrorReader, io_package.Reader>]
[assembly: GoImplement<testFileSystem, go.net.http_package.FileSystem>(Pointer = true)]
[assembly: GoImplement<testLogWriter, io_package.Writer>]
[assembly: GoImplement<testMockTCPConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<testRoundTripper, go.net.http_package.RoundTripper>]
[assembly: GoImplement<testServerExpect_type, io_package.Closer>(Promoted = true)]
[assembly: GoImplement<testServerExpect_type, io_package.WriteCloser>]
[assembly: GoImplement<testServerExpect_type, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<testTransportClosesBodyOnError_body, io_package.Closer>(Promoted = true)]
[assembly: GoImplement<testTransportClosesBodyOnError_body, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<testTransportClosesBodyOnError_body, io_package.Reader>]
[assembly: GoImplement<testing_package.B, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<tlogWriter, io_package.Writer>]
[assembly: GoImplement<trackLastConnListener, net_package.Listener>(Promoted = true)]
[assembly: GoImplement<trackLastConnListener, net_package.Listener>]
[assembly: GoImplement<transportDialTesterConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<transportDialTesterConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<wgReadCloser, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<wgReadCloser, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<wrapWriter, go.net.http_package.ResponseWriter>(Promoted = true)]
[assembly: GoImplement<wrapWriter, go.net.http_package.ResponseWriter>]
[assembly: GoImplement<writeCountingConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<writerFuncConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<writerFuncConn, net_package.Conn>(Promoted = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<closeWriteTestConn, ж<closeWriteTestConn>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.net.http_package.Request, ж<global::go.net.http_package.Request>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.net.http_package.Response, ж<global::go.net.http_package.Response>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.net.http_package.routingIndex, ж<global::go.net.http_package.routingIndex>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: global::go.GoPositionMap("net/http/alpn_test.go", "alpn_test.cs", "AFooooKCgoKClIKUgqampoKmgoKCgpSCgpSAgtyCgoLckqaCgpKCgsyCgoKCgoKUgoKClICCAA0OooKCgpSCgoKUgoKCgoIABxCAog==", "23-34:1")]
[assembly: global::go.GoPositionMap("net/http/client_test.go", "client_test.cs", "AH1GgqrCgoKCgoKUgoKCgpSUgsqA0oKEgoKCgoKUgqKCyIDigoKCgpSAggALEKKC1sKCgoKCgoKClIKUggAMCMKCgoSCgoKEgpSClIKUgpSAggARCMKCgoSCgoKCgoSClIKUgpSAkqSCpoKCgIKkgoKUgIL4gAAMAoKCgpSCgJK2goKUloKCgJK4goCSuIKCgJK4goKAkqaCgoKCgoKUgoKUgoKAkqSClICSuIKCgoCCpIKUgpaCgoCCpIKUgoK6kNKCgpaCgpKCtKS2goKCgpSCAAkUggALGAAXMIKi6IIACBQAFSyCogANCIKIgoKCgoKAgqSCgoKAgoKAgoKCgraCgpS4goKCgoKEgpSCpoKChIKEgoK4ooKCgpSCgpSCgriA8qKCgoKClIKCqIKCgpSUgoKUgpSSgoKUguyg4oKCgoKCgpSCgpSCgpSAgu6Q0qKCgoKUgoKUgoKCgpSCgpSAggALFoKUgoKUgvjCgoKCgoKChIKEgoSChIKEgoKEgoIADBTSgoKClNbSgoLWgKKCgoKCgoKCgoKUgqaCgoKUgoKCgoKmgsqAAA8CooKCgpSCgqaCgoKWgoKUgoKUgoyCAAoWgqaCgqbCgoLWgKKCgpKCgoKogoKClIKCgoKClIKUgoKmgoKCAAgSgoKqoKKClpKCgoKUlIKEgoKUgpaCgoKUgriC1IKClIKCuoKypoKClIKogoIACAiigoKCgoKUguiCpIKCgoKogoKAgsiC1IKSgoKEgoKCgpSCloKCAAMaAAkC5IKCloKCgpaCgpSmguSigpaCgoKCloKClJKClICC/rL0gpKEgoCCypDygoKAgrYABBCCgoKClIKUgoKUgsqA8qKCgoKCgoKClIKCgqamkoKClIKCgoKUAAoGooKChIKChIKUgpSClIKCgoKClIKCpgALCKKCgpaCgoKUgoKEgpSClIKUgoKCgoKUgoKmAAwKooIAFTSyooKC7oAACgKiqoKCgoKUgoKClJSEgoKCAAgUgoKCppSAgqSChIKCgoKCgoKUgpSClJaCgoKCloKEgpSCgqSUgpSAgoKUprqQ8sKCkgAIEpCUgoKCgpSAgqSCgpSClIKUgIKClPygoqKChJKClISCgoKCgpSCgoLqkKKCgpaCgoKClICCpICCyIAACAKCgpKCgqiCgpSCgoKWgtamgtamgt6CgvaiABM4goKClIKClIKCAAkSgsrYssquwAALAqKYgoIABxCClIKUppKUhIKC7oKUloKCgoKCgoKUkoKUgIL8ovSigoKCgIKigvyUpKSkpLaEgpiCgoKCgoKUkoLqkAAIArSCgoKUkoKChpKCgoKWkoKClIKmkoKClIKkgoKClIKkgraCgoKEgoKCgoKUkoKUgIL6kNLCgoKClJaSgoSClO6CgqTcgoKCpNyCgqQACBKCtoKogoKEgoKCgoKCgoKCgoKUkoIADAqSABxGgoKCgpSCgoKUgoLcgAAIAoIAH1SEkoKWgrKigpaCgoKWooCSpKaWgoKClgAMGoKCpoKCqqDiooKClIKClIK6goKUhIKCgoSCgpSCgoLMgoKClILsgMiAooDUooKEgoKUgoKC3IL2gqaCgoKUlKaqoNKigpaEgrKCgoKUgpSUgoKCloSCgoKClICCpICCpICCAAwQggAKEKKEgoKWgpSSgIK4gIKk+pCigoK6goKCgpSCgpQADBqClKSCpIKUggAICKKClKaAooKCkoKAgriCgoKCwoKCgoKClMbouJKCgoKC", "192-205:1;236-240:2;288-290:1;294-302:2;358-360:1;397-399:1;408-433:1;439-439:2;482-490:1;493-498:2;521-538:1;523-526:1.1;546-549:1;677-686:1;690-692:2;738-744:1;787-788:1;791-797:2;823-825:1;871-875:1;888-888:1;922-924:1;929-931:2;943-945:1;951-953:2;974-974:1;986-990:1;1021-1037:1;1156-1162:1;1173-1193:1;1270-1272:1;1280-1280:2;1316-1319:1;1339-1341:1;1360-1365:1;1465-1465:1;1483-1500:1;1501-1503:2;1507-1519:3;1546-1569:1;1573-1575:2;1595-1599:1;1609-1641:2;1666-1672:1;1674-1729:2;1856-1859:1;1863-1866:2;1874-1882:3;1878-1880:3.1;1917-1924:1;1979-1981:1;1998-2007:1;2016-2018:1;2023-2057:2;2083-2087:1;2099-2101:1;2154-2159:1;2165-2174:2")]
[assembly: global::go.GoPositionMap("net/http/clientserver_test.go", "clientserver_test.cs", "AHOAAQALAoKCgoKUtMS2gIKkgoKCgIKkgpQADRyCgqbCgoKUkoKClNaCgpTIpoKCAAMeAA0CgpTKhIKClLTEuIKWlKSkgoKkpIKCgoCCtoKUkpTugoKokgAMBIKKooKCgsSCgIKkgoKUgqSCpIKkgpSCuIDSooKCgoKCloKClJKAgqSCgpSClICCABMggoKUpsKCgpKClIKCgpSCgoKWgIKCpoKChIKCgqaClICCgoL4opKCgoKCAAgSgNSigpSUhILKgpSClIK6kgAJDoKmyoKmgoLcgraAooCigKSCgviCgsiCgoLIgoKCyIKCgoLIgqaClMqCpoKUyoKUgqaCgoKUgpSCAAUY4KKigpKCgoKCgoKCpoKogoKUlIKCAAwMopSAgqSCgoLKgqS2goCCAAYSsKKigoKCgqaSgoKUgpSCgpSCAAkIgpSC+oKmggAJBoLWgpSCvICSAAYQoOKigoKigoKCgpSEgoKEgoKUkqiCgoKUhISCgoKUguqQAAgCgoKCgpSEgoKUgpSClN6ClLiW2IKCgpSAgsqSkraCkgANCIKCgoKEgoLegoKWgoKWpoLcgpSCloKCloDKpoCCpoDKypCigoKClIKClIKCgriA4sKCgoKCgpSygoKClIKU1rKCypTElIKCgoKUgpKClILogPKCgpKWgoKWABIugoKCgpSCgoKUgpSCyoAADAKCgpaCgpSWAAcS2NjYyIKCgoKCgpSCgoKClILKgoKCktqCgpKCgpaCgpSEzIKClISCgoKClIKUgpSAgqTWpIKClJSClIKUgrqSpKKClJSUgraCgoKUgoCSpoCCpJSEgoKCgoLSgoKmgoKCgqaSgoKU1oKEgoKCgpSCqJKCgoKUlJSUAAkIkpKAkoD2goKCgqiCooKCkJKCgpSAgqSAgraCtKTagPKCgpSEABMcgoKSgpSCgoKCgoKUgqTYgqQACwqCkoCSgICSgPbChIKEgoKygoQACAiUlIKClIKSgoKUgpSigoLEhJKCgoKUlIKClJSCgpSUAAsSwoKCAAkIkpSCgoKmgpSCgoKUgpSC3pDiooKUkoKClIKCgpSUgoKCggAIEoKCAAkKgoKmgKLCgqiEgoKUgoKCgpSSguiAAAkCgoKCgoKClIKClICCpIKChICCpICCyIL0gqKCgpSCgpSSxpKCgoKUgriA4qKCsoKSgIKCgoKUurbEgoKUgpSCvKKStoL0woKigoKSgoKClIKCgsSUgoKUkoKClICSuKaUgoKClILoguTCgoCCuIKClIKCkpaCgoKCooKCkoKUpoKClJKCgoKUgpS0pLSC6AANDJKUgqaCgoKmgpTKgOKigoKCgJKkgoKClIKCgpSSgoKUgJL4gAAJAqKCgoKEgoKChISChJaChIKWgoK6goSCgILKgrSUgoS0gra2hKaEgoKUlIKAgqaCgg==", "82-91:1;87-89:1.1;134-136:1;199-201:1;224-229:1;259-263:1;393-394:1;401-403:1;410-415:1;420-420:1;428-430:1;434-436:1;440-443:1;447-450:1;454-458:1;464-469:1;476-481:1;487-490:1;491-503:2;516-530:1;548-556:1;563-563:1;565-570:2;579-585:1;605-608:1;613-613:1;617-617:1;621-621:1;626-629:1;630-632:2;633-637:3;647-653:1;691-713:1;717-719:2;721-723:3;741-743:1;746-748:1;753-769:1;823-825:1;841-870:1;846-856:1.1;858-868:1.2;890-892:1;944-946:1;948-953:2;960-960:3;964-964:4;968-968:5;972-972:6;976-976:7;1002-1006:1;1003-1005:1.1;1011-1015:1;1077-1079:1;1087-1096:2;1094-1094:2.1;1110-1129:3;1162-1165:1;1163-1163:1.1;1164-1164:1.2;1168-1173:1;1176-1190:2;1179-1179:2.1;1203-1205:1;1224-1227:2;1252-1256:1;1253-1253:1.1;1254-1254:1.2;1255-1255:1.3;1266-1275:1;1275-1277:2;1291-1295:3;1298-1319:4;1336-1341:1;1342-1357:2;1364-1366:1;1367-1378:2;1406-1408:1;1431-1437:1;1461-1472:1;1488-1506:1;1490-1504:1.1;1522-1524:1;1531-1544:1;1544-1546:2;1580-1584:1;1592-1594:2;1600-1609:3;1642-1645:1;1646-1651:2;1652-1657:3;1664-1673:1;1693-1707:1;1709-1721:2;1723-1731:3;1735-1753:4")]
[assembly: global::go.GoPositionMap("net/http/example_filesystem_test.go", "example_filesystem_test.cs", "ABUisoKCgqYACRjSgpKCpgAIGLKSloKClKaCgoI=")]
[assembly: global::go.GoPositionMap("net/http/example_test.go", "example_test.cs", "AB8igqKCgoKUgoKCppKCgoKCgpSCAAkIgoKClIKCgpSClNaU5rimuNrUgoKCpoKClAAPDKKCyoKEgoSCgoK4goSCkoKCloCUpJaAlKb2goKogoLmloKWgvaCgpSCloKE1oKC+IKWloQ=", "18-40:1;85-93:1;100-115:1;122-133:1;144-146:1;157-159:1;166-168:1;169-171:2;180-182:1")]
[assembly: global::go.GoPositionMap("net/http/fs_test.go", "fs_test.cs", "AH6WAYAACwKCgpSEhIKCmJKCgILuAAgSgoKCuoKCgpSAkriCgoKClIKClIKUgoKClIKClIKCgoKClIKmgoKCgpSCgpSCgpSAgoKkgoKCgoKUgoCSpIKCgpSCgqaCggAMDIIACx6CgoKClIKCgvySgoKCgoKC6pK4goKCgoKClIKCAA4agNKChIKCgpSCgJIACRKCAAoGooKCkoKkzoKCgoKCgIIACAqA4oKCggAHFpKCgsqWgoKCgoKUgoKUgoKUgIKkuIDSooKSggAQJoSCgpSUgoKUgoLogoKCuIAACQKCgoCCpIKSgoKUgoKUgpSAgqSAggAOCIKClIKClLKCgpSSgoKUgtaCgoKCgqiCgtaCooKCgpTUgoKmgNKCgoKUprSUkoKClICCpJSCgqaA4oKClIKClIKCgILIgKKCgpSCgpSCgrqQ0oKCgpSCgpSCgILMoKKigoIABxKUgoKUgoCCzKDyooKCgoIABxKUgoKUgoKClIKCgpSClICStoCCpIKUgriA4oKCgoKUgrSCtIKChIKCgpSCgpSAgqTcgKKigoKUgoKClIKClICCpOiA0qKEgoKUkoKClIKCgoKUguiAoqKCyIKClIKCAA0egKKAooCigKKAooKClKaCAAkUgKKAooKClISCgpSCloKU3IKCgoKUgpSmgNKCgpKCksoABxKEgoKUgoKUgpSEgoKWgoSCgoKUgpSWhIKClIKUpoKCgpSmgAAdAqIAABCSgoKUgpSWAAAeAN0BrAOCgoKCgpSSlJSUgIKm7oKClIKWgoKClIKCgpSAkqSAkqSAkgAODpKCgoKCgoKAgvyA6ICigOSCkoKUggAOCIKClAAHEIKSgoKCgpSCgoKCgoKUgoKUgoCCgoKUAA4OsoKCgpSAgqaCgpSCgpSmgIKmgoSAgqSEgoKCgoKCgIKmgoKUgoKUloKEgoKC6KKCgpSCgpT6woKUgoKCgpSCgoKUgoKC6pKSgpSCAAwKgoSSgpSCgpaCgoKClIKogoKUgsyCgpaCAAgGggAEEoKCgoKCgpSCAAgSgoKUlAAMCoIACRyCgoLOogAIBIKEAAkegpKigoKUgoKClICSpIKCgpSAkuyCpIKEgoLeAAkUgoKCgpSCgoKUgoKUgpSAkgAICqKCgqaClIKClIKClICCpNaigoKmkpSUgoKUgoKUgIKk1gARHIKCprKCgpLElIKClIKClICCpAAKEIKmgoKAgsqQ4oKigpSCgpSAkqTGkpaSuIKClIIADgaigpSCkoKCgoKCgoKUlIKClISCgoKWgoSCgpSUgJKkgJKkgJKkgJKkgJKkgJKkgJKkgJKkgJKkgJI=", "77-79:1;305-308:1;431-442:1;459-472:1;488-495:1;504-513:1;514-523:2;531-533:1;547-549:1;564-566:1;581-593:1;608-622:1;668-686:1;908-917:1;1227-1234:1;1228-1230:1.1;1231-1233:1.2;1249-1255:1;1371-1373:1;1383-1390:1;1384-1386:1.1;1385-1385:1.1.1;1387-1389:1.2;1388-1388:1.2.1;1405-1425:1;1406-1424:1.1;1523-1544:1;1626-1628:1;1664-1669:1;1705-1717:1;1719-1721:2;1723-1725:3;1729-1731:1;1732-1734:2;1741-1750:1;1768-1773:2")]
[assembly: global::go.GoPositionMap("net/http/main_test.go", "main_test.cs", "ABYsgoKCgpQADgaigoKCgoIAARiUlIKokqaWgoKCgoKCgpSCppSCgpTcsoKUguiCgoKUgqbKgoKClNwABxKEggAHEIKCgoKCgqaCgriUqqKCgoKCgoI=")]
[assembly: global::go.GoPositionMap("net/http/request_test.go", "request_test.cs", "ADE8goKCgIL8ooKUgoKUgoIADQqClISAgqSAgqSAgqSAgqSAgqSAgqSAgqSAgqSAgqSAgvqSgpSCgoKUgIIACwqCAAoYpMqClLS0AAoOgoK4goKClIIACQqCAAQSgsqCgoKUlIIAGAySAAAizN6CgpaCgpbugpa4gviCyoKCloKCggAOCpKOyoKClIK+stSCgriAgoK2goKCgpSAgqSAgqSCgpSCgoKUgoCSyIDigoKUgqSkuIKCgpSCgoIACQiCgoKAkgAICNiCgIKkkoKAgqQACArCgoKAgqSS1saCkoKm1qaCAAgIkoKCgpSCguqSgoKUgoLsooKAgqSAgvzCgoCCpJKAggAKDKKCgIKkgIIANGiCgoKCgpaClJaCABUsgoKCgoKUggALCoKCgpSCgpSCgoKWgoKkABAIgoKCgpQADCKCgoKUggAjPIKCgoIAGDaCgoKCgoLKgoKClIKCABcwgoKCgoKCAAoWgoKmgoKmgpKCgtyC+IKSgoKUgoKC3ILogpKCgpSCgtyCAAgIooKClIKUggAHEpKEgoCCpoKUgoK4goKCgoKCggAHEICigAAIELKCgpSmgoK4ggAIDKKCgoKCgoKCgpSCgpSClIKmAAQQgoKAggAMDsKCAEWSAYKEhIKWggAICoKCgqiCgoIACQyigoKolISClIL8ooKEgqiAkqSAkriAkqSAksqQooKUgoKUgriAggALCpLOgoKCgpSCgpSCgpSCgpSCgpSCgpSC+oKCgpSClIL4goKCgpSCggALBqKAkqSAkqSAgqaCgIK2gpKCgpKClICCuAAIBoKCgpSClIKCgpSAgqQACAqiAAoegoKUgoKCqIKUgpSCAAoKggAoXLKSgoKUgpaEgoKClAANSKKCgoKCgoKCgoIACxiCgoIAEAaUAAsYlAALDpQADQ6UAAkSlKqCpoKmgtaigoKWkoKWgIKmouiiooKCgpaC2ISClIKAgqaCgoKWlIKCgpaC6IKCuISSgJKkgoCSAAoIogAmUIKSgoKCpoKmgpKCgpQACQiigoLKgpSCgILIgpKCgpQAEwa0koKCgoKCgoKUAA8kgoKUgoKUgoCSpICS", "122-137:1;299-307:1;337-347:1;401-405:1;600-604:1;893-913:1;1087-1087:1;1179-1183:1;1308-1326:1;1320-1323:1.1;1456-1459:1;1465-1467:2;1471-1481:1;1572-1582:1;1595-1609:1;1621-1621:1")]
[assembly: global::go.GoPositionMap("net/http/responsecontroller_test.go", "responsecontroller_test.cs", "ABUkgOKigpKCgoCCgqSCloKClJSCgoKCloKC6ICigoKCgoKCgoKUgpSCgpSUgoKUgJLIgqSigoKCgIKkgIKmgoCC7ICCpIKAgriCgpSSgoLogqTCgoKigoKAgqSCgIKkgpaCgoKUkoKClIKC6ILUooKCwoKCgoKCgoKUgIKCpIKClLiAgoKkgoLYgoKC0oKSgubWgrbEgoKC+ILkooKCgoCCpIKClJSCgoKUkoKClAAJDoKmgKKigoKCgIKkgIKkgIK2goKUguaCpKKCgoDKgraCgoKCgoKUgqaCgoKUkoKAgqSCgIKkgqbWgoKCgIKklIKClA==", "21-30:1;55-70:1;84-109:1;128-140:1;164-194:1;199-214:2;227-237:1;261-273:1;286-308:1;331-337:1")]
[assembly: global::go.GoPositionMap("net/http/serve_test.go", "serve_test.cs", "AGBmsoKCgpSCgqaC1oKmgqaCAAgKgNKAooCigKKAAAoWgoKU+AAKFoKmwoKC1oKmgviqogAHEIKmooKCyoK4koLWooKCgoIAARCCgoKCkoKWspaCgoKUgqiCgpSCqICCAAkMggAgOoCiooKClISCgpSSgoKCgoCCgqSAgoKkgoKClJSCgraCgrYAFSKSgr7SgoKCgoKUAChQgoKCgpaC7oKCgoL8soKCgIK2ggATIqKCgoKWgoKCgoKClMqCgoKCgpSUgoKUgpSC/JKCgoKCgpSChISAkoKmgJKCAAUSwgAPBIKCloKCgoKCloQADCiCgoKClIKCgoCStoCSABUKgoSCgoKCgoKEAA4sgoKChICSpoKAkraAkgAGErKCgpSCgoKAkv6ygoKUgoKCgJLIgKKCgoK2gKKAAAgCogABEIKCgpLugoKWgoKCgoKCgoKCgu6AooIADBbCgoKUgpSSloKCgpSCgoK6goKClIKCgoKClIKCzIKClIKCgoKWgoKClJKCgoKClKbWgNLCgpKCgoKUlIKCloKCgpSWgoKCgoKUkoKClILogOLCgoKCgoKCpoCCgqSCgpSUgpaCgoKUpoKChIKC+oCiwpKCooKClIIAASgAEQKCgpSWgpSCgpSSgoKUpIKClKaCAAgKkKLCgoKClIKWgoKUkoKCuILqktSCgpSkmISCgoKWgoKUguyigoKCgpSCgqaokoKUgpLsooKi2taUkoSEgoKUgoKUhIKClIKCgpSCgoKm2LKClIKCkuyigpLa1pKEhIKCgpSCgpSU3LDigpiCwoKCgoKUgoKCgpSCgsSWhKKCgpSCgpSCloCCAAsYsoKCgoKUqJAACAKigpaCgoKUgoLGgrTIgsyCgoKClICCpICSpICStqiCgoKUhIK6goKUgoKogoKCAAoKooKEgoKUlIKCloKCgpaCgpaC6KKCgoKClJKCgoCCpIKClICCpAAJCpKC6pL+woLogoLogrygooCmktaC1oLMkAAJAoKCgpSIgoKClICCpIKUlIK4gKKCgpaCgpSCgpSCggAIEoKCgpS4ggAHEIKokuSCgpKUzJSEwoKCgoKUkoKCgoKU2IKmloKSlqiCgJK4qIKAksygooKCgoKogoKmgoKUgpSAgqSAgqSCgpSCuILUgoK0gpaEgoKUgoKClISCgILIgOKigoKCgrgABhCCgpSUgoKUgoKCgpSCgpSSgoKUguiilIKEgoKUqIKSlIKSlJLKgrCStNi4gpSSgJKkgIL6ktSigpSClIKClJKCgoKUgoLqkqaCpoLWooKCgoKCgoCCpIKC6MKCgoKCpqaAgqSCguiCgoKUyoKSgpTOgoKClNzeopSCgoKCgoKCgoKSgoKCkpS4gpCStIK0goLWgpSSuIKUkoCSpICCAA8YggAhUKAACwKiuIKClKiygoKUuISSgoTChIKClJqCgpSCvoKUgoKClIKmgpTogoKC3IKUlILYgrzCgoKCgpqEhLKCksaCwoKAgqSCgoCSpICC5tyygoKUgoKagoSCkoKUgoKCpoSAggAPGoKClABUsAGCgoKUguiCgoKCioKCgpSclIKUhLKCksaCgpKSgoKCgqaCgpSCgpSCAA4aovaigoKyggABEoKCgpKCgpSUgoL6ooKCsoIAABqCgoKSgoKUlIKCABMkgoKCpsKCgoLWwoKCgtbigoKCgpSClpSmgIKCgraCgraEkpTYptaC+KaCgpSmooKUgrIACRyEgrSCgpS4goSCAAwUgoKUpoDigoKCooKClIKCloKCgpSAkqSCgJKkgIK4hIKClICSpIKClICSyoKAgsqQooKCgoKUgoKohISCgoKCgpSCgoLSgpCSgoKC6KqgooKCloSCgoKCloKCgoLSgpCSgqaClJLWqJCigoKCooKCgpSCgpaCgoKUgJKkgoCSpICCuISCgpSAkqSCgrqCgILKktSigpSClIKClLqCgoKUkoLogKKigpKCuIKCgpSUlIKCgoKUgoKUgJKkgoCSpICC+pCioqaChISCgpSSggAJCpKGorjGgv6CggALCrKEABc8goKCgJKkgIIAEA6iAAkmgoKCgpSCgJKkgJKkgoKClICSAAUW4KSCgoKClIKUloKClISCgoKCqIKCgpSCyoKSuIKSuLSSuAAMEoKUsoKCgpSkxIKUkqiCooKCgoKUloKCloKWAAkKgoKqotSCgsKCgoKCgpSSgoLWloKCgpSCpoKSuIKS6IKCgpSCgpSCgILIgAANAoKCgpSEhAAHFLKygoKUgoKClJSClICSpICS7pKCgoKCuICiwoKUgoKClIKCpMqC7oKUggAJDoKClAANFMKCgqTGgpSCgpTWwoKCgtaAoqKCgoKCgpSClIKClIKouAAJFoKCuISCzqDSgoKU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", "186-189:1;191-193:2;311-313:1;394-398:1;493-495:1;501-503:2;605-607:1;621-623:1;636-636:1;691-693:1;698-700:1;700-703:2;774-780:1;780-784:2;787-792:3;816-831:1;831-834:2;859-863:1;863-866:2;886-891:3;923-926:1;926-929:2;955-955:1;956-958:2;1001-1005:1;1002-1004:1.1;1010-1018:1;1018-1020:2;1061-1065:1;1062-1064:1.1;1070-1078:1;1108-1124:1;1124-1126:2;1130-1138:3;1173-1188:1;1305-1307:1;1312-1314:1;1320-1322:1;1326-1328:1;1332-1335:1;1359-1362:1;1387-1389:1;1438-1440:1;1440-1445:2;1451-1466:3;1505-1516:1;1544-1544:1;1545-1548:2;1571-1578:1;1578-1580:2;1636-1638:1;1639-1639:2;1646-1646:3;1674-1676:1;1676-1679:2;1761-1763:1;1779-1781:1;1801-1803:1;1809-1809:2;1891-1901:1;1903-1978:2;1918-1960:2.1;2001-2005:1;2008-2021:2;2044-2053:1;2199-2203:1;2208-2215:2;2259-2265:1;2294-2300:1;2422-2428:1;2456-2460:1;2511-2520:1;2536-2544:2;2538-2538:2.1;2553-2555:1;2570-2582:2;2572-2572:2.1;2592-2597:1;2650-2652:1;2676-2690:1;2716-2718:1;2736-2738:1;2739-2741:2;2862-2871:1;2899-2901:1;2905-2907:1;2912-2914:1;2929-2938:1;2942-2944:2;2948-2956:3;2984-2997:1;2997-2999:2;3011-3013:1;3017-3019:1;3023-3026:1;3039-3042:1;3058-3079:2;3095-3097:1;3173-3189:1;3227-3227:1;3254-3259:1;3285-3336:1;3295-3297:1.1;3308-3311:1.2;3312-3318:1.3;3341-3345:1;3365-3365:1;3397-3402:1;3408-3416:2;3439-3448:1;3455-3464:2;3494-3499:1;3522-3545:1;3568-3591:1;3613-3615:1;3665-3667:1;3667-3669:2;3704-3706:1;3707-3715:2;3719-3724:3;3725-3736:4;3740-3743:5;3744-3749:6;3753-3757:7;3758-3766:8;3770-3775:9;3776-3787:10;3791-3794:11;3795-3800:12;3804-3807:13;3808-3813:14;3817-3818:15;3819-3824:16;3828-3830:17;3831-3836:18;3840-3843:19;3844-3852:20;3895-3895:1;3913-3926:1;3919-3925:1.1;3944-3955:1;3972-3972:1;4024-4043:1;4027-4030:1.1;4066-4074:1;4097-4101:1;4134-4204:1;4141-4156:1.1;4159-4162:1.2;4165-4194:1.3;4181-4181:1.3.1;4221-4227:1;4222-4225:1.1;4231-4246:2;4260-4263:1;4291-4293:1;4294-4297:2;4298-4302:3;4303-4308:4;4323-4338:5;4340-4342:6;4342-4364:7;4344-4363:7.1;4365-4368:8;4372-4392:9;4394-4397:10;4399-4402:11;4404-4406:12;4408-4410:13;4412-4418:14;4420-4430:15;4432-4448:16;4455-4456:1;4456-4458:2;4473-4475:1;4480-4499:2;4510-4514:1;4515-4515:2;4559-4570:1;4600-4604:1;4604-4606:2;4609-4618:3;4649-4652:1;4695-4697:1;4724-4728:1;4759-4765:1;4788-4791:1;4802-4805:1;4806-4817:2;4879-4879:1;4899-4921:1;4983-4983:1;5003-5011:1;5035-5039:1;5055-5061:1;5074-5076:1;5094-5097:1;5109-5114:1;5130-5132:1;5157-5161:1;5158-5160:1.1;5167-5169:1;5172-5191:2;5232-5235:1;5280-5287:1;5306-5309:2;5310-5313:3;5364-5367:1;5418-5422:1;5447-5450:1;5466-5470:1;5475-5478:1;5483-5486:1;5491-5493:1;5507-5510:1;5524-5530:1;5549-5552:1;5576-5576:1;5577-5577:2;5591-5654:1;5592-5595:1.1;5595-5598:1.2;5605-5619:1.3;5676-5678:1;5683-5683:2;5703-5711:3;5726-5760:1;5728-5734:1.1;5730-5732:1.1.1;5762-5764:2;5763-5763:2.1;5786-5788:1;5788-5794:2;5789-5793:2.1;5811-5813:3;5815-5818:4;5862-5862:1;5867-5875:2;5879-5882:3;5913-5951:1;5914-5921:1.1;5921-5924:1.2;5929-5934:1.3;5967-6005:1;5968-5971:1.1;6047-6072:1;6057-6060:1.1;6091-6114:1;6147-6171:1;6255-6258:1;6289-6291:1;6292-6294:2;6312-6314:1;6314-6327:2;6315-6320:2.1;6321-6326:2.2;6348-6350:1;6350-6357:2;6351-6356:2.1;6381-6383:1;6437-6443:1;6449-6455:2;6460-6466:3;6482-6507:4;6483-6488:4.1;6546-6614:1;6551-6559:1.1;6636-6642:1;6653-6666:1;6666-6668:2;6717-6738:1;6738-6741:2;6792-6792:1;6806-6817:2;6808-6811:2.1;6812-6815:2.2;6821-6836:1;6844-6846:2;6870-6874:1;6871-6873:1.1;6889-6965:1;6897-6901:1.1;6913-6920:1.2;6969-6979:1;6988-6991:1;7014-7027:1;7064-7067:1;7065-7065:1.1;7066-7066:1.2;7073-7076:1;7074-7074:1.1;7075-7075:1.2;7080-7088:1;7129-7132:1;7145-7147:2;7164-7167:1;7209-7214:1;7214-7216:2;7240-7246:1;7241-7245:1.1;7246-7248:2;7267-7274:1;7268-7272:1.1;7274-7276:2;7299-7333:1;7301-7307:1.1")]
[assembly: global::go.GoPositionMap("net/http/sniff_test.go", "sniff_test.cs", "AEqkAYKCgoLKgKKigoKCgoKmlIKCgoLugoKUgIKkgoKklOygooKCgpaCgpaCgoKU7oKClIKUgoKmgAAMAqKavoKC7oKCgoIABhCEgoLugoKC7oKC+JKEgoKUgIKkgJKkgoKklM6A0oKCgoKCgpSCpoKCgpSAgqSAgg==", "93-100:1;135-138:1;181-187:1;190-199:2;202-210:3;213-220:4;223-229:5;231-252:6;259-269:1")]
[assembly: global::go.GoPositionMap("net/http/transport_dial_test.go", "transport_dial_test.cs", "ABIggpaCgoKCloKCpoKWgoKCqIKCgqaCloKCgrqCgoLMgoKChAAhUIKCuKSCgqaUktyCgIKkgoK4gqaqwoKSgu6CgpKClKKkpoKCgoKUqLKCgoKUgryihIKUhISClIKCgqiSgoKCgqiSgoI=", "121-129:1;129-150:2;130-149:2.1;169-172:1;173-184:2;175-177:2.1")]
[assembly: global::go.GoPositionMap("net/http/transport_test.go", "transport_test.cs", "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gpSCgpTGhIKCgpaCurKCgoK0gqQAAxLSpIKCgoLSgoS0xpCUgoKClITcgpQACA6CgoKEgoKCzIKCgqiCpoIACQSigoKygoKClJKCgoKCxoKCgoKClIKUgoKUkoKClICSpIKClICS+IDSooKigoKClIKCgpSCgoKUkpSCgoKClJSC1oKSgoKUgoKClJKClIKCgoKClIKCggANCoKCACRSspKCggAMHIKCpoAADAKCgqKCgqiAgqSAgqaSgpaWmAAeSrKygoKUlIKSkoKCgpaCgpaCqLiCgqqCgpSCgoKClJKCloKClIKWggAIDIKSuKKiogARINqCgoKCgoKUgqiAgriCgoIACAiCAAoagoCC2oKktISChISigoKClJKCxIKClIKClIKClIKUgpaSgIKClKSUgIL4gtSigoKKgpSCgpSSgIKkgIIACQyCgqSCqqIADASCgpaEADJwsqKCgoKCgpSClICSABUk4oKCgpTokqSClISClIKCgoKClJaCgoK6hJSCxoKCguiCgpSCgqaAkqSAkgAEEMKkgoSCgoKohIKChILSgoKClJKCgtiCgoKygtaChIIACA7SgoK4goKCpAALEKKCyoCmkPKCgoCCtoKUgIKklICCzKLkgoKCloKCgpSAkgAKFICigKKAooCigKKAAAwSgoKCgIK2psKCgpKShMqUlNKEgqaClOj4qIKE3uKClIKCgoKUgoKClu6ypIKCkoKCgpaCgoKEhIKCguKCyIKCgqaCgoKU/LS0pIK02IKC4oKCgoKUgqjqgoSEgqaAooKCgpaCgoKygoKCgoKCgvqmgPKClJSCgIK2goKCgriU1IKCgoaipoKCpoKClIKCgoKClNiygoSCooKCgriAgoKkgoKWgpCShJaCgpaCgpaohKiSgoKClJToggAKBqKClgADELKCooKClIKCgpSAkqSC", "107-115:1;142-144:1;176-186:1;212-234:1;268-294:1;358-388:1;369-376:1.1;433-443:1;484-497:1;507-524:2;508-514:2.1;568-573:1;578-582:2;589-606:3;592-594:3.1;638-643:1;653-660:2;662-685:3;664-668:3.1;669-671:3.2;690-693:4;743-748:1;787-789:1;794-810:2;818-827:3;841-865:1;842-848:1.1;898-906:1;922-936:2;947-953:1;982-989:1;995-995:2;1033-1048:1;1099-1126:1;1116-1118:1.1;1217-1225:1;1231-1233:2;1241-1248:3;1334-1395:1;1404-1406:2;1408-1430:3;1445-1547:1;1447-1449:1.1;1451-1490:1.2;1485-1488:1.2.1;1567-1569:1;1571-1613:2;1608-1611:2.1;1628-1640:3;1636-1638:3.1;1643-1652:4;1678-1688:1;1680-1686:1.1;1695-1725:2;1729-1731:3;1756-1758:1;1759-1761:2;1804-1808:1;1835-1838:1;1862-1865:1;1906-1911:1;1920-1929:2;1977-1978:1;2055-2063:1;2131-2134:1;2141-2161:2;2145-2158:2.1;2189-2192:1;2197-2205:2;2217-2222:1;2254-2283:1;2259-2261:1.1;2290-2292:1;2320-2322:1;2333-2333:2;2341-2365:3;2376-2378:1;2383-2393:2;2415-2417:1;2418-2421:2;2426-2436:3;2487-2490:1;2491-2495:2;2568-2583:1;2571-2573:1.1;2574-2576:1.2;2577-2581:1.3;2592-2595:1;2596-2600:2;2597-2599:2.1;2601-2605:3;2614-2616:1;2617-2619:2;2620-2624:3;2629-2641:1;2631-2633:1.1;2635-2637:1.2;2638-2640:1.3;2654-2658:1;2686-2695:2;2703-2705:1;2714-2717:2;2720-2731:3;2735-2737:1;2741-2743:1;2762-2769:1;2775-2780:2;2792-2794:3;2820-2824:1;2853-2862:2;2867-2874:1;2868-2870:1.1;2871-2873:1.2;2878-2880:1;2902-2906:1;2912-2915:2;2943-2952:1;3049-3053:1;3054-3056:2;3062-3073:3;3087-3109:4;3110-3112:5;3130-3130:1;3131-3131:2;3133-3177:3;3180-3194:4;3186-3190:4.1;3200-3216:5;3232-3237:1;3244-3247:2;3266-3274:1;3294-3300:1;3325-3329:1;3406-3413:1;3421-3428:1;3440-3444:1;3448-3448:2;3453-3455:3;3495-3497:1;3523-3531:1;3534-3536:2;3569-3578:1;3652-3654:1;3662-3672:2;3673-3676:3;3678-3694:4;3690-3693:4.1;3720-3729:1;3773-3779:1;3798-3800:2;3810-3812:3;3821-3823:4;3834-3836:5;3842-3922:6;3847-3852:6.1;3854-3857:6.2;3861-3879:6.3;3870-3877:6.3.1;3881-3883:6.4;3930-3933:1;3942-3948:2;3974-3978:1;3980-3989:2;4015-4019:1;4021-4026:2;4059-4063:1;4065-4074:2;4098-4100:1;4116-4123:1;4156-4163:1;4165-4165:2;4171-4174:3;4195-4199:1;4200-4200:2;4203-4215:3;4240-4247:1;4262-4266:1;4303-4326:1;4305-4310:1.1;4335-4335:1;4336-4348:2;4337-4347:2.1;4340-4342:2.1.1;4343-4345:2.1.2;4355-4357:3;4410-4412:1;4415-4420:2;4422-4433:3;4465-4467:1;4475-4483:2;4524-4578:1;4529-4536:1.1;4548-4555:1.2;4631-4633:1;4659-4662:1;4693-4693:1;4698-4714:2;4728-4733:3;4730-4732:3.1;4735-4737:4;4738-4752:5;4767-4769:1;4773-4775:1;4781-4788:1;4792-4792:2;4793-4793:3;4794-4794:4;4795-4795:5;4796-4796:6;4826-4830:1;4857-4859:1;4864-4866:1;4872-4884:1;4884-4888:2;4895-4900:3;4909-4915:4;4921-4921:5;4922-4922:6;4923-4923:7;4924-4924:8;4925-4925:9;4926-4926:10;4927-4927:11;4928-4933:12;4934-4936:13;4937-4939:14;4940-4940:15;4941-4941:16;4942-4945:17;4948-4948:18;4949-4951:19;4986-4990:20;4991-4995:21;5057-5062:1;5064-5066:2;5066-5071:3;5067-5070:3.1;5084-5084:4;5085-5087:5;5101-5105:6;5125-5128:1;5143-5148:1;5152-5152:2;5153-5153:3;5154-5154:4;5155-5155:5;5169-5173:6;5208-5208:1;5213-5217:2;5218-5225:3;5254-5256:1;5265-5267:2;5269-5277:3;5313-5315:1;5321-5327:2;5330-5363:3;5333-5337:3.1;5374-5382:4;5400-5402:1;5412-5434:2;5468-5480:1;5470-5473:1.1;5474-5477:1.2;5475-5475:1.2.1;5495-5508:1;5508-5512:2;5520-5526:3;5530-5535:4;5536-5540:5;5564-5575:1;5578-5580:2;5606-5617:1;5620-5622:2;5628-5633:3;5752-5761:1;5789-5793:1;5804-5809:1;5870-5891:1;5878-5878:1.1;5932-5960:1;5941-5941:1.1;5997-6009:1;6045-6075:1;6150-6155:1;6175-6195:1;6189-6192:1.1;6197-6199:2;6198-6198:2.1;6239-6302:3;6247-6263:3.1;6248-6262:3.1.1;6268-6272:3.2;6308-6308:1;6309-6311:2;6312-6312:3;6313-6313:4;6314-6314:5;6315-6315:6;6327-6327:7;6331-6331:8;6394-6403:1;6423-6431:2;6441-6450:1;6480-6482:1;6543-6557:2;6587-6587:1;6594-6602:2;6615-6618:3;6619-6625:4;6654-6659:1;6668-6679:2;6684-6687:3;6732-6736:1;6738-6744:2;6756-6759:1;6809-6811:1;6817-6833:2;6877-6882:1;6894-6913:2;6897-6904:2.1;6932-6945:3;6960-6963:1;6968-6979:2;6990-6994:1;7010-7021:1;7042-7057:1;7060-7060:2;7086-7095:3;7103-7105:1;7117-7133:2")]
// </GoSourcePositionMaps>

namespace go.net;

[GoPackage("http_test")]
public static partial class http_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct TestFileServerCleanPath_tests {}
    internal partial struct TestFileServerCleans_tests {}
    internal partial struct TestH12_RequestContentLength_Unknown_type {}
    internal partial struct TestHeaderToWire_tests {}
    internal partial struct TestInvalidChunkedBodies_type {}
    internal partial struct TestIs408_tests {}
    internal partial struct TestMaxBytesReaderDifferentLimits_tests {}
    internal partial struct TestMaxBytesReaderStickyError_tests {}
    internal partial struct TestMultipartReader_tests {}
    internal partial struct TestNewRequestContentLength_tests {}
    internal partial struct TestNewRequestContentLength_type {}
    internal partial struct TestNewRequestGetBody_tests {}
    internal partial struct TestOnProxyConnectResponse_type {}
    internal partial struct TestParseFormUnknownContentType_type {}
    [GoLocalName("version")] internal partial struct TestParseHTTPVersion_version {}
    internal partial struct TestPathValueAndPattern_type {}
    internal partial struct TestQuerySemicolon_tests {}
    [GoLocalName("ctHeader")] internal partial struct TestRedirectContentTypeAndBody_ctHeader {}
    internal partial struct TestRedirectContentTypeAndBody_type {}
    internal partial struct TestRedirect_type {}
    internal partial struct TestReferer_tests {}
    internal partial struct TestRequestCookie_type {}
    internal partial struct TestRequestCookiesByName_tests {}
    internal partial struct TestScanETag_tests {}
    internal partial struct TestServeFile_DotDot_tests {}
    internal partial struct TestServeWithSlashRedirectForHostPatterns_tests {}
    internal partial struct TestServerValidatesHeaders_tests {}
    internal partial struct TestServerValidatesHostHeader_tests {}
    internal partial struct TestServerValidatesMethod_tests {}
    internal partial struct TestShouldCopyHeaderOnRedirect_tests {}
    internal partial struct TestStatus_type {}
    internal partial struct TestStripPasswordFromError_testCases {}
    internal partial struct TestTransportProxy_testCases {}
    internal partial struct TestTransportRequestReplayable_tests {}
    internal partial struct apiHandler {}
    internal partial struct basicAuthCredentialsTest {}
    [GoLocalName("test")] internal partial struct benchmarkServeMux_test {}
    internal partial struct blockingRemoteAddrConn {}
    internal partial struct blockingRemoteAddrListener {}
    internal partial struct bodyCloser {}
    internal partial struct bodyLimitReader {}
    internal partial struct breakableConn {}
    internal partial struct brokenState {}
    internal partial struct byteAtATimeReader {}
    internal partial struct byteFromChanReader {}
    internal partial struct cancelProto {}
    internal partial struct cancelTest {}
    internal partial struct cancelableTimeoutContext {}
    internal partial struct clientServerTest {}
    internal partial struct closeWriteTestConn {}
    internal partial struct contextCounter {}
    internal partial struct countCloseListener {}
    internal partial struct countCloseReader {}
    internal partial struct countedConn {}
    internal partial struct countedContext {}
    internal partial struct countingDialer {}
    internal partial struct delayedEOFReader {}
    internal partial struct delegateReader {}
    internal partial struct doneContext {}
    internal partial struct dotFileHidingFile {}
    internal partial struct dotFileHidingFileSystem {}
    internal partial struct dummyAddr {}
    internal partial struct dumpConn {}
    internal partial struct eofListenerNotComparable {}
    internal partial struct errorListener {}
    internal partial struct fakeFS {}
    internal partial struct fakeFile {}
    internal partial struct fakeFileInfo {}
    internal partial struct fileServerCleanPathDir {}
    internal partial struct fooProto {}
    internal partial struct fsRedirectTestDataᴛ1 {}
    internal partial struct funcConn {}
    internal partial struct getBasicAuthTest {}
    internal partial struct getBasicAuthTestsᴛ1 {}
    internal partial struct gzipResponseWriter {}
    internal partial struct h12Compare {}
    internal partial struct handlerBodyCloseTest {}
    internal partial struct handlerTest {}
    internal partial struct handlersᴛ1 {}
    internal partial struct http09Writer {}
    internal partial struct infiniteReader {}
    internal partial struct issue12991FS {}
    internal partial struct issue12991File {}
    internal partial struct issue15577Tripper {}
    internal partial struct issue18239Body {}
    internal partial struct issue40382Body {}
    internal partial struct lockedBytesBuffer {}
    internal partial struct logWrites {}
    internal partial struct logWritesConn {}
    internal partial struct neverEnding {}
    internal partial struct newRequestHostTestsᴛ1 {}
    internal partial struct nilBodyRoundTripper {}
    internal partial struct noopConn {}
    internal partial struct noteCloseConn {}
    internal partial struct oneConnListener {}
    internal partial struct panicOnSeek {}
    internal partial struct parseBasicAuthTestsᴛ1 {}
    internal partial struct parseHTTPVersionTestsᴛ1 {}
    internal partial struct proxyFromEnvTest {}
    internal partial struct readRequestErrorTestsᴛ1 {}
    internal partial struct recordingTransport {}
    internal partial struct redirectTest {}
    internal partial struct repeatReader {}
    internal partial struct responseWriterJustWriter {}
    internal partial struct roundTripTestsᴛ1 {}
    internal partial struct roundTripperWithoutCloseIdle {}
    internal partial struct rwTestConn {}
    internal partial struct serveMuxRegisterᴛ1 {}
    internal partial struct serveMuxTests2ᴛ1 {}
    internal partial struct serveMuxTestsᴛ1 {}
    internal partial struct serverExpectTest {}
    internal partial struct slowTestConn {}
    internal partial struct slurpResult {}
    internal partial struct sniffTestsᴛ1 {}
    internal partial struct stringHandler {}
    internal partial struct terrorWriter {}
    internal partial struct testClientHeadContentLength_tests {}
    internal partial struct testClientRedirectTypes_tests {}
    internal partial struct testCloseConn {}
    internal partial struct testConn {}
    [GoLocalName("connKey")] internal partial struct testConnContextNotModifyingAllContexts_connKey {}
    internal partial struct testConnSet {}
    internal partial struct testConnectRequest_tests {}
    [GoLocalName("setting")] internal partial struct testContentEncodingNoSniffing_setting {}
    [GoLocalName("readerOnly")] internal partial struct testContentTypeWithVariousSources_readerOnly {}
    internal partial struct testContentTypeWithVariousSources_type {}
    internal partial struct testErrorReader {}
    internal partial struct testFileServerEscapesNames_tests {}
    internal partial struct testFileSystem {}
    internal partial struct testHTTP10ConnectionHeader_tests {}
    internal partial struct testHandlerBodyConsumer {}
    [GoLocalName("data")] internal partial struct testKeepAliveFinalChunkWithEOF_data {}
    internal partial struct testLogWriter {}
    internal partial struct testMockTCPConn {}
    internal partial struct testMode {}
    internal partial struct testNewClientServerTest_got {}
    internal partial struct testNotParallelOpt {}
    internal partial struct testRedirectsByMethod_log {}
    internal partial struct testRetryRequestsOnError_testCases {}
    [GoLocalName("serveParam")] internal partial struct testServeContent_serveParam {}
    [GoLocalName("testCase")] internal partial struct testServeContent_testCase {}
    internal partial struct testServeFileRejectsInvalidSuffixLengths_tests {}
    internal partial struct testServeWithSlashRedirectKeepsQueryString_tests {}
    [GoLocalName("stateLog")] internal partial struct testServerConnState_stateLog {}
    [GoLocalName("baseKey")] internal partial struct testServerContexts_baseKey {}
    [GoLocalName("connKey")] internal partial struct testServerContexts_connKey {}
    internal partial struct testServerExpect_type {}
    internal partial struct testStripPrefix_cases {}
    internal partial struct testTimeoutHandlerSuperfluousLogs_tests {}
    internal partial struct testTransportClosesBodyOnError_body {}
    internal partial struct testTransportClosesBodyOnInvalidRequests_tests {}
    internal partial struct testTransportNoReuseAfterEarlyResponse_sconn {}
    internal partial struct testTransportRejectsInvalidHeaders_tests {}
    internal partial struct testTransportRequestWriteRoundTrip_cases {}
    internal partial struct testTransportRespectRequestWantsClose_tests {}
    internal partial struct testTransportResponseHeaderTimeout_tests {}
    internal partial struct testTransportUserAgent_tests {}
    internal partial struct testValidateClientRequestTrailers_cases {}
    internal partial struct tlogWriter {}
    internal partial struct trackLastConnListener {}
    internal partial struct transport100ContinueTest {}
    internal partial struct transportDialTester {}
    internal partial struct transportDialTesterConn {}
    internal partial struct transportDialTesterRoundTrip {}
    internal partial struct vtestsᴛ1 {}
    internal partial struct wantRange {}
    internal partial struct wgReadCloser {}
    internal partial struct wrapWriter {}
    internal partial struct writeCountingConn {}
    internal partial struct writerFuncConn {}
    public partial interface TBRun<T> {}
    public partial struct RecordingJar {}
    public partial struct ServeFileRangeTestsᴛ1 {}
    public partial struct TestJar {}
    // </TypeAccessibility>
}
