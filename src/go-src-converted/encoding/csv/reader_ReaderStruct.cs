//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:39:27 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bufio = go.bufio_package;
using bytes = go.bytes_package;
using errors = go.errors_package;
using fmt = go.fmt_package;
using io = go.io_package;
using unicode = go.unicode_package;
using utf8 = go.unicode.utf8_package;
using go;

#nullable enable

namespace go {
namespace encoding
{
    public static partial class csv_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Reader
        {
            // Constructors
            public Reader(NilType _)
            {
                this.Comma = default;
                this.Comment = default;
                this.FieldsPerRecord = default;
                this.LazyQuotes = default;
                this.TrimLeadingSpace = default;
                this.ReuseRecord = default;
                this.TrailingComma = default;
                this.r = default;
                this.numLine = default;
                this.rawBuffer = default;
                this.recordBuffer = default;
                this.fieldIndexes = default;
                this.fieldPositions = default;
                this.lastRecord = default;
            }

            public Reader(int Comma = default, int Comment = default, nint FieldsPerRecord = default, bool LazyQuotes = default, bool TrimLeadingSpace = default, bool ReuseRecord = default, bool TrailingComma = default, ref ptr<bufio.Reader> r = default, nint numLine = default, slice<byte> rawBuffer = default, slice<byte> recordBuffer = default, slice<nint> fieldIndexes = default, slice<position> fieldPositions = default, slice<@string> lastRecord = default)
            {
                this.Comma = Comma;
                this.Comment = Comment;
                this.FieldsPerRecord = FieldsPerRecord;
                this.LazyQuotes = LazyQuotes;
                this.TrimLeadingSpace = TrimLeadingSpace;
                this.ReuseRecord = ReuseRecord;
                this.TrailingComma = TrailingComma;
                this.r = r;
                this.numLine = numLine;
                this.rawBuffer = rawBuffer;
                this.recordBuffer = recordBuffer;
                this.fieldIndexes = fieldIndexes;
                this.fieldPositions = fieldPositions;
                this.lastRecord = lastRecord;
            }

            // Enable comparisons between nil and Reader struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Reader value, NilType nil) => value.Equals(default(Reader));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Reader value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Reader value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Reader value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Reader(NilType nil) => default(Reader);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Reader Reader_cast(dynamic value)
        {
            return new Reader(value.Comma, value.Comment, value.FieldsPerRecord, value.LazyQuotes, value.TrimLeadingSpace, value.ReuseRecord, value.TrailingComma, ref value.r, value.numLine, value.rawBuffer, value.recordBuffer, value.fieldIndexes, value.fieldPositions, value.lastRecord);
        }
    }
}}