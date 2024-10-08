//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:29:59 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bufio = go.bufio_package;
using bytes = go.bytes_package;
using heap = go.container.heap_package;
using context = go.context_package;
using elf = go.debug.elf_package;
using json = go.encoding.json_package;
using fmt = go.fmt_package;
using os = go.os_package;
using filepath = go.path.filepath_package;
using runtime = go.runtime_package;
using strings = go.strings_package;
using sync = go.sync_package;
using time = go.time_package;
using @base = go.cmd.go.@internal.@base_package;
using cache = go.cmd.go.@internal.cache_package;
using cfg = go.cmd.go.@internal.cfg_package;
using load = go.cmd.go.@internal.load_package;
using trace = go.cmd.go.@internal.trace_package;
using buildid = go.cmd.@internal.buildid_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace go {
namespace @internal
{
    public static partial class work_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct actionJSON
        {
            // Constructors
            public actionJSON(NilType _)
            {
                this.ID = default;
                this.Mode = default;
                this.Package = default;
                this.Deps = default;
                this.IgnoreFail = default;
                this.Args = default;
                this.Link = default;
                this.Objdir = default;
                this.Target = default;
                this.Priority = default;
                this.Failed = default;
                this.Built = default;
                this.VetxOnly = default;
                this.NeedVet = default;
                this.NeedBuild = default;
                this.ActionID = default;
                this.BuildID = default;
                this.TimeReady = default;
                this.TimeStart = default;
                this.TimeDone = default;
                this.Cmd = default;
                this.CmdReal = default;
                this.CmdUser = default;
                this.CmdSys = default;
            }

            public actionJSON(nint ID = default, @string Mode = default, @string Package = default, slice<nint> Deps = default, bool IgnoreFail = default, slice<@string> Args = default, bool Link = default, @string Objdir = default, @string Target = default, nint Priority = default, bool Failed = default, @string Built = default, bool VetxOnly = default, bool NeedVet = default, bool NeedBuild = default, @string ActionID = default, @string BuildID = default, time.Time TimeReady = default, time.Time TimeStart = default, time.Time TimeDone = default, slice<@string> Cmd = default, time.Duration CmdReal = default, time.Duration CmdUser = default, time.Duration CmdSys = default)
            {
                this.ID = ID;
                this.Mode = Mode;
                this.Package = Package;
                this.Deps = Deps;
                this.IgnoreFail = IgnoreFail;
                this.Args = Args;
                this.Link = Link;
                this.Objdir = Objdir;
                this.Target = Target;
                this.Priority = Priority;
                this.Failed = Failed;
                this.Built = Built;
                this.VetxOnly = VetxOnly;
                this.NeedVet = NeedVet;
                this.NeedBuild = NeedBuild;
                this.ActionID = ActionID;
                this.BuildID = BuildID;
                this.TimeReady = TimeReady;
                this.TimeStart = TimeStart;
                this.TimeDone = TimeDone;
                this.Cmd = Cmd;
                this.CmdReal = CmdReal;
                this.CmdUser = CmdUser;
                this.CmdSys = CmdSys;
            }

            // Enable comparisons between nil and actionJSON struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(actionJSON value, NilType nil) => value.Equals(default(actionJSON));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(actionJSON value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, actionJSON value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, actionJSON value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator actionJSON(NilType nil) => default(actionJSON);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static actionJSON actionJSON_cast(dynamic value)
        {
            return new actionJSON(value.ID, value.Mode, value.Package, value.Deps, value.IgnoreFail, value.Args, value.Link, value.Objdir, value.Target, value.Priority, value.Failed, value.Built, value.VetxOnly, value.NeedVet, value.NeedBuild, value.ActionID, value.BuildID, value.TimeReady, value.TimeStart, value.TimeDone, value.Cmd, value.CmdReal, value.CmdUser, value.CmdSys);
        }
    }
}}}}