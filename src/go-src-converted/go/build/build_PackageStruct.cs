//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:52:22 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using errors = go.errors_package;
using fmt = go.fmt_package;
using ast = go.go.ast_package;
using constraint = go.go.build.constraint_package;
using doc = go.go.doc_package;
using token = go.go.token_package;
using buildcfg = go.@internal.buildcfg_package;
using exec = go.@internal.execabs_package;
using goroot = go.@internal.goroot_package;
using goversion = go.@internal.goversion_package;
using io = go.io_package;
using fs = go.io.fs_package;
using ioutil = go.io.ioutil_package;
using os = go.os_package;
using pathpkg = go.path_package;
using filepath = go.path.filepath_package;
using runtime = go.runtime_package;
using sort = go.sort_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using unicode = go.unicode_package;
using utf8 = go.unicode.utf8_package;
using go;

#nullable enable

namespace go {
namespace go
{
    public static partial class build_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Package
        {
            // Constructors
            public Package(NilType _)
            {
                this.Dir = default;
                this.Name = default;
                this.ImportComment = default;
                this.Doc = default;
                this.ImportPath = default;
                this.Root = default;
                this.SrcRoot = default;
                this.PkgRoot = default;
                this.PkgTargetRoot = default;
                this.BinDir = default;
                this.Goroot = default;
                this.PkgObj = default;
                this.AllTags = default;
                this.ConflictDir = default;
                this.BinaryOnly = default;
                this.GoFiles = default;
                this.CgoFiles = default;
                this.IgnoredGoFiles = default;
                this.InvalidGoFiles = default;
                this.IgnoredOtherFiles = default;
                this.CFiles = default;
                this.CXXFiles = default;
                this.MFiles = default;
                this.HFiles = default;
                this.FFiles = default;
                this.SFiles = default;
                this.SwigFiles = default;
                this.SwigCXXFiles = default;
                this.SysoFiles = default;
                this.CgoCFLAGS = default;
                this.CgoCPPFLAGS = default;
                this.CgoCXXFLAGS = default;
                this.CgoFFLAGS = default;
                this.CgoLDFLAGS = default;
                this.CgoPkgConfig = default;
                this.TestGoFiles = default;
                this.XTestGoFiles = default;
                this.Imports = default;
                this.ImportPos = default;
                this.TestImports = default;
                this.TestImportPos = default;
                this.XTestImports = default;
                this.XTestImportPos = default;
                this.EmbedPatterns = default;
                this.EmbedPatternPos = default;
                this.TestEmbedPatterns = default;
                this.TestEmbedPatternPos = default;
                this.XTestEmbedPatterns = default;
                this.XTestEmbedPatternPos = default;
            }

            public Package(@string Dir = default, @string Name = default, @string ImportComment = default, @string Doc = default, @string ImportPath = default, @string Root = default, @string SrcRoot = default, @string PkgRoot = default, @string PkgTargetRoot = default, @string BinDir = default, bool Goroot = default, @string PkgObj = default, slice<@string> AllTags = default, @string ConflictDir = default, bool BinaryOnly = default, slice<@string> GoFiles = default, slice<@string> CgoFiles = default, slice<@string> IgnoredGoFiles = default, slice<@string> InvalidGoFiles = default, slice<@string> IgnoredOtherFiles = default, slice<@string> CFiles = default, slice<@string> CXXFiles = default, slice<@string> MFiles = default, slice<@string> HFiles = default, slice<@string> FFiles = default, slice<@string> SFiles = default, slice<@string> SwigFiles = default, slice<@string> SwigCXXFiles = default, slice<@string> SysoFiles = default, slice<@string> CgoCFLAGS = default, slice<@string> CgoCPPFLAGS = default, slice<@string> CgoCXXFLAGS = default, slice<@string> CgoFFLAGS = default, slice<@string> CgoLDFLAGS = default, slice<@string> CgoPkgConfig = default, slice<@string> TestGoFiles = default, slice<@string> XTestGoFiles = default, slice<@string> Imports = default, map<@string, slice<token.Position>> ImportPos = default, slice<@string> TestImports = default, map<@string, slice<token.Position>> TestImportPos = default, slice<@string> XTestImports = default, map<@string, slice<token.Position>> XTestImportPos = default, slice<@string> EmbedPatterns = default, map<@string, slice<token.Position>> EmbedPatternPos = default, slice<@string> TestEmbedPatterns = default, map<@string, slice<token.Position>> TestEmbedPatternPos = default, slice<@string> XTestEmbedPatterns = default, map<@string, slice<token.Position>> XTestEmbedPatternPos = default)
            {
                this.Dir = Dir;
                this.Name = Name;
                this.ImportComment = ImportComment;
                this.Doc = Doc;
                this.ImportPath = ImportPath;
                this.Root = Root;
                this.SrcRoot = SrcRoot;
                this.PkgRoot = PkgRoot;
                this.PkgTargetRoot = PkgTargetRoot;
                this.BinDir = BinDir;
                this.Goroot = Goroot;
                this.PkgObj = PkgObj;
                this.AllTags = AllTags;
                this.ConflictDir = ConflictDir;
                this.BinaryOnly = BinaryOnly;
                this.GoFiles = GoFiles;
                this.CgoFiles = CgoFiles;
                this.IgnoredGoFiles = IgnoredGoFiles;
                this.InvalidGoFiles = InvalidGoFiles;
                this.IgnoredOtherFiles = IgnoredOtherFiles;
                this.CFiles = CFiles;
                this.CXXFiles = CXXFiles;
                this.MFiles = MFiles;
                this.HFiles = HFiles;
                this.FFiles = FFiles;
                this.SFiles = SFiles;
                this.SwigFiles = SwigFiles;
                this.SwigCXXFiles = SwigCXXFiles;
                this.SysoFiles = SysoFiles;
                this.CgoCFLAGS = CgoCFLAGS;
                this.CgoCPPFLAGS = CgoCPPFLAGS;
                this.CgoCXXFLAGS = CgoCXXFLAGS;
                this.CgoFFLAGS = CgoFFLAGS;
                this.CgoLDFLAGS = CgoLDFLAGS;
                this.CgoPkgConfig = CgoPkgConfig;
                this.TestGoFiles = TestGoFiles;
                this.XTestGoFiles = XTestGoFiles;
                this.Imports = Imports;
                this.ImportPos = ImportPos;
                this.TestImports = TestImports;
                this.TestImportPos = TestImportPos;
                this.XTestImports = XTestImports;
                this.XTestImportPos = XTestImportPos;
                this.EmbedPatterns = EmbedPatterns;
                this.EmbedPatternPos = EmbedPatternPos;
                this.TestEmbedPatterns = TestEmbedPatterns;
                this.TestEmbedPatternPos = TestEmbedPatternPos;
                this.XTestEmbedPatterns = XTestEmbedPatterns;
                this.XTestEmbedPatternPos = XTestEmbedPatternPos;
            }

            // Enable comparisons between nil and Package struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Package value, NilType nil) => value.Equals(default(Package));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Package value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Package value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Package value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Package(NilType nil) => default(Package);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Package Package_cast(dynamic value)
        {
            return new Package(value.Dir, value.Name, value.ImportComment, value.Doc, value.ImportPath, value.Root, value.SrcRoot, value.PkgRoot, value.PkgTargetRoot, value.BinDir, value.Goroot, value.PkgObj, value.AllTags, value.ConflictDir, value.BinaryOnly, value.GoFiles, value.CgoFiles, value.IgnoredGoFiles, value.InvalidGoFiles, value.IgnoredOtherFiles, value.CFiles, value.CXXFiles, value.MFiles, value.HFiles, value.FFiles, value.SFiles, value.SwigFiles, value.SwigCXXFiles, value.SysoFiles, value.CgoCFLAGS, value.CgoCPPFLAGS, value.CgoCXXFLAGS, value.CgoFFLAGS, value.CgoLDFLAGS, value.CgoPkgConfig, value.TestGoFiles, value.XTestGoFiles, value.Imports, value.ImportPos, value.TestImports, value.TestImportPos, value.XTestImports, value.XTestImportPos, value.EmbedPatterns, value.EmbedPatternPos, value.TestEmbedPatterns, value.TestEmbedPatternPos, value.XTestEmbedPatterns, value.XTestEmbedPatternPos);
        }
    }
}}