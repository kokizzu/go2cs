// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.log.slog_package;
using static go.log.slog_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<captureHandler, global::go.log.slog_package.ΔHandler>(Pointer = true)]
[assembly: GoImplement<discardHandler, global::go.log.slog_package.ΔHandler>]
[assembly: GoImplement<wrappingHandler, global::go.log.slog_package.ΔHandler>]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("log/slog/attr_test.go", "attr_test.cs", "ACMaooQADxSSgoKCgoKClIKUgoKCgqaiAAgWgoKCgoKCgoKUgoKCgoKC")]
[assembly: go.GoPositionMap("log/slog/handler_test.go", "handler_test.cs", "AFougoKCggA2eqKCkoKUgpSCgoCCpIIACwyCgoKCkoKClKSkpIKCgoKSgpKCgtKCgIKkgIL4goKCgoIAHxDEhIKEAIMC9AOCgoKUAAgSgpSClABGsgGClAAJDpKCgoKSkgAGEKKCgpSCgIKkgoKCAAcUwpKCgqa4goIACg6CAAkKooKCgpYACBaCgoIADwqGooK6goKCgoKCAA0IhAAAEIKCgpSCpAAGHAAMGoLcgsqCgoLKooKCgoI=")]
[assembly: go.GoPositionMap("log/slog/json_handler_test.go", "json_handler_test.cs", "AD0uggAOIIKCgoKCgIKkgoIAChaA1IKClAAJDoCktAASJoKCgpSCyoKCgoKAgqT2lAAFEoKCyoKCgoCCpAAXBoK+goKUgpTKgoKUgpTogoLKgoKCAB0WogAJJIKClJKAgrgADiCCooKCgoIAEBaigoKSkoKCgoKUgqaSgoKAgqSm")]
[assembly: go.GoPositionMap("log/slog/level_test.go", "level_test.cs", "ABkcggAMIIKCyoKCgJKkgoCSpIKAksqCgoKCgpSClIKAgqSCuIKCgoKClIKUgoCCpIIACAiCAAocgoCCpIIACQqCAAYUgoKCAAkKgoKSgoKClICSyIKCgoKClIKAgqSAksiCgoKCgoKClICS+IKCgoKCgg==")]
[assembly: go.GoPositionMap("log/slog/logger_test.go", "logger_test.cs", "AEFCgoKEhIKCgpSCloKWgoSChIKEgoSChIKEggAQBoLKgoKCgqKCgqiCgoKCgoKCgoKCgoKCgoKCloKChLiCgrqCgpaCgoKAgriCgoKCgoKAggAIEIKkgKKAoqCkgpKCgqiCgoKCgqaigoKEkoKCgoKCgoK6goKogpaCgoKCgoKCgoKCgoKCgoKCgoKCgoKCggAYBqKCgoKEgoCkgoCkkpCkoqCkkoKCksySgoKCosySgoKCooLekoKCgpLckpCkoqCSoKSioqaSgqKmoqLKoqIADxCCAAcWgoKCgsq0gpKAkoKSgoKClIKAggANCqSCgoKSgoKWgoKEAAUUgoKC6pKCgpTMgoKCgqKCgoKWgoKCgoKC5oKEgoKUlIKCgpSCgqaCgoKCgtaCgoKUgpSCAAgIxoKCgoQACRqChIKAggAICoKCgoKCgpSCupKClAAKFtKCgoKC1oCkwoKCgoKCgtbCgoKCgoKC1sKCgoIAChCAooCigoKkgqqigoKCAAoIsoKCooKCpqKCgqaigqKCuKKCgqaigoKmooKigt6SgoLWgoKUgoKCggAIEoKmgva4goKCooKCloKChIIABRKCgpaCAAUSgoI=")]
[assembly: go.GoPositionMap("log/slog/record_test.go", "record_test.cs", "AA4egpSCgJKkgILKgoKCgpSCggALCrQABhaCgpSCgoCCpIIABxCCgoKClJaSgoKCzIKUgoKUlIKCgoKCqIKCgoKmgoKCpqKSgICSpoKqopKCgoKClMqigoSCgoKUgICk")]
[assembly: go.GoPositionMap("log/slog/text_handler_test.go", "text_handler_test.cs", "ACsqogAnUpIAECaSgoKCgoCCpJSCgoIADBqA/oDUgoKU1oKCgpSSgoCCpIKCgriCgpKClKKAlIKCgAAIBqIAChyCgg==")]
[assembly: go.GoPositionMap("log/slog/value_test.go", "value_test.cs", "ABgggoCS+KKSABgugoKCgoLcsoKAgraCAAsGggAJGoCC2qQAERaSgoKCgoKCgpSClIKCgoKCpoaigoCS9oIAFDCCgtyCAA8cgoKCAAgKgoKCgoCSpIKCqIKCqIKCgIK4goKCgoKogoKCgriAksiU6oKClICC2oK4goKCAAcQgNiArvKCgoKCgpSCgoKCgqY=")]
// </GoSourcePositionMaps>

namespace go.log;

[GoPackage("slog")]
public static partial class slog_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
