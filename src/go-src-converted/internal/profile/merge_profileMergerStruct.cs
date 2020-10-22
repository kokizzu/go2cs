//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 04:59:10 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using fmt = go.fmt_package;
using sort = go.sort_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using go;

#nullable enable

namespace go {
namespace @internal
{
    public static partial class profile_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct profileMerger
        {
            // Constructors
            public profileMerger(NilType _)
            {
                this.p = default;
                this.locationsByID = default;
                this.functionsByID = default;
                this.mappingsByID = default;
                this.samples = default;
                this.locations = default;
                this.functions = default;
                this.mappings = default;
            }

            public profileMerger(ref ptr<Profile> p = default, map<ulong, ptr<Location>> locationsByID = default, map<ulong, ptr<Function>> functionsByID = default, map<ulong, mapInfo> mappingsByID = default, map<sampleKey, ptr<Sample>> samples = default, map<locationKey, ptr<Location>> locations = default, map<functionKey, ptr<Function>> functions = default, map<mappingKey, ptr<Mapping>> mappings = default)
            {
                this.p = p;
                this.locationsByID = locationsByID;
                this.functionsByID = functionsByID;
                this.mappingsByID = mappingsByID;
                this.samples = samples;
                this.locations = locations;
                this.functions = functions;
                this.mappings = mappings;
            }

            // Enable comparisons between nil and profileMerger struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(profileMerger value, NilType nil) => value.Equals(default(profileMerger));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(profileMerger value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, profileMerger value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, profileMerger value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator profileMerger(NilType nil) => default(profileMerger);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static profileMerger profileMerger_cast(dynamic value)
        {
            return new profileMerger(ref value.p, value.locationsByID, value.functionsByID, value.mappingsByID, value.samples, value.locations, value.functions, value.mappings);
        }
    }
}}