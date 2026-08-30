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
// </ImportedTypeAliases>

using go;
using static go.math.big_package;

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
[assembly: GoDynamicTypeLift("7374727563747b73796e632e4d757465783b207461626c65205b36345d6d6174682f6269672e64697669736f727d", "cacheBase10ᴛ1")]
[assembly: GoDynamicTypeLift("7374727563747b73796e632e4f6e63653b2076202a6d6174682f6269672e466c6f61747d", "threeOnceᴛ1")]
[assembly: GoTypeAlias("Int", "ΔInt")]
[assembly: GoTypeAlias("Rat", "ΔRat")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<Float, fmt_package.Formatter>(Pointer = true)]
[assembly: GoImplement<Float, fmt_package.Scanner>(Pointer = true)]
[assembly: GoImplement<byteReader, fmt_package.ScanState>(Promoted = true)]
[assembly: GoImplement<byteReader, io_package.ByteScanner>]
[assembly: GoImplement<bytes_package.Reader, io_package.ByteScanner>(Pointer = true)]
[assembly: GoImplement<fmt_package.State, io_package.Writer>]
[assembly: GoImplement<strings_package.Reader, io_package.ByteScanner>(Pointer = true)]
[assembly: GoImplement<ΔInt, fmt_package.Formatter>(Pointer = true)]
[assembly: GoImplement<ΔInt, fmt_package.Scanner>(Pointer = true)]
[assembly: GoImplement<ΔRat, fmt_package.Scanner>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<Float, ж<Float>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("math/big/accuracy_string.go", "accuracy_string.cs", "AA0OhqKCgu6CgoKU")]
[assembly: go.GoPositionMap("math/big/arith.go", "arith.cs", "AB1YABECgqiSgoKCqqKoxIKCgpSoxIKCgpSospSCgoKUAAIUAAkClIKCgpSCgpSmopSCgoKUqLKUgoKClIKClKaigoKUgpSCgoKCgpSCpqKCgpSClJSUgoKCgoKUgqailIKUprSCgoKClKyygoKCgpQADBqCgqaUgoIAESSCgqaCgpSokoKCgoI=")]
[assembly: go.GoPositionMap("math/big/arith_decl_pure.go", "arith_decl_pure.cs", "AAgSgqaCppSCgpSmlIKClKaCpoKmgqaC")]
[assembly: go.GoPositionMap("math/big/decimal.go", "decimal.cs", "AB9CkoKUAAYkAA0EgoKCzIKCgoKUgqiCgqiCgqaClJaCgoKUuriCgoKCgpSUgpSCgpSWgoKCgoKCgoKCqIKCgoKClJaCgoKClqaCgpaCloKCgriCgoK4goK2qJKClKyylKas0oKWgpS4goK6gpaUgoKCqIKosoKUgqqigoKUgoI=")]
[assembly: go.GoPositionMap("math/big/float.go", "float.cs", "AFuiAYKssoKUAD2QAQAJApaCgpSClKiClIKCgpSmgoKUrNKCgqqirLKClKiSrLKu4oKUgpSClAACJAATAoKUgpSCgoKmpqKUgoKWlIKCloKCAAIoABQCgoKUhJSUqJKokqrCgqaCpoKmqJKUlICCAAgIooKUgoKUgoKUgpQAAhYACgKCloKUuoKClAAOIoKmgpSWgoKCqIKWuIKUyKSkpKTshJSUlIKUlISSzISCuKKClIKCgoKmgoKCgoKUrLKs0oKCuKzSgpSClIKCgoKUgoKmgoKCgoKUrLKClIKCgoKmrAAJCIKClIKCgoKmgoKCrOKClJKCgoKUruKCgoIAAhIACQKClIKCgoKCgpSCpKaq0oKUgoKCgoKCgoKmqJKCgpSClJSkpKiSgoKUgpSUgoKUpKQAAhDygpaUgpaklqSCgpSWpqaSlKYAAhDygpaWgqSatIKClIKUlJSClJamppKUpgACEPKClpgABxaMguQABxC0goKUyoKClKyygoKKxIKUmtKCmuqCpoKWpoKClKaClKYAAhDygpaYAAcWjILkAAcQtIKClMqCgpSssoKCisSClJrSgprqgqaClqaCgpSmgpSmAAISAAkCgpaClpaCpJyCgrKWkpSClLS0tKampgACEAAIAoKWgpaWhIKUoraCtoKCorSmpqaqwoKCqsKCgqaClJSClIK+ABAWgrqChKiUgoKUgsjEgoKUgpTIrAALDIKWgoSElIKClILIxIKClIKUyIKCgoKorPKCAAYSgoKUlKzygsyWgoCUggAHEIaSgozCgpas4oKWlKTKgoKCkoKClIKClJSkuAACOAAdAoKCloKWAAYQhIKmuIKUgqaClJa4goKClpSCgoKWprquAAgCgoKWgpaUgoKmuIKUgqaClJa4goKClpSCgoKWprquAAgCgoKWgpaElIKWgriCgpamgrqCrgAIAoKCloKWhJSCloK4goKWpoK6gq7ygoKWgoKUpMqUpKYAAhTygpSkpKSClKaCgpQ=")]
[assembly: go.GoPositionMap("math/big/floatconv.go", "floatconv.cs", "ABss0oCCpK4ACQKCgqiWgoKYkoKCmJKCgoKogoKCgoIADSCClriCpLakpKTcpLakyoKCgoKUgpaUgrqCgpSWACZSwoKCqIKohIKClIKWAAJgADIEgoKUgoKWgoCCuICCpKaqogAEFNKCgg==")]
[assembly: go.GoPositionMap("math/big/floatmarsh.go", "floatmarsh.cs", "ABYq0oKogoKU3IKmlISCgoKUgoSCgpbu4pSClIKWgpaChIKCgoKChIKClIKWgoKWgIKmrLKClIKu1IKClA==")]
[assembly: go.GoPositionMap("math/big/ftoa.go", "ftoa.cs", "ABVeAB4CgoKUqqKq1IKogoKUqJSkpAAGEpKUqIKCgpSUtLTYlra0opTalKSmgpKcwpSCgoKUlIKUuIKUprSCAAocgoKClLS0mJKChpK6qIKCuqiolIKkgqSC3KSCgpSWgoKCgoKClIK6goKClIKClJSWgpSopIKCgoKmqIKCgqgAAhTygpaCuoKApLTGgoKCgpQAAhIACAKCgoKCgqaCloKYkoKUppaCgKS0xISCgpSCgoKWgoKUgqaClAACEuKCloLMgoKClISCgoKClAAEHAALAoKClsqmgqSytoKkgoKCqIKUgraCkpS0tLaCgIKmlpKCtpKCtpKC")]
[assembly: go.GoPositionMap("math/big/int.go", "int.cs", "AC1Y+IKUgpSosoKCgpSCgqiygoKotoKClIKklJSowoKClAACEAAICAACEPKCgqiygoKosoKCqNKCpriClIKmgqjSgqa4gpSCpoKoAAkKgoKClIKCrNKUpLiCgoKWgoKosoKmggABLgAYAoKCgoKCgpSs8oKCrPKCggACHAAQAoKCrOKCgoKCgpSmrOKCgpSCgoKClKYAAiIAEgKCgpSCgoKClIKmrgAMCsiCksa0tK7iqJKClKiSgpSCgpSqooKClKqiqJKCgpSokqrCgoKogoKClJYAAjIAFgKqooCCtoCCpKrCgoKs6IKu1IKCqtiqogACEuKmgqbkgoKCpoKClJSEgoKClJaCgpSClgACHAARAoKygpSUgoKClIKmgoKUgqaWAAIeABIExIKWgpSUtLQACBCUgsyCkoKCgpQAAhgADgSCgoKEgoSCgoKEgoSCqgAJAoSElIKCggADHAAQArSChISUgqiCgoKWgoKolJa4hKbMqJSUlJSSgrKCgoKCkoKCgpaCgoKEgoSUgqamgpSClKaCgpSCloKCgqiErgAIBIKCgpSCgq4ACQSCgpSCgpSSloK6gpSUppSqwoLOooKChIKClJaCgpSClIKCuoKCgoKmloKUggADGAAMAoKCggACFgANBoKCgoKCgoKCgoKCgqrUkoKChpKCgp7ygoKCgoKChJKCgoKWgpaUgoKCAAMQ8pSktLSSlpa2ttrCgoKowpSCgoKCloKCqqKUgpSUgpSCgpYAAhAACAKClIKCgoKClIKCqNKClIKCgoKogoKogqiCgoKo0oKUgoKCgqiCgpaUgoKCqIKCgqjSgpSCgoKCqIKCqIKogoKCqNKClIKCgoKogoKogqiCgoKowpSCgqiCgqrSgpSCgg==")]
[assembly: go.GoPositionMap("math/big/intconv.go", "intconv.cs", "AA8qAAgCgpSqwoKUqqKokoKCggAGJAANBJKUtLS0tpKmgoKogpS0tNiCgpS0tLT2gpaClIKCvJKCloKClLTKgoCSgKa2tgAKCoKCgoKCAAIcAA4EgoKogoKUhKaygoCCpJToxAAIEoKCgpSmggAHELKCgpS0tLTotII=")]
[assembly: go.GoPositionMap("math/big/intmarsh.go", "intmarsh.cs", "AA8ksoKUgoKCgpSCqJKUgpSCgpSCgqiygpSokoCCpAACEPKClKikgpQ=")]
[assembly: go.GoPositionMap("math/big/nat.go", "nat.cs", "ACNWgqaCgoKUpoKClJSYoqaCgpSCgqaUgIK2goKCpoKCgqaCgoSUpqbYgoKClISmgoKElLam2IKCgpSClqaigoKClLS0loKClpS0tKaCgoKogoSqooKCggAEHAANCoKUgoKCgoKCgoKCgoKClKaClJSqooCCypKAggAJGsK6goIAGTiCggAJGIKWgoKSgqiCkoK6gqiCAAgUgoKClAADFOKssoCCgIKCggAHFMKCgoKUpoKChJSkpMqCqIKCggAIFKiCgoKCggANHoKCloKCgpaCgoKClIKCgoKWlq7CgoKCgoKClJSUgoIAAhDShIKCloKEgpaCgpaChIKEgoIACRSSgpSkgoKCpoKWgoKClIKCggAEEISCgoKChIKCgoKCgoKCgoKWqqKWpKSktIKqooKAgqSClIKClKaC3tiAuIKCgoKCgoKkqqKClIKCpqiSgoKUgpSmgqiSgoKUgqiCgqiCgoKEqJKCgpSCqIKCgqiChKaCgoKClIKClJSCpIKClJSChLSokoKCpqqigoKClKaCgqaClKaCgoKCqIKClqiSgoKUgoKClKaCgoKCqIKClISmgoKCgoKCqIKClISmgoKCgoKCqIKClISqwoKUhIKClISClIK2grakgoKoqqKUqIK6grqCuoK6goKUqJTegoKUgIKkqIKCgoKEvIaygoKEgoKWgoKWloKEgoKEgoKWgoKWqAACFgAJBIKCAAYQggAKGJaWgoKWhKqigpSmlIK6goKEhJKClIKCgoKCgoIABhCCgoKAgqSClIKCgoKClILKgoKEgoKEgoKEgoKWgoKEgqiCgoKWqqKogqaCgoLMgoKCgpSWgoKCgoKCpoKEhJKCgoKogoSWgoKCgoKCgpSCgrioAAcQgoKorgAJCIKigoKCpJSogpSClqiSgpSqooSCgoKUgoKCgpSWqJKClIIAARDigoKCgoKCgqaClJS6koKUlKaClJSmgqaCgpSClII=")]
[assembly: go.GoPositionMap("math/big/natconv.go", "natconv.cs", "ABo+8oKUgqao6IKCgpSClAAHagAyBIaC3oKWlpKUgoKCgoKUlLS0tILGgoKCAAkWgoKCgoKCgoKCgpSCpIKUlpKUtLSClMa0goKUgpaCloKCgqiWgqiClpSmlKiClJaUlqqiqJKCqIK6goKUloCUgoKClpSCgoKCqJSCpoKCloK6goKCqKiWlrqCgriCgpYAAiQAEASEkoKUgoKClIKCgrqWgoK6goKUlIKCuIKCuJSCgoKCzJKCABgukqikgqiCgpiSgoKUqISSgoKCgpSCqIKCgpa6gpY=")]
[assembly: go.GoPositionMap("math/big/natdiv.go", "natdiv.cs", "APcD8gfCgpSCgoKCqtKCloKCgpaGooKCloKs4oKUtIKkgraCgoKolJKCqsKCgoKClIKClK7ygsyCgoKCgqiClJaClJSEloKErLKChIKWgpamgoKCzIKCloKCgpKCgqaCuLqCgoLMgoKmgpS6gpSWrLIABhgACAiCgoSCloKCpgACEAAKCIKCgoKogoKCqIKCAAcSloKUAAcSggAKGJaWgoKCAAscgoKCgoKClIKCgpSUgpSCgpSCzoKCgoKCgoKUgoCCgoKClLaClIKClIKo")]
[assembly: go.GoPositionMap("math/big/prime.go", "prime.cs", "AAo0ABkUgpSCmLiCgpaCloKEkpSCpIKCpKaElgACENKUgoSChKKEgoKClIKUgoKUgoKCgpSCprYAAjYAGQSCuIIACBSCgoKCgoKmlIKCgpTclLiCgoIADyKCgoIAGzyCgoKCgqaCgoKUgoK4goKClIKCugAHEIKCgpSCgoKCgoK6gpK4kriCgpQ=")]
[assembly: go.GoPositionMap("math/big/rat.go", "rat.cs", "AB1CkqrCgoKCgpSkpILWloKCloKCgoKUlK7yAAgigoKUgoIABxKCkoKCgIKkrLKCgqiCgpSClIKolIKCgoKmgoKCgoCUgsiEgoKUrvIACCKCgpSCggAHEoKSgoKAgqSssoKCqIKClIKUgqiUgoKCgqaCgoKCgJSCyISCgpSu8oKClIKClK7ygoKUgoKUquKCgoKUgpSCgqrCgpSCgoKUgqiygoKosoKCqLKCgqiygoKUgpSosoKCqLKCgqrSgpSCgq7CqJKuwgACFAAKBLiUpqKWgqamgoKCgLKCpKSsspSkpKSq0oKClIKu8pKCgqjSkoKCgoKo0pKCgoKCqNKUgoKClJSUgoKq4oKUkoKCgoKC")]
[assembly: go.GoPositionMap("math/big/ratconv.go", "ratconv.cs", "AA4iggAMEKKCgpSClICCpAACKgAUAoK6gIKAgqSCgoCCtoCCpIKUuJaCgpiSgoKCmJKCgoKogIK4ggAMIKK4gqS2pKSk3KS2pNyCgoKCpqaClIKCgpSmqIKUgqSWhAACJgAVBIKCgpSopLSCgoTEgqiSgoKClMyCloKCgoKCpIKUlIKUloKUgpSCpoKWqJKosoKCgoKUlKrCgpSs0oSCgoKCgqaohIKCloKWgoKCgoKogpSEgoKCgpSWAAIoABkSirKCjtKCgpKCgIKkggABFPKCgIKCyoKAgqSClg==")]
[assembly: go.GoPositionMap("math/big/ratmarsh.go", "ratmarsh.cs", "ABQosoKUgoKCgpSUgoKCgpSC6JKUgpSClIKClIKCgpSCgpSCgoKosoKUqKSAgqQ=")]
[assembly: go.GoPositionMap("math/big/roundingmode_string.go", "roundingmode_string.cs", "/oaigoKCgoLugoKU")]
[assembly: go.GoPositionMap("math/big/sqrt.go", "sqrt.cs", "ABIigoKUAAIYAAwCgpaClpSogoKCgsyCgsy4pOyWAAIQAA4OgoKCsoKCgoKCgoKWgoKCgoK6qqKUgg==", "18-20:1;99-108:1")]
// </GoSourcePositionMaps>

namespace go.math;

[GoPackage("big")]
public static partial class big_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct @decimal {}
    internal partial struct byteReader {}
    [GoValueClone("table")] internal partial struct cacheBase10ᴛ1 {}
    internal partial struct divisor {}
    internal partial struct form {}
    internal partial struct nat {}
    internal partial struct threeOnceᴛ1 {}
    public partial struct Accuracy {}
    public partial struct ErrNaN {}
    public partial struct Float {}
    public partial struct RoundingMode {}
    public partial struct Word {}
    public partial struct ΔInt {}
    public partial struct ΔRat {}
    // </TypeAccessibility>
}
