//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:28:30 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using fmt = go.fmt_package;
using format = go.go.format_package;
using ioutil = go.io.ioutil_package;
using log = go.log_package;
using strings = go.strings_package;
using template = go.text.template_package;

#nullable enable

namespace go
{
    public static partial class main_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct fncData
        {
            // Constructors
            public fncData(NilType _)
            {
                this.Name = default;
                this.Type_ = default;
                this.Symbol = default;
                this.FNumber = default;
                this.Number = default;
            }

            public fncData(@string Name = default, @string Type_ = default, @string Symbol = default, @string FNumber = default, @string Number = default)
            {
                this.Name = Name;
                this.Type_ = Type_;
                this.Symbol = Symbol;
                this.FNumber = FNumber;
                this.Number = Number;
            }

            // Enable comparisons between nil and fncData struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(fncData value, NilType nil) => value.Equals(default(fncData));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(fncData value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, fncData value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, fncData value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator fncData(NilType nil) => default(fncData);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static fncData fncData_cast(dynamic value)
        {
            return new fncData(value.Name, value.Type_, value.Symbol, value.FNumber, value.Number);
        }
    }
}