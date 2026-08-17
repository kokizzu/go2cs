// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.database.sql_package;
using static go.database.sql_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<Dummy, go.database.sql.driver_package.Driver>(Promoted = true)]
[assembly: GoImplement<Dummy, go.database.sql.driver_package.Driver>]
[assembly: GoImplement<anyTypeConverter, go.database.sql.driver_package.ValueConverter>]
[assembly: GoImplement<badConn, go.database.sql.driver_package.Conn>]
[assembly: GoImplement<badDriver, go.database.sql.driver_package.Driver>]
[assembly: GoImplement<concurrentDBExecTest, concurrentTest>(Pointer = true)]
[assembly: GoImplement<concurrentDBQueryTest, concurrentTest>(Pointer = true)]
[assembly: GoImplement<concurrentRandomTest, concurrentTest>(Pointer = true)]
[assembly: GoImplement<concurrentStmtExecTest, concurrentTest>(Pointer = true)]
[assembly: GoImplement<concurrentStmtQueryTest, concurrentTest>(Pointer = true)]
[assembly: GoImplement<concurrentTxExecTest, concurrentTest>(Pointer = true)]
[assembly: GoImplement<concurrentTxQueryTest, concurrentTest>(Pointer = true)]
[assembly: GoImplement<concurrentTxStmtExecTest, concurrentTest>(Pointer = true)]
[assembly: GoImplement<concurrentTxStmtQueryTest, concurrentTest>(Pointer = true)]
[assembly: GoImplement<ctxOnlyConn, go.database.sql.driver_package.Conn>(Pointer = true)]
[assembly: GoImplement<ctxOnlyConn, go.database.sql.driver_package.ExecerContext>(Pointer = true)]
[assembly: GoImplement<ctxOnlyConn, go.database.sql.driver_package.QueryerContext>(Pointer = true)]
[assembly: GoImplement<ctxOnlyDriver, go.database.sql.driver_package.Driver>(Pointer = true)]
[assembly: GoImplement<dec, global::go.database.sql_package.decimalDecompose>]
[assembly: GoImplement<decFinite, global::go.database.sql_package.decimalDecompose>]
[assembly: GoImplement<fakeConn, go.database.sql.driver_package.Conn>(Pointer = true)]
[assembly: GoImplement<fakeConn, go.database.sql.driver_package.Validator>(Pointer = true)]
[assembly: GoImplement<fakeConn, memToucher>(Pointer = true)]
[assembly: GoImplement<fakeConnector, go.database.sql.driver_package.Connector>(Pointer = true)]
[assembly: GoImplement<fakeDriver, go.database.sql.driver_package.Driver>(Pointer = true)]
[assembly: GoImplement<fakeDriverCtx, go.database.sql.driver_package.Driver>(Pointer = true)]
[assembly: GoImplement<fakeDriverCtx, go.database.sql.driver_package.DriverContext>(Pointer = true)]
[assembly: GoImplement<fakeDriverString, go.database.sql.driver_package.ValueConverter>]
[assembly: GoImplement<fakeError, error>]
[assembly: GoImplement<fakeStmt, go.database.sql.driver_package.Stmt>(Pointer = true)]
[assembly: GoImplement<fakeStmt, memToucher>(Promoted = true)]
[assembly: GoImplement<fakeTx, go.database.sql.driver_package.Tx>(Pointer = true)]
[assembly: GoImplement<nvcConn, go.database.sql.driver_package.Conn>(Pointer = true)]
[assembly: GoImplement<nvcConn, go.database.sql.driver_package.NamedValueChecker>(Pointer = true)]
[assembly: GoImplement<nvcDriver, go.database.sql.driver_package.Driver>(Pointer = true)]
[assembly: GoImplement<pingConn, go.database.sql.driver_package.Conn>]
[assembly: GoImplement<pingConn, go.database.sql.driver_package.Pinger>]
[assembly: GoImplement<pingDriver, go.database.sql.driver_package.Driver>(Pointer = true)]
[assembly: GoImplement<rowsCursor, go.database.sql.driver_package.Rows>(Pointer = true)]
[assembly: GoImplement<stubDriverStmt, go.database.sql.driver_package.Stmt>]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.database;

[GoPackage("sql")]
public static partial class sql_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
