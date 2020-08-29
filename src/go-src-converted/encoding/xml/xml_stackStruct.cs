//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:36:12 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using bufio = go.bufio_package;
using bytes = go.bytes_package;
using errors = go.errors_package;
using fmt = go.fmt_package;
using io = go.io_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using unicode = go.unicode_package;
using utf8 = go.unicode.utf8_package;
using go;

namespace go {
namespace encoding
{
    public static partial class xml_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct stack
        {
            // Constructors
            public stack(NilType _)
            {
                this.next = default;
                this.kind = default;
                this.name = default;
                this.ok = default;
            }

            public stack(ref ptr<stack> next = default, long kind = default, Name name = default, bool ok = default)
            {
                this.next = next;
                this.kind = kind;
                this.name = name;
                this.ok = ok;
            }

            // Enable comparisons between nil and stack struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(stack value, NilType nil) => value.Equals(default(stack));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(stack value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, stack value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, stack value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator stack(NilType nil) => default(stack);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static stack stack_cast(dynamic value)
        {
            return new stack(ref value.next, value.kind, value.name, value.ok);
        }
    }
}}