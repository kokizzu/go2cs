//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:47:04 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using binary = go.encoding.binary_package;
using go;

#nullable enable

namespace go {
namespace vendor {
namespace golang.org {
namespace x {
namespace text {
namespace unicode
{
    public static partial class norm_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct formInfo
        {
            // Constructors
            public formInfo(NilType _)
            {
                this.form = default;
                this.composing = default;
                this.compatibility = default;
                this.info = default;
                this.nextMain = default;
            }

            public formInfo(Form form = default, bool composing = default, bool compatibility = default, lookupFunc info = default, iterFunc nextMain = default)
            {
                this.form = form;
                this.composing = composing;
                this.compatibility = compatibility;
                this.info = info;
                this.nextMain = nextMain;
            }

            // Enable comparisons between nil and formInfo struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(formInfo value, NilType nil) => value.Equals(default(formInfo));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(formInfo value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, formInfo value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, formInfo value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator formInfo(NilType nil) => default(formInfo);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static formInfo formInfo_cast(dynamic value)
        {
            return new formInfo(value.form, value.composing, value.compatibility, value.info, value.nextMain);
        }
    }
}}}}}}