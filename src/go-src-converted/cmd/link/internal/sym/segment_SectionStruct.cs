//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:33:29 UTC
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
namespace link {
namespace @internal
{
    public static partial class sym_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Section
        {
            // Constructors
            public Section(NilType _)
            {
                this.Rwx = default;
                this.Extnum = default;
                this.Align = default;
                this.Name = default;
                this.Vaddr = default;
                this.Length = default;
                this.Seg = default;
                this.Reloff = default;
                this.Rellen = default;
                this.Relcount = default;
                this.Sym = default;
                this.Index = default;
            }

            public Section(byte Rwx = default, short Extnum = default, int Align = default, @string Name = default, ulong Vaddr = default, ulong Length = default, ref ptr<Segment> Seg = default, ulong Reloff = default, ulong Rellen = default, uint Relcount = default, LoaderSym Sym = default, ushort Index = default)
            {
                this.Rwx = Rwx;
                this.Extnum = Extnum;
                this.Align = Align;
                this.Name = Name;
                this.Vaddr = Vaddr;
                this.Length = Length;
                this.Seg = Seg;
                this.Reloff = Reloff;
                this.Rellen = Rellen;
                this.Relcount = Relcount;
                this.Sym = Sym;
                this.Index = Index;
            }

            // Enable comparisons between nil and Section struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Section value, NilType nil) => value.Equals(default(Section));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Section value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Section value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Section value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Section(NilType nil) => default(Section);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Section Section_cast(dynamic value)
        {
            return new Section(value.Rwx, value.Extnum, value.Align, value.Name, value.Vaddr, value.Length, ref value.Seg, value.Reloff, value.Rellen, value.Relcount, value.Sym, value.Index);
        }
    }
}}}}