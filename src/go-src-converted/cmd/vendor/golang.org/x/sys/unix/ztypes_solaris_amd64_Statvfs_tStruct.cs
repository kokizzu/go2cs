//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 06:00:47 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace golang.org {
namespace x {
namespace sys
{
    public static partial class unix_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct Statvfs_t
        {
            // Constructors
            public Statvfs_t(NilType _)
            {
                this.Bsize = default;
                this.Frsize = default;
                this.Blocks = default;
                this.Bfree = default;
                this.Bavail = default;
                this.Files = default;
                this.Ffree = default;
                this.Favail = default;
                this.Fsid = default;
                this.Basetype = default;
                this.Flag = default;
                this.Namemax = default;
                this.Fstr = default;
            }

            public Statvfs_t(ulong Bsize = default, ulong Frsize = default, ulong Blocks = default, ulong Bfree = default, ulong Bavail = default, ulong Files = default, ulong Ffree = default, ulong Favail = default, ulong Fsid = default, array<sbyte> Basetype = default, ulong Flag = default, ulong Namemax = default, array<sbyte> Fstr = default)
            {
                this.Bsize = Bsize;
                this.Frsize = Frsize;
                this.Blocks = Blocks;
                this.Bfree = Bfree;
                this.Bavail = Bavail;
                this.Files = Files;
                this.Ffree = Ffree;
                this.Favail = Favail;
                this.Fsid = Fsid;
                this.Basetype = Basetype;
                this.Flag = Flag;
                this.Namemax = Namemax;
                this.Fstr = Fstr;
            }

            // Enable comparisons between nil and Statvfs_t struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Statvfs_t value, NilType nil) => value.Equals(default(Statvfs_t));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Statvfs_t value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Statvfs_t value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Statvfs_t value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Statvfs_t(NilType nil) => default(Statvfs_t);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static Statvfs_t Statvfs_t_cast(dynamic value)
        {
            return new Statvfs_t(value.Bsize, value.Frsize, value.Blocks, value.Bfree, value.Bavail, value.Files, value.Ffree, value.Favail, value.Fsid, value.Basetype, value.Flag, value.Namemax, value.Fstr);
        }
    }
}}}}}}