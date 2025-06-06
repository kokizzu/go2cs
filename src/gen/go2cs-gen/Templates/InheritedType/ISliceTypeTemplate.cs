﻿using static go2cs.Symbols;

namespace go2cs.Templates.InheritedType;

internal static class ISliceTypeTemplate
{
    public static string Generate(string structName, string typeName, string targetTypeName) =>
        $$"""
        
                public {{targetTypeName}}[] Source => m_value;
                    
                public ISlice<{{targetTypeName}}> Append({{targetTypeName}}[] elems) => m_value.Append(elems);
                    
                public nint Low => ((ISlice)m_value).Low;
                
                public nint High => ((ISlice)m_value).High;
                
                public nint Capacity => ((ISlice)m_value).Capacity;
                
                public nint Available => ((ISlice)m_value).Available;
                
                public nint Length => ((IArray)m_value).Length;
                
                Array IArray.Source => ((IArray)m_value).Source!;
                
                object? IArray.this[nint index]
                {
                    get => ((IArray)m_value)[index];
                    set => ((IArray)m_value)[index] = value;
                }
                    
                public ref {{targetTypeName}} this[nint index] => ref m_value[index];
        
                public slice<{{targetTypeName}}> this[Range range] => ToSpan()[range];
                
                ISlice<{{targetTypeName}}> ISlice<{{targetTypeName}}>.this[Range range] => this[range];
                
                public slice<{{targetTypeName}}> Slice(int start, int length) => ToSpan().Slice(start, length);
                
                ISlice<{{targetTypeName}}> ISlice<{{targetTypeName}}>.Slice(int start, int length) => Slice(start, length);
                
                public slice<{{targetTypeName}}> Slice(nint start, nint length) => ToSpan().Slice((int)start, (int)length);
                
                ISlice<{{targetTypeName}}> ISlice<{{targetTypeName}}>.Slice(nint start, nint length) => Slice(start, length);
        
                public Span<{{targetTypeName}}> {{EllipsisOperator}} => ToSpan();
                
                public Span<{{targetTypeName}}> ToSpan() => m_value.ToSpan();
                
                public ISlice? Append(object[] elems) => ((ISlice)m_value).Append(elems);
                
                public IEnumerator<(nint, {{targetTypeName}})> GetEnumerator() => m_value.GetEnumerator();
                
                IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)m_value).GetEnumerator();
                
                public bool Equals(IArray<{{targetTypeName}}>? other) => m_value.Equals(other);
                
                public bool Equals(ISlice<{{targetTypeName}}>? other) => m_value.Equals(other);
                
                public object Clone() => ((ICloneable)m_value).Clone();
                
                public static {{structName}} Make(nint p1 = 0, nint p2 = -1) => new {{structName}}(p1, p2);
        
                public {{structName}}(nint length, nint capacity = -1, nint low = 0) => m_value = new {{typeName}}(length, capacity, low);
        """;
}
