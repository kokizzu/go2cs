//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:26:05 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using errors = go.errors_package;
using fmt = go.fmt_package;
using filepath = go.path.filepath_package;
using sort = go.sort_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using unicode = go.unicode_package;
using lazyregexp = go.golang.org.x.mod.@internal.lazyregexp_package;
using module = go.golang.org.x.mod.module_package;
using semver = go.golang.org.x.mod.semver_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace golang.org {
namespace x {
namespace mod
{
    public static partial class modfile_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        [PromotedStruct(typeof(VersionInterval))]
        public partial struct Retract
        {
            // VersionInterval structure promotion - sourced from value copy
            private readonly ptr<VersionInterval> m_VersionIntervalRef;

            private ref VersionInterval VersionInterval_val => ref m_VersionIntervalRef.Value;

            public ref @string Low => ref m_VersionIntervalRef.Value.Low;

            public ref @string High => ref m_VersionIntervalRef.Value.High;

            // Constructors
            public Retract(NilType _)
            {
                this.m_VersionIntervalRef = new ptr<VersionInterval>(new VersionInterval(nil));
                this.Rationale = default;
                this.Syntax = default;
            }

            public Retract(VersionInterval VersionInterval = default, @string Rationale = default, ref ptr<Line> Syntax = default)
            {
                this.m_VersionIntervalRef = new ptr<VersionInterval>(VersionInterval);
                this.Rationale = Rationale;
                this.Syntax = Syntax;
            }

            // Enable comparisons between nil and Retract struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Retract value, NilType nil) => value.Equals(default(Retract));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Retract value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Retract value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Retract value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Retract(NilType nil) => default(Retract);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static Retract Retract_cast(dynamic value)
        {
            return new Retract(value.VersionInterval, value.Rationale, ref value.Syntax);
        }
    }
}}}}}}