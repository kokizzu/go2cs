//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 10:05:10 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using bufio = go.bufio_package;
using fmt = go.fmt_package;
using io = go.io_package;
using exec = go.os.exec_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using sync = go.sync_package;
using plugin = go.github.com.google.pprof.@internal.plugin_package;
using go;

namespace go {
namespace cmd {
namespace vendor {
namespace github.com {
namespace google {
namespace pprof {
namespace @internal
{
    public static partial class binutils_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct addr2LinerJob
        {
            // Constructors
            public addr2LinerJob(NilType _)
            {
                this.cmd = default;
                this.@in = default;
                this.@out = default;
            }

            public addr2LinerJob(ref ptr<exec.Cmd> cmd = default, io.WriteCloser @in = default, ref ptr<bufio.Reader> @out = default)
            {
                this.cmd = cmd;
                this.@in = @in;
                this.@out = @out;
            }

            // Enable comparisons between nil and addr2LinerJob struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(addr2LinerJob value, NilType nil) => value.Equals(default(addr2LinerJob));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(addr2LinerJob value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, addr2LinerJob value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, addr2LinerJob value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator addr2LinerJob(NilType nil) => default(addr2LinerJob);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static addr2LinerJob addr2LinerJob_cast(dynamic value)
        {
            return new addr2LinerJob(ref value.cmd, value.@in, ref value.@out);
        }
    }
}}}}}}}