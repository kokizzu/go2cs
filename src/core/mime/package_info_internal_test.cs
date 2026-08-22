// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.mime_package;
using static go.mime_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
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
[assembly: go.GoPositionMap("mime/encodedword_test.go", "encodedword_test.cs", "ABccopIADyqCgIIACgqCAAcYgoKCgoKCloKCAAsOggAOJoKCgoKClIKClIIACAqiABxAgoKCgpSCAAsKggAMJrKCpIKUgoKUgoKUhKaCgpSCAAkKgqyAgviiguiihIK4ooSC")]
[assembly: go.GoPositionMap("mime/mediatype_test.go", "mediatype_test.cs", "AA0agtyigoKCgrbcggALGKKCgoKCttyCAAwaooKCgoKCtrYAORi0koKClJaCAKAC4gSCgoKCgpSUgJKUpIKUggAfRIKCgoKClIKUgpSClIIAIUKCgoKClIKUgoKUgpSigoI=")]
[assembly: go.GoPositionMap("mime/type_test.go", "type_test.cs", "AA0cgoKCgoK4gqaCgpSAgsiC7oSCgoIADArCgoKCgpSUAAcQgoKCAAwKooKEgoKClKaAgqSAgriAggAUCKKCgoKCgoKUlAAGFoKCgpSCgpSCgpSCAAsKgoKClIK4ooKEyoKCgu6igoTKgoKCgIIADRCigpSUlK6CgoKClII=")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("mime")]
public static partial class mime_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
