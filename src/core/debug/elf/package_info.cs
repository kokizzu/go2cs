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
global using dwarfꓸLineReader = go.debug.dwarf_package.ΔLineReader;
global using dwarfꓸReader = go.debug.dwarf_package.ΔReader;
global using dwarfꓸType = go.debug.dwarf_package.ΔType;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
// </ImportedTypeAliases>

using go;
using static go.debug.elf_package;

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
[assembly: GoTypeAlias("Data", "ΔData")]
[assembly: GoTypeAlias("Section", "ΔSection")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<FormatError, error>(Pointer = true)]
[assembly: GoImplement<Prog, io_package.ReaderAt>(Promoted = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<errorReader, error>(Promoted = true)]
[assembly: GoImplement<errorReader, io_package.ReadSeeker>]
[assembly: GoImplement<go.@internal.zstd_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<io_package.ReadSeeker, io_package.Reader>]
[assembly: GoImplement<io_package.SectionReader, io_package.ReadSeeker>(Pointer = true)]
[assembly: GoImplement<io_package.SectionReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<io_package.SectionReader, io_package.ReaderAt>(Pointer = true)]
[assembly: GoImplement<nobitsSectionReader, io_package.ReaderAt>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Closer>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.ReaderAt>(Pointer = true)]
[assembly: GoImplement<readSeekerFromReader, io_package.ReadSeeker>(Pointer = true)]
[assembly: GoImplement<ΔSection, io_package.ReaderAt>(Promoted = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.debug;

[GoPackage("elf")]
public static partial class elf_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct errorReader {}
    internal partial struct intName {}
    internal partial struct nobitsSectionReader {}
    internal partial struct readSeekerFromReader {}
    internal partial struct verneed {}
    public partial struct Chdr32 {}
    public partial struct Chdr64 {}
    public partial struct Class {}
    public partial struct CompressionType {}
    public partial struct Dyn32 {}
    public partial struct Dyn64 {}
    public partial struct DynFlag {}
    public partial struct DynFlag1 {}
    public partial struct DynTag {}
    public partial struct File {}
    public partial struct FileHeader {}
    public partial struct FormatError {}
    [GoValueClone("Ident")] public partial struct Header32 {}
    [GoValueClone("Ident")] public partial struct Header64 {}
    public partial struct ImportedSymbol {}
    public partial struct Machine {}
    public partial struct NType {}
    public partial struct OSABI {}
    public partial struct Prog {}
    public partial struct Prog32 {}
    public partial struct Prog64 {}
    public partial struct ProgFlag {}
    public partial struct ProgHeader {}
    public partial struct ProgType {}
    public partial struct R_386 {}
    public partial struct R_390 {}
    public partial struct R_AARCH64 {}
    public partial struct R_ALPHA {}
    public partial struct R_ARM {}
    public partial struct R_LARCH {}
    public partial struct R_MIPS {}
    public partial struct R_PPC {}
    public partial struct R_PPC64 {}
    public partial struct R_RISCV {}
    public partial struct R_SPARC {}
    public partial struct R_X86_64 {}
    public partial struct Rel32 {}
    public partial struct Rel64 {}
    public partial struct Rela32 {}
    public partial struct Rela64 {}
    public partial struct Section32 {}
    public partial struct Section64 {}
    public partial struct SectionFlag {}
    public partial struct SectionHeader {}
    public partial struct SectionIndex {}
    public partial struct SectionType {}
    public partial struct Sym32 {}
    public partial struct Sym64 {}
    public partial struct SymBind {}
    public partial struct SymType {}
    public partial struct SymVis {}
    public partial struct Symbol {}
    public partial struct Type {}
    public partial struct Version {}
    public partial struct ΔData {}
    public partial struct ΔSection {}
    // </TypeAccessibility>
}
