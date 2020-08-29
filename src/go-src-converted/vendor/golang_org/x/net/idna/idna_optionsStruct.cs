//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 10:11:47 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using fmt = go.fmt_package;
using strings = go.strings_package;
using utf8 = go.unicode.utf8_package;
using bidirule = go.golang_org.x.text.secure.bidirule_package;
using bidi = go.golang_org.x.text.unicode.bidi_package;
using norm = go.golang_org.x.text.unicode.norm_package;
using go;

namespace go {
namespace vendor {
namespace golang_org {
namespace x {
namespace net
{
    public static partial class idna_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct options
        {
            // Constructors
            public options(NilType _)
            {
                this.transitional = default;
                this.useSTD3Rules = default;
                this.validateLabels = default;
                this.verifyDNSLength = default;
                this.removeLeadingDots = default;
                this.trie = default;
                this.fromPuny = default;
                this.mapping = default;
                this.bidirule = default;
            }

            public options(bool transitional = default, bool useSTD3Rules = default, bool validateLabels = default, bool verifyDNSLength = default, bool removeLeadingDots = default, ref ptr<idnaTrie> trie = default, Func<ref Profile, @string, error> fromPuny = default, Func<ref Profile, @string, (@string, bool, error)> mapping = default, Func<@string, bool> bidirule = default)
            {
                this.transitional = transitional;
                this.useSTD3Rules = useSTD3Rules;
                this.validateLabels = validateLabels;
                this.verifyDNSLength = verifyDNSLength;
                this.removeLeadingDots = removeLeadingDots;
                this.trie = trie;
                this.fromPuny = fromPuny;
                this.mapping = mapping;
                this.bidirule = bidirule;
            }

            // Enable comparisons between nil and options struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(options value, NilType nil) => value.Equals(default(options));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(options value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, options value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, options value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator options(NilType nil) => default(options);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static options options_cast(dynamic value)
        {
            return new options(value.transitional, value.useSTD3Rules, value.validateLabels, value.verifyDNSLength, value.removeLeadingDots, ref value.trie, value.fromPuny, value.mapping, value.bidirule);
        }
    }
}}}}}