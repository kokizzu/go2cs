// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.time_package;
global using static global::go.time_internal_test_package;

// <ImportedTypeAliases>
global using bigꓸInt = go.math.big_package.ΔInt;
global using bigꓸRat = go.math.big_package.ΔRat;
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
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
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
using Δtesting = go.testing_package;
using Δtime = go.time_package;
// </ImportedTypeAliases>

using go;
using static global::go.time_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Hour", "const:ΔHour")]
[assembly: GoTypeAlias("Local", "const:ΔLocal")]
[assembly: GoTypeAlias("Location", "ΔLocation")]
[assembly: GoTypeAlias("Minute", "const:ΔMinute")]
[assembly: GoTypeAlias("Month", "ΔMonth")]
[assembly: GoTypeAlias("Nanosecond", "const:ΔNanosecond")]
[assembly: GoTypeAlias("Second", "const:ΔSecond")]
[assembly: GoTypeAlias("UTC", "const:ΔUTC")]
[assembly: GoTypeAlias("Weekday", "ΔWeekday")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<tickerTimer, timer>(Pointer = true)]
[assembly: GoImplement<time_package.Timer, timer>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<ParseTest, ж<ParseTest>>]
[assembly: GoImplicitConv<parsedTime, ж<parsedTime>>]
[assembly: GoImplicitConv<tickerTimer, ж<tickerTimer>>(Indirect = true)]
// </ImplicitConversions>

namespace go;

[GoPackage("time_test")]
public static partial class time_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface timer {}
    internal partial struct addDateTestsᴛ1 {}
    internal partial struct afterResult {}
    internal partial struct dateTestsᴛ1 {}
    internal partial struct dayOutOfRangeTestsᴛ1 {}
    internal partial struct daysInTestsᴛ1 {}
    internal partial struct defaultLocTestsᴛ1 {}
    internal partial struct durationAbsTestsᴛ1 {}
    internal partial struct durationRoundTestsᴛ1 {}
    internal partial struct durationTestsᴛ1 {}
    internal partial struct durationTruncateTestsᴛ1 {}
    internal partial struct goStringTestsᴛ1 {}
    internal partial struct hourDurationTestsᴛ1 {}
    internal partial struct invalidEncodingTestsᴛ1 {}
    internal partial struct jsonTestsᴛ1 {}
    internal partial struct longFractionalDigitsTestsᴛ1 {}
    internal partial struct mallocTestᴛ1 {}
    internal partial struct minDurationTestsᴛ1 {}
    internal partial struct monotonicStringTestsᴛ1 {}
    internal partial struct monthOutOfRangeTestsᴛ1 {}
    internal partial struct msDurationTestsᴛ1 {}
    internal partial struct notEncodableTimesᴛ1 {}
    internal partial struct nsDurationTestsᴛ1 {}
    internal partial struct parseDurationErrorTestsᴛ1 {}
    internal partial struct parseDurationTestsᴛ1 {}
    internal partial struct parsedTime {}
    internal partial struct secDurationTestsᴛ1 {}
    internal partial struct slimTestsᴛ1 {}
    internal partial struct subTestsᴛ1 {}
    internal partial struct tickerTimer {}
    internal partial struct truncateRoundTestsᴛ1 {}
    internal partial struct usDurationTestsᴛ1 {}
    public partial struct BenchmarkParallelTimerLatency_type {}
    public partial struct BenchmarkStaggeredTickerLatency_type {}
    public partial struct FormatTest {}
    public partial struct ISOWeekTest {}
    public partial struct ParseErrorTest {}
    public partial struct ParseTest {}
    public partial struct ParseTimeZoneTest {}
    public partial struct SecondsTimeZoneOffsetTest {}
    public partial struct TestAppendInt_tests {}
    public partial struct TestFirstZone_type {}
    public partial struct TestFormatFractionalSecondSeparators_tests {}
    public partial struct TestMarshalInvalidTimes_tests {}
    public partial struct TestQuote_tests {}
    public partial struct TestStd0xParseError_tests {}
    public partial struct TestTicker_type {}
    public partial struct TestTimeIsDST_tests {}
    public partial struct TestTimeWithZoneTransition_tests {}
    public partial struct TestTzsetName_type {}
    public partial struct TestTzsetOffset_type {}
    public partial struct TestTzsetRule_type {}
    public partial struct TestTzset_type {}
    public partial struct TestUnmarshalInvalidTimes_tests {}
    public partial struct TestZoneBounds_realTests {}
    public partial struct TimeFormatTest {}
    public partial struct TimeTest {}
    public partial struct YearDayTest {}
    // </TypeAccessibility>
}
