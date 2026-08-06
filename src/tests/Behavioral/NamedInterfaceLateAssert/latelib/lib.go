// Package latelib is the io/fs shape in miniature: it declares the interfaces AND
// the code that asserts to them, and it is compiled BEFORE (and independently of)
// the package that supplies the concrete types — exactly as io/fs is converted
// before os, which is why fs/package_info.cs can never record os.dirFS.
package latelib

// Base, Mid and Top form a THREE-DEEP embedded chain. Top's runtime shell has to
// forward every transitively-inherited method (Name from Base, Kind from Mid,
// Detail from Top itself), so this pins the AllInterfaces walk against silent
// regression.
type Base interface {
	Name() string
}

type Mid interface {
	Base
	Kind() string
}

type Top interface {
	Mid
	Detail(prefix string) string
}

// quiet is UNEXPORTED: its generated shells are internal types with internal
// constructors, so construction must not depend on public-constructor binding.
type quiet interface {
	Whisper() string
}

func TryTop(v any) (string, bool) {
	if t, ok := v.(Top); ok {
		return t.Name() + "/" + t.Kind() + "/" + t.Detail("d"), true
	}
	return "", false
}

func TryMid(v any) (string, bool) {
	if m, ok := v.(Mid); ok {
		return m.Name() + "/" + m.Kind(), true
	}
	return "", false
}

func TryBase(v any) (string, bool) {
	if b, ok := v.(Base); ok {
		return b.Name(), true
	}
	return "", false
}

func TryQuiet(v any) (string, bool) {
	if q, ok := v.(quiet); ok {
		return q.Whisper(), true
	}
	return "", false
}
