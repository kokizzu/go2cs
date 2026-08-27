// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using parse = go.text.template.parse_package;
// </ImportedTypeAliases>

using go;
using static go.html.template_package;
using static go.html.template_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<ErrorWriter, io_package.Writer>]
[assembly: GoImplement<S, I>]
[assembly: GoImplement<T, I>(Pointer = true)]
[assembly: GoImplement<badMarshaler, go.encoding.json_package.Marshaler>(Pointer = true)]
[assembly: GoImplement<goodMarshaler, go.encoding.json_package.Marshaler>(Pointer = true)]
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
[assembly: go.GoPositionMap("html/template/clone_test.go", "clone_test.cs", "AEMiopKCgpSSgoKClICSABkIjNKWppKSpoKAgqSAkriSkqaCgIKkgJK4kpKmkqaSgIK2gIK2gIK4goCCpICSuICCuICCpICCuIKAgqSAkviChLiSgoKUgoKCgoKmggAIDJKCkgAIDNKSkpKCpoKC+pKqkqiSkoSC+pKYkrSCgoKygoKAggAICtbGkpKC+IKSkpKClIK6kqiElIKSkoKCAAsMkpKSkoKAgqSAkg==")]
[assembly: go.GoPositionMap("html/template/content_test.go", "content_test.cs", "ACQcogAMHADdAsYFgpKCgoKCgoCCgqSAkoIACxaC7oLmgoKCkoCCpIKClIKCgIKkgoIACwqSlIKCloKChII=")]
[assembly: go.GoPositionMap("html/template/css_test.go", "css_test.cs", "ABcaogAKHIKCggAJCqIAFTKCgoIACAqiABw+goKClIKAgtqCkoKAgqSCgIIACQqiAAwegoKCyqIAABQAABiCgpaCgviCADBmgoKC+qKC6KKCuKKCgoK4ooKCguiiguiigg==")]
[assembly: go.GoPositionMap("html/template/escape_test.go", "escape_test.cs", "ACsolNqCABYGogAQMoQAjgXQCrKigqSClIKAgqSAkqSCgIKkgJKkggAJDKK4AA0gkoKAgoKkgJKCABIKogAJIgBYugGSgoCCtqaCgoKUgoKClISAgoKkgIIACgyiANYBugOCgoKCgpSCgoKUgoKUlIKCpoCCAAoMogDqBN4JgoKCgoKUgoIADAqiAFq8AYKSgoKClIKCgoKCgsqC3IKCgoKUgoL6goKCgoKk6KKCgoKUgoKk+KIACCaCgoKCgoKAggAKDqKSgpKSgpKCgoKklIKCgqQACAqSkoCC7ILewpKygoKCpoKCxIKCgoIACBCyhIKCpAAJCrKUlIKUgoKCgpSClLiCgriCgviikoKCgoIACgqykpSCgoCCooK0goCCpIKAgvqSvJKAgqSSgIKkgIKkkoKUgg==")]
[assembly: go.GoPositionMap("html/template/exec_test.go", "exec_test.cs", "AG2+AYIADRaigpQACQ6igpQANV6yxgAOHLKmoqaigoKCqJKmgqaCpoKmgoKCpoKCgpTMkoKUqJLWgoKUpoIAnAPmBoKmgqaCpqKokoKUgpKClJTYktiSqJKmooKClKaCpqKClIKClKaCpoKmooIADyCCgoKClIKCgpSUgoKUgoKUgrSCtpLGgoLKggAVFIKCgoKCgoKCgpKUkpSUlJSCgpSCgoKUggAIDJKCgoKClIKCpIKUAAsakoKClIKCgpSCgoL4ggAJGIKCggAUOIIAI0iCgpSClIKClIKCpoKCgpSCguiUABEWlIKCgoKUgoKCqIKCgpSCgoKUgoKmgoLogoKClIKCggCXAYICgoIACiKCgoKClIKCgoKUgoKUggALCoKmgoKUlIKClIKCgqaCgoKClIKCpoKCgoKUgoKCpoKCgqaCgoL8ooKClIKCAAkUgva0goKUgoKUgqaCgpSCgpSCgpSCgriCpoLugrqCyoLKgriigoKU3gAIBoK8goKUkoKWgoCCpICCpoKAgqSAggAOCKIAKFqykpKCgoKUggAJDIKClJKCgoKUgoLo3LiCkoKCgpSCAAwK7gAfSIKSggAHFIKCgpSUgoKUggALDLKUpgAfSoKCgoKUgoKkgpQACQyShpKCgoKUgoKUgqiCgoKCmJKCgoKClIIADB6CgoKClIKCgoKogoKCsoKCgoCCAAgKAAgGgoSSgoCCpJaogoKUgoKUgIIAChSCgoCCpNaCgoKClIKClLiAggAICpKCgqyygoCCpICC")]
[assembly: go.GoPositionMap("html/template/html_test.go", "html_test.cs", "ABIaogAAFgAAGoKCloKCgviCAA0igoCC2qKC6KKCuKKCuKKC")]
[assembly: go.GoPositionMap("html/template/js_test.go", "js_test.cs", "ABscogBGlAGCgIKkgIK4gpaCAAkMggAMBqIAL2iCgIKkgriCgoCCAAoKogApXIKCggAJCqIAHkaCgoIACgqiAAAUAAtKgoCCgqqigpaAgoIACgqCAAgagoLKooK4ooLoooIACAiivoIACAiivoK4ooK4ooK4ooK4ooI=")]
[assembly: go.GoPositionMap("html/template/multi_test.go", "multi_test.cs", "ADJYlIKClIKClAAIBoKCgpSCgoKU5oKCgpSCgpSCgoKUAAgGgoSCgoKogoKCgpSWgoKCgpQADBSCgoKU1oKCgpTmgoKClIKClAALFpKCgoKClIIACAaCgoKAgqSAgqSAgvqSggAJCJKCgoKUgoKUgoL4poKAgqSAgqSAggAKCIIACBqChJiCgoKmgoCCgqSCAAkQ2viCgoKClIKCgqaCgoI=")]
[assembly: go.GoPositionMap("html/template/transition_test.go", "transition_test.cs", "ABIaogARKoKAggANCqSIgoKEgoKSgIKk")]
[assembly: go.GoPositionMap("html/template/url_test.go", "url_test.cs", "AA4WogANIIKAgqSCAAoKogAAFAALQoKAgoIACwqCAB9KgoCCAAgKooK4ooLoooK4ooLoooK4ooI=")]
// </GoSourcePositionMaps>

namespace go.html;

[GoPackage("template")]
public static partial class template_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
