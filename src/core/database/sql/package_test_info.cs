// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.database.sql_package;
global using static global::go.database.sql_internal_test_package;

// <ImportedTypeAliases>
global using driverꓸRowsAffected = go.database.sql.driver_package.ΔRowsAffected;
global using driverꓸValue = object;
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using httpꓸCookie = go.net.http_package.ΔCookie;
global using httpꓸHandler = go.net.http_package.ΔHandler;
global using httpꓸHeader = go.net.http_package.ΔHeader;
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
global using sqlꓸConn = go.database.sql_package.ΔConn;
global using sqlꓸStmt = go.database.sql_package.ΔStmt;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.database.sql_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Conn", "ΔConn")]
[assembly: GoTypeAlias("Stmt", "ΔStmt")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<global::go.database.sql_package.dsnConnector, go.database.sql.driver_package.Connector>]
[assembly: GoImplement<testing_package.B, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<global::go.database.sql_package.DB, ж<global::go.database.sql_package.DB>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.database.sql_package.Rows, ж<global::go.database.sql_package.Rows>>(Indirect = true)]
// </ImplicitConversions>

namespace go.database;

[GoPackage("sql_test")]
public static partial class sql_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    public partial struct ExampleDB_Prepare_projects {}
    public partial struct ExampleTx_Prepare_projects {}
    // </TypeAccessibility>
}
