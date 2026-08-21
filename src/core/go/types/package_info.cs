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
global using constantꓸKind = go.go.constant_package.ΔKind;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using runtimeꓸError = go.runtime_package.ΔError;
global using tokenꓸFile = go.go.token_package.ΔFile;
global using tokenꓸPos = go.go.token_package.ΔPos;
global using tokenꓸPosition = go.go.token_package.ΔPosition;
using ast = go.go.ast_package;
using bytes = go.bytes_package;
using token = go.go.token_package;
using typeparams = go.go.@internal.typeparams_package;
// </ImportedTypeAliases>

using go;
using static go.go.types_package;

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
[assembly: GoTypeAlias("Error", "ΔError")]
[assembly: GoTypeAlias("Info", "ΔInfo")]
[assembly: GoTypeAlias("Scope", "ΔScope")]
[assembly: GoTypeAlias("Signature", "ΔSignature")]
[assembly: GoTypeAlias("String", "const:ΔString")]
[assembly: GoTypeAlias("Term", "ΔTerm")]
[assembly: GoTypeAlias("Type", "ΔType")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<Alias, cleaner>(Pointer = true)]
[assembly: GoImplement<Alias, ΔType>(Pointer = true)]
[assembly: GoImplement<Alias, ΔgenericType>(Pointer = true)]
[assembly: GoImplement<ArgumentError, error>(Pointer = true)]
[assembly: GoImplement<Basic, ΔType>(Pointer = true)]
[assembly: GoImplement<Builtin, go.go.types_package.Object>(Pointer = true)]
[assembly: GoImplement<Chan, ΔType>(Pointer = true)]
[assembly: GoImplement<Const, go.go.types_package.Object>(Pointer = true)]
[assembly: GoImplement<Interface, cleaner>(Pointer = true)]
[assembly: GoImplement<Interface, ΔType>(Pointer = true)]
[assembly: GoImplement<Label, go.go.types_package.Object>(Pointer = true)]
[assembly: GoImplement<Label, positioner>(Pointer = true)]
[assembly: GoImplement<Map, ΔType>(Pointer = true)]
[assembly: GoImplement<Named, cleaner>(Pointer = true)]
[assembly: GoImplement<Named, ΔType>(Pointer = true)]
[assembly: GoImplement<Named, ΔgenericType>(Pointer = true)]
[assembly: GoImplement<Nil, go.go.types_package.Object>(Pointer = true)]
[assembly: GoImplement<PkgName, go.go.types_package.Object>(Pointer = true)]
[assembly: GoImplement<PkgName, positioner>(Pointer = true)]
[assembly: GoImplement<Pointer, ΔType>(Pointer = true)]
[assembly: GoImplement<Slice, ΔType>(Pointer = true)]
[assembly: GoImplement<StdSizes, Sizes>(Pointer = true)]
[assembly: GoImplement<Struct, ΔType>(Pointer = true)]
[assembly: GoImplement<TypeName, go.go.types_package.Object>(Pointer = true)]
[assembly: GoImplement<TypeName, positioner>(Pointer = true)]
[assembly: GoImplement<TypeParam, cleaner>(Pointer = true)]
[assembly: GoImplement<TypeParam, ΔType>(Pointer = true)]
[assembly: GoImplement<Union, ΔType>(Pointer = true)]
[assembly: GoImplement<Var, go.go.types_package.Object>(Pointer = true)]
[assembly: GoImplement<Var, positioner>(Pointer = true)]
[assembly: GoImplement<atPos, positioner>]
[assembly: GoImplement<byUniqueMethodName, sort_package.Interface>]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<gcSizes, Sizes>(Pointer = true)]
[assembly: GoImplement<go.go.@internal.typeparams_package.IndexExpr, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.AssignStmt, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.BasicLit, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.BranchStmt, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.CallExpr, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.ChanType, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.CompositeLit, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.Decl, positioner>]
[assembly: GoImplement<go.go.ast_package.Ellipsis, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.Expr, positioner>]
[assembly: GoImplement<go.go.ast_package.Field, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.FieldList, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.File, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.FuncLit, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.Ident, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.ImportSpec, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.InterfaceType, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.KeyValueExpr, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.Node, positioner>]
[assembly: GoImplement<go.go.ast_package.ReturnStmt, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.SelectorExpr, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.Spec, positioner>]
[assembly: GoImplement<go.go.ast_package.Stmt, positioner>]
[assembly: GoImplement<go.go.ast_package.TypeAssertExpr, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.TypeSpec, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.TypeSwitchStmt, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.UnaryExpr, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.ast_package.ValueSpec, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.types_package.Array, ΔType>(Pointer = true)]
[assembly: GoImplement<go.go.types_package.Func, go.go.types_package.Object>(Pointer = true)]
[assembly: GoImplement<go.go.types_package.Func, positioner>(Pointer = true)]
[assembly: GoImplement<go.go.types_package.Object, positioner>]
[assembly: GoImplement<go.go.types_package.Tuple, ΔType>(Pointer = true)]
[assembly: GoImplement<importDecl, decl>]
[assembly: GoImplement<inSourceOrder, sort_package.Interface>]
[assembly: GoImplement<lazyObject, go.go.types_package.Object>(Pointer = true)]
[assembly: GoImplement<nodeQueue, go.container.heap_package.Interface>(Pointer = true)]
[assembly: GoImplement<operand, positioner>(Pointer = true)]
[assembly: GoImplement<posSpan, positioner>]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<typeParamsById, sort_package.Interface>]
[assembly: GoImplement<ΔError, error>]
[assembly: GoImplement<ΔSignature, ΔType>(Pointer = true)]
[assembly: GoImplement<ΔSignature, ΔgenericType>(Pointer = true)]
[assembly: GoImplement<ΔconstDecl, decl>]
[assembly: GoImplement<ΔfuncDecl, decl>]
[assembly: GoImplement<ΔtypeDecl, decl>]
[assembly: GoImplement<ΔvarDecl, decl>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<Alias, ж<Alias>>(Indirect = true)]
[assembly: GoImplicitConv<Basic, ж<Basic>>(Indirect = true)]
[assembly: GoImplicitConv<Chan, ж<Chan>>(Indirect = true)]
[assembly: GoImplicitConv<Const, ж<Const>>(Indirect = true)]
[assembly: GoImplicitConv<Context, ж<Context>>(Indirect = true)]
[assembly: GoImplicitConv<Interface, ж<Interface>>(Indirect = true)]
[assembly: GoImplicitConv<Label, ж<Label>>(Indirect = true)]
[assembly: GoImplicitConv<Map, ж<Map>>(Indirect = true)]
[assembly: GoImplicitConv<Named, ж<Named>>(Indirect = true)]
[assembly: GoImplicitConv<Package, ж<Package>>(Indirect = true)]
[assembly: GoImplicitConv<PkgName, ж<PkgName>>(Indirect = true)]
[assembly: GoImplicitConv<Pointer, ж<Pointer>>(Indirect = true)]
[assembly: GoImplicitConv<Slice, ж<Slice>>(Indirect = true)]
[assembly: GoImplicitConv<Struct, ж<Struct>>(Indirect = true)]
[assembly: GoImplicitConv<TypeName, ж<TypeName>>(Indirect = true)]
[assembly: GoImplicitConv<TypeParam, ж<TypeParam>>(Indirect = true)]
[assembly: GoImplicitConv<Union, ж<Union>>(Indirect = true)]
[assembly: GoImplicitConv<Var, ж<Var>>(Indirect = true)]
[assembly: GoImplicitConv<ast.BasicLit, ж<ast.BasicLit>>(Indirect = true)]
[assembly: GoImplicitConv<ast.BinaryExpr, ж<ast.BinaryExpr>>(Indirect = true)]
[assembly: GoImplicitConv<ast.BlockStmt, ж<ast.BlockStmt>>(Indirect = true)]
[assembly: GoImplicitConv<ast.CallExpr, ж<ast.CallExpr>>(Indirect = true)]
[assembly: GoImplicitConv<ast.FieldList, ж<ast.FieldList>>(Indirect = true)]
[assembly: GoImplicitConv<ast.FuncType, ж<ast.FuncType>>(Indirect = true)]
[assembly: GoImplicitConv<ast.Ident, ж<ast.Ident>>(Indirect = true)]
[assembly: GoImplicitConv<ast.InterfaceType, ж<ast.InterfaceType>>(Indirect = true)]
[assembly: GoImplicitConv<ast.LabeledStmt, ж<ast.LabeledStmt>>(Indirect = true)]
[assembly: GoImplicitConv<ast.SelectorExpr, ж<ast.SelectorExpr>>(Indirect = true)]
[assembly: GoImplicitConv<ast.SliceExpr, ж<ast.SliceExpr>>(Indirect = true)]
[assembly: GoImplicitConv<ast.StructType, ж<ast.StructType>>(Indirect = true)]
[assembly: GoImplicitConv<ast.TypeSpec, ж<ast.TypeSpec>>(Indirect = true)]
[assembly: GoImplicitConv<ast.UnaryExpr, ж<ast.UnaryExpr>>(Indirect = true)]
[assembly: GoImplicitConv<block, ж<block>>(Indirect = true)]
[assembly: GoImplicitConv<declInfo, ж<declInfo>>(Indirect = true)]
[assembly: GoImplicitConv<go.go.types_package.Array, ж<go.go.types_package.Array>>(Indirect = true)]
[assembly: GoImplicitConv<go.go.types_package.Func, ж<go.go.types_package.Func>>(Indirect = true)]
[assembly: GoImplicitConv<go.go.types_package.Tuple, ж<go.go.types_package.Tuple>>(Indirect = true)]
[assembly: GoImplicitConv<operand, ж<operand>>(Indirect = true)]
[assembly: GoImplicitConv<token.FileSet, ж<token.FileSet>>(Indirect = true)]
[assembly: GoImplicitConv<typeparams.IndexExpr, ж<typeparams.IndexExpr>>(Indirect = true)]
[assembly: GoImplicitConv<ΔInfo, ж<ΔInfo>>(Indirect = true)]
[assembly: GoImplicitConv<ΔSignature, ж<ΔSignature>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: global::go.GoPositionMap("go/types/alias.go", "alias.cs", "ABxAopSCqqCkgK7QqKCooKiigqqgqKCswoCCpKaigpSCgpSCAAYQgpaqooKq0oKCgoKCgqiClqzSgoKCgoKCgqbGhKY=")]
[assembly: global::go.GoPositionMap("go/types/api.go", "api.cs", "AD6AAaIACBKAooAAaPgBwgBz+AGCqqKAgqSAgoCCxgACFPKAgqQAAhIACAKCgpSUggAKGqKokqqirLKUpKqiqqKqoqqiABMogoKCgpSUgoIAAhwACwKC")]
[assembly: global::go.GoPositionMap("go/types/api_predicates.go", "api_predicates.cs", "AA0mAAkGgpQAAhDSkoIAAhDSkq7ClLiClK7CAAIiAA4CgqqigoI=")]
[assembly: global::go.GoPositionMap("go/types/array.go", "array.cs", "AA8koKigppCkgKKA")]
[assembly: global::go.GoPositionMap("go/types/assignments.go", "assignments.cs", "ABMwAAgChJSkzsKCpoLcgoKCgoK2toKCgoKUpoKCgpSkpKSCgpSCgpSCgsyAgoKC3IKWkoCCgpSU+LKCgpSogoKClJSWgpaCgpau8oKClIKogoKUgoKCgpSUlq70loKCnLKCgoC4gIKC2oKEgpaCupTIgJKCgoKCtoKm3NKCgoKUlJaClIKAgraCloKUqLKClKiygpT+woKCgoS0pMq4tqSClJSmgoKU5oKCgoSCgIKCtuaCkoKCgqSUgoKCgv7igoKWuIKCuoKCgoKClIKUupSCgoKUuIKCppaCgoKCuIKUuoKClLiCgs7SuIKCuoKClLqUgoKClJaCgoKCuIKUuoKUqLKCloKCgoKCgoKUgoKWgoKCgoKU3oCClICClIKkuIKCgpSogoKoloSCggAGEIKC")]
[assembly: global::go.GoPositionMap("go/types/badlinkname.go", "badlinkname.cs", "AAko")]
[assembly: global::go.GoPositionMap("go/types/basic.go", "basic.cs", "AEaYAZCmkKaQpICigA==")]
[assembly: global::go.GoPositionMap("go/types/builtins.go", "builtins.cs", "AB4uAAoCloKCpoIABhCCgpScsoIAASAAAhmCgpKCqJIABRCCgoKklIKCqK6C0oCClIKUtICClNbGgqyygIKCgIKCgoKUgoLckoKGgoKSzIKipIKCgpTaisKCgpT6toLIgpSCpIL2pIK2lLiUgoKClJSYkpaCgqiUgqSkgpSWgoK6koKCgpSCgpSUlIKCuoaCopSClMrWAAQUAAgCgoKmgpSCyqKYkoKaooKAgpSkpMaUgoKCmJKUloKWqISCgpKUhIKCloKCloKUgqyCsoKCgoKUgoKUgpSWgoKCloKCAAMQsqaC7pSCvqKCgIKUpKTGlIKCgoKUgpiSgpSmloKWrIKCspaClMS0gqSCpIKCloKCgoKCgqaCloKCkrykgoKWgoKWgoKogoKCloKCloKCprySlIKCqpKWgoKClLyCopaCgoK+xIKUgpSWgoKWgoK8ooKCgoKClKiCgrqCgpK6lIKCloKCloKCgrqSgpaCgoKmgpaqgoKygoKWgoKWgoKClIKswqSCgpiWkpKClJ7CgoKmgoKClIKWqKKCloKCgqaCgoKUgpaopIKCgpaCgpaCgoK6lIKCgpaCgoK6lIKCloKCloKCgrqUgoKWgoKCvrKClIKClIIABBbygoKUgoKCgoKUgsq2gtwACQiAgoCCpIKUgqK4pKSCgtjEpAAGFAAKAoCGooKClICCgqSUnsKUpKSkpLqCgoSmqsKCgpSCgoKClKqigIKAgsY=")]
[assembly: global::go.GoPositionMap("go/types/call.go", "call.cs", "ABNIABgChIKCgpSUhpKCgoKCgoKUzIKSlIKCloKCAAEaAAoCgoKCgoKUpoK4goKCuoSCgoKClIKUlJaCgoLm4oKEgoKCgoKogoKClqKCgJSCgpSUuObCgoK4lJSClKiUgoKokoKUgoKApLSCgoKClICCgoK2xoLEgqiCkpSEkpTKloKCgoKCqIaSgoKCgoKCgoKUlpKCgoKCggAGEIKCpoK6goSUqJS0gpSUtIK0gqiClqrCgIK2goKCgrYAAhQADgKClJSCgoCUAAgSgoKClpSCgoCUgpSClLiUpoKCgJSCgrj6goKCgoKAlIKUgpS4uKSoAAUeABECAAgWgoKWgoKClJSCuLiCgoKCgpSCgqa4lILMgoKCgoKUlpKClIKCgoKCmKaCgoKkgrTIkoKUgqaGkoKUgO7KgoKCgpSCpoKUyJbOgoKCyoKUqIK4gpS6goKCgpSCgrqCgoKoABcc1NzKgIKCgIKCgoKEgoK4gpSUpoKCpoKClJSCgoKUlIK4qJSCgoK0grSCgoLGgoKCgsaCgsSCpILIgpaSgriSpAAQIoKCloKUgpaUgpaCgpSUloKClIKUgqiAgqaUgoKCloSCgoKaooIABxCClIKCpoKC3qiUgoKUlLoAAhCCAAsMgtyAggALFoKAgoLcuIaSgoTG2oKEgoIAAhDwqtCkgoKCgqamooKC2qKasoKCgLiAgoLYgoLWpA==")]
[assembly: global::go.GoPositionMap("go/types/chan.go", "chan.cs", "ABg2kqiQppCkgKKA")]
[assembly: global::go.GoPositionMap("go/types/check.go", "check.cs", "ADtqgoKCgoKUABk2koIAGDbCggA9hgGSgoKUgIKkAAISAAgCgoKUgqiygoKokoKmgoKCgpQAAhLigoKokoKokoKCgoIABhKiqvSCqIIABxSEAA4c1IKChIKCgoKCloKCgLSClJSmpuyCgpSEgoK6uJaAAAoWuqa2uIKClOzSxsb6AAoCuM6CggAPDgAKBoKClJSClJaCgoKogoSChIKEgoSChIKEgoKWgoSUloKWgoKCgoKCggAIDAALDoKCgoKUpoKCpoKopIKUpqaigpSkpIKkpISmlOiigpaCgoKUuIKCgoKUgqaUgILIyoKCtKTE3qKCgoKUgoKAgoKCgoKolIKClAAFFuKCgoKAgviCgpS01KSUpJiSgoKmgoKAgsiCgoKAgsiCgoKAgsiigoKAgsiCgoKAgg==")]
[assembly: global::go.GoPositionMap("go/types/const.go", "const.cs", "ABUs0oS4gt6CgpiSgoKClIIAAyAAEAKCloKClpKClpSCgpSClICClIKkgqSCpIKkpICCpKSCpIKkgqSkyICkkqSk6IKClJSClIKCgraClIKCgrak2IKClJSClIKCgoK2gpSCgoKCtqTYpqamgoKCpoKCgoKUpoKCpoKCgpSqwoKCgoKUgq7ygpKCAAcQgpSmlPaCgpSkpKjCgoKCgpSCgpSCgpSCgg==")]
[assembly: global::go.GoPositionMap("go/types/context.go", "context.cs", "ADZ0kgAFErKCgoSCuIKmlqrigoSCgpSUqN4ACAKEgoSCgpSUqMzY0oKCgoKCgpQ=")]
[assembly: global::go.GoPositionMap("go/types/conversions.go", "conversions.cs", "ABIowoSCgOikgoCCpIKUtJaCgpaYsoKCAAQQ9IKClIKUgpSUlJSUtoLGgoKUlILMggAIEraklJYABiIAEwSAgqaCgoKCgoKogsyAgoCCguyCqIKogqiCuoKmgrqAgpSCgqaUlMaAgoKCpoKUAAgOgpaSgoKClMyUkrKClIKygpSCgpTIkrKClIKCgpS2ooKUgoKUuKaCgqaCgqaCgqaCgIKCpA==")]
[assembly: global::go.GoPositionMap("go/types/decl.go", "decl.cs", "ABEg6oKAgoKCgoKklIK6koKCgpSU6uKCgpSCgoKCABw+goKWtIiCsriCqAACGpQACwLIgsjcyAAFFKSCAAgGgoKCqIKUAAcSlIK0graSttQADwwACASCgoKCgoK6goKCgoKCgoK0urKCAAEYAAkCgpSAgpS2ggAICuiCgoKUlIKClLq4gsyCqILo+oiClIKmgrqCgpSUloKClJSCgoKCgpSClIKqopKCgqYAHiqAooCigKKAooCkgoK4gsiSgpS0loKUgrSCtJKkgqTWxOjEyNKWgoKUgpaWgoKmgpSClJiSgu6UlAAIBrKWggAKGIKUlJaCgoKCgpaUgoKCgqaCAAYQgoKoqJKCgpSCAAgG4paEgoKAkraYkoKopoKUgpYACBKCloKCgpSCgpaCgoLuhIKCloKClKiCloKEgoKCqIKCqIIABhCCggAJCOK4goLMAAcSgoKCloKCpoKCyoKmlIKU6LiClLSkgoKUgIKklNYACA6CgoKCloKWpvqCgpSChriCgqiSzIKCgrqmgoCCgpSUpoLKgoCCgoKCgrqCgoCmqIKCggAIDsKWhIIABhCCgoKCqIKEgrqCosqihJKUhoKSgoSCgpaYjILSyISCgpiSgoKWpoKkoraC3IKCpqqIgsTIiIK0gpLE")]
[assembly: global::go.GoPositionMap("go/types/errors.go", "errors.cs", "ABYkgoKmgIKkABMqkoKUAAISAAgCqJKAuMiCpoKClNiSgpaCgoKCgoKmlPiSggAGEIKUgoKogsyCgoKCgrqCgoKmqKiShO6CgqiCqIKCgIKU7JaCAAgUyoKCgpaCloKClAAKGoKCgqaigoKmooKCgqaigoKCzIIACxiCrLKSgpSsspS0pIKkgoKUtII=")]
[assembly: global::go.GoPositionMap("go/types/errsupport.go", "errsupport.cs", "AA8eAB42AAUSgoKCgoKCpIKktoK2qIKUpKSktoKUtLSUpKSkuKqigoKm")]
[assembly: global::go.GoPositionMap("go/types/eval.go", "eval.cs", "ABAwAA0EgoKWpoIAAioAGQSSgoKk3IKAgraCgoKmgsyCgoKWkoKChA==")]
[assembly: global::go.GoPositionMap("go/types/expr.go", "expr.cs", "AEGCAZQABxCigIKCgqaCpKqilILGgrYADySigoCCpOjSgoKWgpiAooKCpIKCpoKCgoKUgoKCgpSCgoKWgoKCqJKCgpSCpoKCloKUlIKClIKCgpaokqaUlKQAAhgACQLWooKCqAAPJLKClAALGgAJENKUtoKU7KaC2MqCgoK6hLiCgtyUkoKCuqiSgIKCAAQUAAgCgqiUgIKklqSCgoKUnJTCtoK8sriSlqTWkoKClIKUpoKUnMKWkpT0gpa0pKjkgoKWgpaCqIKCgpS4goKogpaWgqKUyoLIgoK2goLalIK0gtjIgrjKgrqChISSlIKUlKaClJSqorSmkpKUAAwIkpSkpKSkpIKUpKSkyuaCgpbKgoIAAxCilIKCgoKWpoKCgsqUgoLKooKCxoKCuIKUgpSClJaSgoKCgtyCpoKCgoCCpIKoABMogIKCtoK6goKClsqUAA4m0oSChIKUgoKCloKCloKCloKClqaCgoKUgpSmgpaCgpaUgoKCqIKCgoKCgrqUgpSmgpSCgoKWrAARFJSC3IKmgqaCpoKUgriClJaCgoKUgoKCABEwooKAgrYABRQACwKCgoKCgqiEgpaEAAkM0oKUgrSCxoKCgpS2goKCvMKCgqaCgpSClIKCgpSCgpSCAA8OAAkGgoSUtrqitpQAABQACQKCgraCyoKWxoCmgoK4gsqypoKUgtiUmoCygLiCgsaCuIKCkoLKkraoooKUgpqCgMSCgoKCgpSmgoKClIKCgoCCpIKClIKCgpSCgpS4goCCgqSCgoKmgoKmlIKUggAGEsKClAAAEgAIAsqC3qKClLqigpqCgrKCgoKUgoKClIKCgoKCgoKmlIKUgoKmgtzCgLiklpKCuIK4koKm1oKCgpSUgsiCgsiCgpa2gpSCgpSAgoKkgoKUgoK2poKUpIKkgoKCgoKUgoKUgpSUgtiCgpSCgsiCgsqSAAkIggAEEAAICIKEgoKCAAIWAAgCpIKCgoKUgqSCgoKUgqSAgqSAgsakpKiygoKWgoKWAAIQ0oKCqJKCggACEAAIAoKChICUgoKUuIKCgoKUgpassoKCgq7CgoIACArCgoKClIKUlKSCpIKkpIK6spSAgoKC")]
[assembly: global::go.GoPositionMap("go/types/exprstring.go", "exprstring.cs", "ABAmsoKCAAsMAAkMpLa2goLItoKCtoKCgpS2goK2goLWgoKCgraCgoKUgoKUgoKCpraCgoK2goKCgpS2graCtoKCgoK2goKUgraCgraCtoKCtoKCgraClKSkpILIooKChIKClJaClIKogoKmooKCqJaAgoK4gpa8soKClLiigoKU")]
[assembly: global::go.GoPositionMap("go/types/format.go", "format.cs", "ABQkoqKUtLS0gsa0goKCgrS0goKCgrSCgoKCgoKUlIK0goKCgoKClJSCpJSowoKCgoKUprIABRDCprSCgoKCpoKUlKrCgpSEgoKClISCupKClJKmgpQ=")]
[assembly: global::go.GoPositionMap("go/types/gcsizes.go", "gcsizes.cs", "AA4e0oK6qKQACRCcgrKAgrbMgqbS5qSUgqaClIKU5oKCgoKUgqaCgoKAgpS2pqKkgoKCgIK2gsaCgpaCkpSCmKKClKSkgoKUgoKCgpiilriCxMSssoKU")]
[assembly: global::go.GoPositionMap("go/types/index.go", "index.cs", "ABQm8paUgqiEgqKUpoCU2oKCqIKClIKCgsqCyIKCgpS2gIKCgoLYgoK2goKClIKChIKCgqaSlqKEkoKSlIKCxoKCgsaAgoLWtIKkgpSUgoK4gqaCpoKUpoKCgoKUgoKUgoKCqIKCuJSCgoKWgoKCzIKWgtbCgoKCloKClIKCpoKCgoKUgoKUgoK4gtqCgoKCgpS2gIKCgti4goKClpaCgoKYkoKCmoKylICC2LbUuoKCgoK4goIACBTSgoKUlJTe8oKEgoKCloKWgpaCgoKCqKaigqiCgqiCgpaUgoKogoKo3uKCkpSCgoCCgIKCgpS2pJS4goKUlIKCmJKClA==")]
[assembly: global::go.GoPositionMap("go/types/infer.go", "infer.cs", "ABpEABQKgpKogoKSuoKWloK6goLMgoKCABw+goLMhLSCuIKCgoKmgoKmlMqCgpSmqrqCgpaCypSCpoKCgrQAChbKgoK8ggAQJIKCgoKUloKCgoK6lAABGAANCILM9gAAFgAKAoKApILcgqiCgryCnLKCgoKCgpSCgpSCgoKUlMqCgoIACRiCgoIAGTaKsoKCqIKCyoKCgoKAAAwagIKkgoK23oKCgoKo2JKCgqYAAhQAJDiCloKCgoKWgoKW2rSClKSkqJKCgpSUgoKsssgABxDkgIKkgoKWyqampqYAAhAAAhSmggANAoKmqqamgoLatqbmgoKCpq7igoKCgoKClIKCgpSUgoKUlIDKpAACFPKSggAJFMKCuICCgJTqlIKEAAQQtra2vuKUgsiCyIKUgsiCtraCyICC6PiCgryigoKm")]
[assembly: global::go.GoPositionMap("go/types/initorder.go", "initorder.cs", "ABYk1qiShIKCgoKUgIKCgoKmyISCgoKCpoQABxKClISCuoIACBKCAAcSgoKogoKC3oKUhIKClIKWgoKCgpS+soKUhIKClICCuKiSloKCloKUgoKmggAaOKLKgoKUrMSClICC7pSUgIKCgsqSgoCClAANHJamlKaUgoKmpoK6goKWAAQUoKSCgoKmgpaCgoK6poKmgoKCgoI=")]
[assembly: global::go.GoPositionMap("go/types/instantiate.go", "instantiate.cs", "ABpuAB4CgoKUhIKCgoKUgJK4ggACHAAPDNKClIKUqIKCzLKClLqCgIK4lLaCloSSlIKWpoSEkpSClIiykpiCyOissoKUtLSmgoKClqaCgpTKgoKCpgAGEgAJAoKCgpSAgqaCgpaCgoKCgpSUlKiCzIKCuoKClKiAkoKUuJKCuJK4pJKUgpSUgpS6gsyClIKUlJiSgriCkoKCppSUgoKUtLS0lJaqopSCgtiCguiCtg==")]
[assembly: global::go.GoPositionMap("go/types/interface.go", "interface.cs", "ABs6sAAHFNKCgpQAAhLigqiCgoCCyoSCgoSosoKClK7CqJCooKaQrMCmkKaQqKCmkKaQqKCmkAACEgAJAoKUgqaAooCqwoKC5tKCgoKUloKCgrqCgoKWgoKCgpSogoKAgqSYkoKAgraEgoK6hJSCqMyC")]
[assembly: global::go.GoPositionMap("go/types/labels.go", "labels.cs", "ABIcxITMgoKCgoCCgoKUgqSoooKAggAMHNKCgpSCgoKUqqKCgIK2qqKCgIK2rNKEAAUQgoKWgoKCgriWtpaCwpSAgtqAkoKAgoKCgoKmgraCgpSCgoIACRaCpoKktoKYgpqCgMLU1oKCvIKAoqTWgoK4lIK4gqiCgsaCyLaCgsi2tra2tra4gpY=")]
[assembly: global::go.GoPositionMap("go/types/lookup.go", "lookup.cs", "AA5eAB4CgpSqAA0QgIKAgoKAgqTIjMKCgIKCgILYAAIiABQGgsyWgoCCyo7mgpaCqICCgNykloCmgoKUgoLIlpKCgoKClIKCAAoWgrjcgJKCgoKUgtrcgJSCtpaWAAocsoKWgoKCgIKmgoK2ppSAgqaCgqgACRSCgoKmgoKmpoKCgoKmgpSCAAIYAAkCAAIYAAwCgoKWAAcWgoKEgIKCooSCgpSCloKCuKKWgpS0tIKCgpKCptiogoKCqIKWgoLKgpaCpoKmlJS0tNaSpLSSppQABxCClLSkpKS4poKCqJKCgIKkqsKCgoKUgoKCggACEgALCIKmggACEAAKCIKUrLKAlIKClJSkqqKAgoCCxqqigoKqooKCgriqooKCgrg=")]
[assembly: global::go.GoPositionMap("go/types/map.go", "map.cs", "AA4gkqiQppCkgKKA")]
[assembly: global::go.GoPositionMap("go/types/methodset.go", "methodset.cs", "ABgsgoKWgoKClIKokKaQprKCloKCgpSCgoKmAAYaAA8WgIKolJaCqI7mgoaShIKogIKA3KSEgriUgoKUzIK42tyigJSClIKU3IKAgoKUuJaCmJKCgoK4lgAEFrKClIKUpqKClJTKgIKCtoI=")]
[assembly: global::go.GoPositionMap("go/types/mono.go", "mono.cs", "AFCsAf6CgoSCgqiCgpaCgoKCloLKooLMgoKCzIKqgoKEgoKChJS0tMaqooKUqqKCgoKUugALEoKogoKClpqikrS2graAgqaCgsjotIK0tLaSxoKCpoK0gsiqwoKCloKCloCCpqiCgoCCgIKCgpbsgpSCqLKAgqaEgIKmgpaCgoKmlA==")]
[assembly: global::go.GoPositionMap("go/types/named.go", "named.cs", "AIcBpgKygpQAAhQACwKSuoKEgpaCgoSCgoSCgoSCgpSUAAgUgoKEhIKCgoKWgtiSqqKowoKCpoKUAAISAAoChILegpSClIKUpqIACBKUgta0qqKClKrCgpSqoKjCgqiSgpSokgACHAAPAoSCloKEgoSCgpaCgoKogoKo2uaChJSCloIABxKClriCgoKClJamkpaCgpSWgqrCgoKUgpSCgr7igoKCgr6ygpSCgoK4goK4AAIQ5KaAAAI0ABoCirKYxqbmgrqChIKEgoKCgoKAlIKCpIKUgsS26MqClJamsoK4gJS2qJKClOrigoKCgpKCqIKCloKEppaUuoKChIKCgpS4gIKAuIKCgoKCgoKUgpaS2t7CgIKk")]
[assembly: global::go.GoPositionMap("go/types/object.go", "object.cs", "ADqIAYKCqsKC7qaClAAdNIKUpKTMooKUqqCmkKigppCmkKqwppCkgKKAooCigKSgooCigICigICigKS0gtyCpoKmAAIQAAgCgqiClIKogoKCqIKUgpYAChqiqqAACBSiqJCkAAkc8qqigoKospSqsgABEKSktAATHqKokqyyqqCmkKaQAAISAAkCgpSmAA0aooKC7qiSggAJFKqigoKssAACEgAJAoKUrsCm2oCCggAHEKYACxSSAAkUggAPEKKChJSCgIKkpraCgoLIgpTIgoKClKaCtoK2gramloKUhIKWgpikoraCgoCSxIK47oKCloKmooKUgoKUlIKUrLKCgqaAooCigKKAooCigKKAooCkooKCgIKCgMqUpIKktg==")]
[assembly: global::go.GoPositionMap("go/types/objset.go", "objset.cs", "ABAwwoKAgqSClII=")]
[assembly: global::go.GoPositionMap("go/types/operand.go", "operand.cs", "AECKAbSClAAKTgAlBIKClKSk2IKohIKClJSkpMqCgqiCyqKCgoKU2paCgIKCyoKCgoKUlIKCgIKClILIuoKWpoKokoKUpKSkpKSmgoKCgpSCgqiSgqQABhQACgKCloKCloKWgoKCloKCpoKCyoKmgt6CzICCgriClILKgIKUgpTugIKAgtqClpKCgoKUzIKCgqKClIKCgpSUzIKSgoKygpSCgoKClJSW")]
[assembly: global::go.GoPositionMap("go/types/package.go", "package.cs", "ABg4ooKokKaQppCswKzigpSqoKaQAAIWAAkAqKCkgg==")]
[assembly: global::go.GoPositionMap("go/types/pointer.go", "pointer.cs", "AA4gkKaQpICigA==")]
[assembly: global::go.GoPositionMap("go/types/predicates.go", "predicates.cs", "AAwakKywooCigKKAooCigKKAooCigKqyggACEtCigKKAooCigKKAooCswoCCpKyytKSssqSkrNaCqqKqxoKokoKokqiSgq7CgIKCpKzEgIKkgqiSqqKClIKUhKjEpKKCgpSmpIKClJSkgpSCgpTaqJKk9KSYqNSCpgAIEqIACBSygoSCloKWmoCy3IDG2oCSAAUQgMKCgoKIpuyAktyAooKCgoKCuOqCggABEtKagoTGgoSCgpSEgpaCgoKogpaqgKaCgoIABRaA8oKCgpSClIKCABYugoKClJSCgpSCgoKm7ICS3ICi3oDogoKClIKCpgAMFMaopKyygpaCgqis1oCClKSkpKSkxq7mgpSUgpSUqNKS")]
[assembly: global::go.GoPositionMap("go/types/resolver.go", "resolver.cs", "ACJIoqiSgoKClN4ACAKCgoKWgpaSxpSCuMiC6IKCgpSClIKCgqbq0qiCgrqCgpaCgqiSgoCCpNb8goKCqIKClIKClpKAgqKCgoKmgoLqgoKUgoKmgoCCpICCpKa6griClKgADQzSAAAQ4oKWjIKmupKAgqSCgrqEspaSlIKCgpaCgpiCkoKUgqiCgpyygpaClJSWlJiSlIKmzNyAgoKCgpSC/siShIKCloLIisK4mJKChIKEkoKUlsaCtIKCgpSCpoKCgpSCgpSAlLaUgpSUpgAHFLiClJSCioIACQyCooCCgoKAgoKUlKQACBKClIKClIKCggAHFAAJAojClLSCxOqkgoKCgoKU6NSkgpTsgIKmrgALDIKCppaAlIKUgqiSlLyAxqKCgsqAgpLYgta6goKogoKogrqCgqiCgpS6xIKCgoKUloKAgrgAECKCAAIQ0pSCgIKClKbIgqaCAAYQzICigKKApsSCzoKCygAIDoKCgIKkgpQAAxDCgIK2")]
[assembly: global::go.GoPositionMap("go/types/return.go", "return.cs", "AAwisqQADxC4gJLYpoLIpobIpqaCgoKopoK4ppSCgIK2pqKCgoKClIKmAAIU8qQAEBC2goKUgtqmhMimgsiCyKaCyILIgrimgoKCpg==")]
[assembly: global::go.GoPositionMap("go/types/scope.go", "scope.cs", "ACBKwpSCgpSokKaQppKCgoKClIKokKaQqKIACBKClAACGgAMApKAgrYAAhDygoCCpIKClAACFAAJAoKUgqaCgpQAAhIACAKCgqKCgoCCuIKCgoKmgoKChISCrsCigKqyAAIQAAkGgoKAgsqCgoKmlAACEPKChISCgpaCgqiokoKCAAscooCCkoSAgqSCloKUlqSqoKKAooCigKKAooCigKKAooCigKKAooCigKKAooCigKKA")]
[assembly: global::go.GoPositionMap("go/types/selection.go", "selection.cs", "ACuoAZCmkKigqKKYkpKCgq6SkoKC4oKUgrgAAhoACgAAAhLwpIAABxYACwKClKSkpKSCgoKCgoCCgpSk")]
[assembly: global::go.GoPositionMap("go/types/signature.go", "signature.cs", "ACJO4gACEgAIAoKCgpSCgIK2goKClJSCgpSUAAIS4KaQppCmkKaQppCkgKKAAAkMAAsCgoKCgoS4gqaCggAHEIKCgpSqosqAgsiCgoKCuP6CuoK4ggAJFoKCgoKCgoKCgpaIsra2grSkqKSCgoLKmKKClIKClIKmkua4pKLGxMqCggAJCvKClpKCgoCCgoKUyKaUgoKmgoKUpoKCgqiC3oKCgpY=")]
[assembly: global::go.GoPositionMap("go/types/sizes.go", "sizes.cs", "ADFq0oK6qKQACRCcgrKAgrbMgqbS5qSUgqaClIKU5oKCgpSCrIKCgoKUgqaCgoKAgpS2ABImoqSCgoKAgraCxoKCloKSlIKWgpKCloSygpSkpIKClIKCgoKUqILExAAYPuKUgILGgILG7IKCgpSAgqSmooKUgoKUlIKmAAIQ0oKCgoKClIKClJSqooKClKyygg==")]
[assembly: global::go.GoPositionMap("go/types/slice.go", "slice.cs", "AA4gkKaQpICigA==")]
[assembly: global::go.GoPositionMap("go/types/stmt.go", "stmt.cs", "ABgk4oKWgrqCgpTchISCloK61qKCooKAgraWgpamggAMLIKCuIKCgIK2poKCgoKCgoKU6KKCgoKUgsaC1qSCgpTcgoKCpoKmlIKU5oKCgoKUgoKCtoKkpKSopILclICCpICCxoCCxqQADyKigoKCgoKUgoKmkoKClIKmgKaCgoKCgoKm/KSAgoKkAAIQAAgCgoKUgoKUgoLKgpSCgpSCgoKCpoKCxgAdWgAvBIKUgIKkupaE6raCzLKCgoIAARCCpIIAAhOClIIAAhC2koKCgpSCgoKUgoKClIKClLaClKSkgqaCgoKUgoKWgoKClLaUgoKUgqa6koKUgoKClIKCgpTYtraG2IKAgoKCgtqCgpTIgoKUlIK2graCgpS0tLS22IKEtoKEgoKCgpSG6OTogoKEgoKCpoKCgsqCgoKWhIKCgoKClIKCgoKUlILIgoKEAAAUAAgCgpS0goKWgoKClpSClJbGgpiCkoKWgoKCgoKCpJS6hIKCgoKCgqaClNyCgoKmgoKAgqSCuJSCmJKCgoKUlILahISCgoKogoK0tILGuIKAgriCgpaCgpSCyIKChIKCgoKCpoaAoriktoLGAAwI1IaCgoKCmpKGopSGlLS0tLS0uoK8goSEhJKCgpiSgJSCgpSCpoKkloKUgoKWgoKUgoKCgpSogoKCttiCgqiCgpaCgriCpoKCgoIAChSWABAQAAoChoKCloKUpIKUgoKUxqSkpIKUpIKUgpSkpKSCgpSkpIKUgpSU")]
[assembly: global::go.GoPositionMap("go/types/struct.go", "struct.cs", "ABc2woKCgqaClIKCqJCmkKaSgpSmgKKAqqKCAAkIwoKCgpiShpaSgoKClIKWgoKUgoIABhCSgoKWgoKClILugoKCgoKCgpQABRCChKKClJSWksa0uIKUgu6CgqaClKaAktakpJSmooCCgoKCgqSmooKCgIK2lA==")]
[assembly: global::go.GoPositionMap("go/types/subst.go", "subst.cs", "AA8oooKCgpSqooKCgpSmgqaCgIKkAAIU8oSCqJSkqO4AChaClgAGEIKCwpiSnoLCyIKCyIKCyICCgoLYgoLIAAIghIKCAA0CAA0agrjIgoKCgoKCgoIADRyWkpTIgoKCyIKCAAQSgoLSloKeggAICsi2pqyygpSmooKAgramopKCgqaigoCCtqaygoKAgqaCgoKUtqaigoCCtqaikoKCprKCgoCCpoKCgpS2prKCgoCCpoKCgpS2prKCgoCCpoKCgpS2AAISAAkCgoKCgriCgpSSgqY=")]
[assembly: global::go.GoPositionMap("go/types/termlist.go", "termlist.cs", "ABQ2koKUgoKClJSoyIKCpqjIgoKmqLaCgqKClIKCgpSA7oKUgraUqJKokoKaooKCgILIqKSokoKCpqiSgoKmqJKCqIKCpg==")]
[assembly: global::go.GoPositionMap("go/types/tuple.go", "tuple.cs", "ABIksoKUqLKClKiQpICigA==")]
[assembly: global::go.GoPositionMap("go/types/typelists.go", "typelists.cs", "AA4eoKaQqtKClP6SgpSqoKaQqtKClKyigpSCgpSU")]
[assembly: global::go.GoPositionMap("go/types/typeparam.go", "typeparam.cs", "ABMmoAALIuKo1JKCgpSCgriCpJSokKiiqJIAAhLigpSmrsKmgKqigqiShpKUlMaUlLiCgoKolJSAgqSWrLKssg==")]
[assembly: global::go.GoPositionMap("go/types/typeset.go", "typeset.cs", "ACJIkKaQppCmkoKUvJCooKaS1oKUpKaChIKCgoKCpoKClJSClIKUgq7AprCu0oKUgoKCpqyygpSClIKClIKUgqYACg7iggALGoKWuIKWgoKCggAHEoSCgoKUlAANIIKCgpKApIK0goKCggAEENKigoKCggAIDoKogoKGooKUgoKmgpSSlIKClLSClIKClIKCxIKUgpTcloKCgpSEAAUS4qaClIKCgpKCpoKCpoKmgqaCgpSC3oCigKKAAAgSwoCCuISCooKCgJSCpJSmlMiCgoKUgqaE")]
[assembly: global::go.GoPositionMap("go/types/typestring.go", "typestring.cs", "ABVGooKUgoKUvrKCgqyyrLIADRyCpoKCpoKCgpSClIKCuIKmgoKUAAwGwoKClIKElLqigIKCtraCgoK2graCgoK6gpSCzIKClIKCgoKUgIK4traCtraCuqKClIKClIKUyILKgpSCgqaCgoK4lIKCgpSCgpSCgpSCgpSCpraCgoK2goKUhICSxqSkpIKClIKCzKKUgpTGyIKClIC4lIKC3IKCuPyCgJSklNz68oKCgoKUgoKUyLSShJKClIKUgoKU2KKCgoKUlNaigoK4goKUgpSClJSClIKClKaigtaygoKCgqaCgpSCgoCCgriAgoKkgoK2pqbSgoKCgoKmloSClJaClIKo2JKCgoKCgoKCgqY=")]
[assembly: global::go.GoPositionMap("go/types/typeterm.go", "typeterm.cs", "ABQqopSkpKTK1JSkuKjUlKSkpKS4ggAGEIKUqNSUpKS4ggAGEIKUqMSUpLiCgpSo1JSkpKS4ggAGEKrCgpSCgpSCgpQ=")]
[assembly: global::go.GoPositionMap("go/types/typexpr.go", "typexpr.cs", "ABcsAAgCgqiClLiAgoKUtpSkgtqCgqa6goKmgJKkAAkWgoKClMyAgqaUgqaCgpSCgoKUlJSCtoKClLyylIKClLaCtoK2xqaqoqyygoKq1ILMgoCCgoKClAAHFuKCgoKClIKs4oKCgoKUpoLaogAICvKCgoKCgoKmlIKUusqShJSCgsi02IKElIKCyLT4goKqxoKCgoKWgoaAooKUpIKCzrK2goKCpoKCgoKmgoKCpoKCgqaChIIAABDygoKClKimgoSCuKSkuIKCtpKmgoLmooKYopS0tMQACArigoKCgpSogpaCgoKUgqiCgpaAgqaCgqiWyISCgJSCgpSU7pYACgz6gIKCgoKUgIKCyIKCgoKUloKAgoKAguyCgpSUgqqigoKCgpSCpg==")]
[assembly: global::go.GoPositionMap("go/types/under.go", "under.cs", "AAkcwoCCpAACEuKCgoKWgoKClIKCgriClJSssoKCgpaCgoKClIKClIKCgriClIKUlAACEOSCqICCgKaUpPw=")]
[assembly: global::go.GoPositionMap("go/types/unify.go", "unify.cs", "AEK2AcKCyoKCgpSUAA0qgpSkpKSkrLKmoqq0goKCgpSEgoKCgoKUgoKUgsqAooCigKrigpSA2qbuAAkEqqKAgoCCxqqigoKCgsySqqKCgpSokoKCgqauwoKClKrCgIKkrgAMAoKClIKClKiCqIKClIKU3oKClAASKICCgpSmlIIACRSAppKWuICklIKCgqa4ggAJFIL+ABUsgsrWgKTolKaSyN6mgqaCgoKCuIIAFzCCgoKUuIKUgriCgqaCgIK2qIKCzJSCgoKAgrYADB6CgpS6goKokpSagLLcgMbagJIABRCAwoKCgoim7ICS3ICigoKCgoK4AAYUgNLcjIDCgoKClIKUgoIAFi6CgoKUlIKClIKCgqbsgJLegLLcgNiCgoKUgoKm2pIAARwAEg6AgoLcAAoSxg==")]
[assembly: global::go.GoPositionMap("go/types/union.go", "union.cs", "ABYuooKUpoCigKSAooDMkKSAooCigAALFMKChISCgoK4lIKCgqaCloKogsyigoKWgoKCgoKWgoIABhCCgpS0tLS6gILK1qKCgoCCgqTcgoKUlJSCgpSu4oKCgriCgqaCpqrSgIKCgqQ=")]
[assembly: global::go.GoPositionMap("go/types/universe.go", "universe.cs", "AEuaAYKClIIAESaCgqa6goKCqIKCgpaCgoKWgoSCqIKCgpaEggAPHIKCuIIASKIBgoKCgpTusoKUgqaCgoKEgoKChIKCgoKssoKCgqaAgraCgpSUtMS2gg==")]
[assembly: global::go.GoPositionMap("go/types/util.go", "util.cs", "ABI4AAgAppCmkKawppCmkKaS")]
[assembly: global::go.GoPositionMap("go/types/validtype.go", "validtype.cs", "AAwgwgAFGgAOAoSCgJKkgoKCqJiiyKaCgtqCgtqCggAFOAAWApqiABUsgoKC3oKCgqYAAhDCAAQUgNKmpoLuuILs2qKCgpQ=")]
[assembly: global::go.GoPositionMap("go/types/version.go", "version.cs", "ABIsoqiSqqIADS7igoCC3ISs0oKClAACFAAJApSClIKCpg==")]
// </GoSourcePositionMaps>

namespace go.go;

[GoPackage("types")]
public static partial class types_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface cleaner {}
    internal partial interface decl {}
    internal partial interface dependency {}
    internal partial interface positioner {}
    internal partial struct @object {}
    internal partial struct _TypeSet {}
    internal partial struct action {}
    internal partial struct actionDesc {}
    internal partial struct atPos {}
    internal partial struct bailout {}
    internal partial struct block {}
    internal partial struct builtinId {}
    internal partial struct byUniqueMethodName {}
    [GoLocalName("methodInfo")] internal partial struct collectObjects_methodInfo {}
    internal partial struct comparer {}
    internal partial struct ctxtEntry {}
    internal partial struct cycleFinder {}
    internal partial struct declInfo {}
    internal partial struct dotImportKey {}
    internal partial struct embeddedType {}
    internal partial struct environment {}
    internal partial struct errorDesc {}
    internal partial struct error_ {}
    internal partial struct exprInfo {}
    internal partial struct exprKind {}
    internal partial struct gcSizes {}
    internal partial struct goVersion {}
    internal partial struct graphNode {}
    internal partial struct ifacePair {}
    internal partial struct importDecl {}
    internal partial struct importKey {}
    internal partial struct inSourceOrder {}
    [GoValueClone("buf")] internal partial struct instanceLookup {}
    internal partial struct lazyObject {}
    internal partial struct methodSet {}
    internal partial struct monoEdge {}
    internal partial struct monoGraph {}
    internal partial struct monoVertex {}
    internal partial struct namedState {}
    internal partial struct nodeQueue {}
    internal partial struct nodeSet {}
    internal partial struct objset {}
    internal partial struct opPredicates {}
    internal partial struct operand {}
    internal partial struct operandMode {}
    internal partial struct posSpan {}
    internal partial struct predeclaredConstsᴛ1 {}
    internal partial struct predeclaredFuncsᴛ1 {}
    internal partial struct stmtContext {}
    internal partial struct substMap {}
    internal partial struct subster {}
    internal partial struct target {}
    internal partial struct tpWalker {}
    internal partial struct typeParamsById {}
    internal partial struct typeWriter {}
    internal partial struct unifier {}
    internal partial struct unifyMode {}
    internal partial struct valueMap {}
    internal partial struct valueType {}
    public partial interface Importer {}
    public partial interface ImporterFrom {}
    public partial interface Object {}
    public partial interface Sizes {}
    public partial interface ΔType {}
    public partial interface ΔgenericType {}
    public partial struct Alias {}
    public partial struct ArgumentError {}
    public partial struct Array {}
    public partial struct Basic {}
    public partial struct BasicInfo {}
    public partial struct BasicKind {}
    public partial struct Builtin {}
    public partial struct Chan {}
    public partial struct ChanDir {}
    public partial struct Checker {}
    public partial struct Config {}
    public partial struct Const {}
    public partial struct Context {}
    public partial struct Func {}
    public partial struct ImportMode {}
    public partial struct Initializer {}
    public partial struct Instance {}
    public partial struct Interface {}
    public partial struct Label {}
    public partial struct Map {}
    public partial struct MethodSet {}
    public partial struct Named {}
    public partial struct Nil {}
    public partial struct Package {}
    public partial struct PkgName {}
    public partial struct Pointer {}
    public partial struct Selection {}
    public partial struct SelectionKind {}
    public partial struct Slice {}
    public partial struct StdSizes {}
    public partial struct Struct {}
    public partial struct Tuple {}
    public partial struct TypeAndValue {}
    public partial struct TypeList {}
    public partial struct TypeName {}
    public partial struct TypeParam {}
    public partial struct TypeParamList {}
    public partial struct Union {}
    public partial struct Var {}
    public partial struct term {}
    public partial struct ΔError {}
    public partial struct ΔInfo {}
    public partial struct ΔScope {}
    public partial struct ΔSignature {}
    public partial struct ΔTerm {}
    public partial struct Δcolor {}
    public partial struct ΔconstDecl {}
    public partial struct ΔfuncDecl {}
    public partial struct Δinstance {}
    public partial struct Δtermlist {}
    public partial struct ΔtypeDecl {}
    public partial struct ΔvarDecl {}
    // </TypeAccessibility>
}
