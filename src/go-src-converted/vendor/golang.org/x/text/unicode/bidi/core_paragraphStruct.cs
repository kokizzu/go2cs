//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:46:41 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using log = go.log_package;
using go;

#nullable enable

namespace go {
namespace vendor {
namespace golang.org {
namespace x {
namespace text {
namespace unicode
{
    public static partial class bidi_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct paragraph
        {
            // Constructors
            public paragraph(NilType _)
            {
                this.initialTypes = default;
                this.pairTypes = default;
                this.pairValues = default;
                this.embeddingLevel = default;
                this.resultTypes = default;
                this.resultLevels = default;
                this.matchingPDI = default;
                this.matchingIsolateInitiator = default;
            }

            public paragraph(slice<Class> initialTypes = default, slice<bracketType> pairTypes = default, slice<int> pairValues = default, level embeddingLevel = default, slice<Class> resultTypes = default, slice<level> resultLevels = default, slice<nint> matchingPDI = default, slice<nint> matchingIsolateInitiator = default)
            {
                this.initialTypes = initialTypes;
                this.pairTypes = pairTypes;
                this.pairValues = pairValues;
                this.embeddingLevel = embeddingLevel;
                this.resultTypes = resultTypes;
                this.resultLevels = resultLevels;
                this.matchingPDI = matchingPDI;
                this.matchingIsolateInitiator = matchingIsolateInitiator;
            }

            // Enable comparisons between nil and paragraph struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(paragraph value, NilType nil) => value.Equals(default(paragraph));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(paragraph value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, paragraph value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, paragraph value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator paragraph(NilType nil) => default(paragraph);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static paragraph paragraph_cast(dynamic value)
        {
            return new paragraph(value.initialTypes, value.pairTypes, value.pairValues, value.embeddingLevel, value.resultTypes, value.resultLevels, value.matchingPDI, value.matchingIsolateInitiator);
        }
    }
}}}}}}