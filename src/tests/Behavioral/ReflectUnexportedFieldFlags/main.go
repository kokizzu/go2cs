package main

import (
	"fmt"
	"reflect"
)

// The read-only half of the reflection bridge's struct-field descriptor, from BOTH sides.
//
// reflect.StructField.IsExported() is nothing but `PkgPath == ""`, and the bridge left PkgPath
// unpopulated — so it answered TRUE for every field of every converted struct. Silently, because
// "" is the correct answer for most fields. The consequence is a guard that can never fire:
// encoding/asn1 opens both its struct arms with `if !t.Field(i).IsExported() { return
// StructuralError{"struct contains unexported fields"} }`, so Marshal returned a nil error where
// Go returns that error and Unmarshal ran on to write through the unexported field and panicked.
//
// The two halves of the model degraded INDEPENDENTLY: reflect.Value.Field already stamped the
// read-only flag (which is why the write panicked rather than silently succeeding) while the
// type-side descriptor had no answer at all. Both must read exportedness from one projection, so
// a PROBE of the type and a write through the value can never disagree about a field — that
// agreement is what the second loop below asserts field for field.

type mixed struct {
	Exported   int
	unexported int
	_          int
	Tagged     string `probe:"yes"`
	secret     string
}

type allExported struct {
	A int
	B int
}

func main() {
	t := reflect.TypeOf(mixed{})

	for i := 0; i < t.NumField(); i++ {
		f := t.Field(i)
		fmt.Printf("%d %-10s exported=%-5v pkgpath=%q tag=%q\n", i, f.Name, f.IsExported(), f.PkgPath, string(f.Tag))
	}

	// FieldByName carries the same flags as the indexed walk (it composes it).
	f, ok := t.FieldByName("secret")
	fmt.Println("byname secret:", ok, f.IsExported(), f.PkgPath)
	f, ok = t.FieldByName("Exported")
	fmt.Println("byname Exported:", ok, f.IsExported(), f.PkgPath)
	_, ok = t.FieldByName("absent")
	fmt.Println("byname absent:", ok)

	// The value side must AGREE with the type side, field for field: a field the type calls
	// unexported is a field the value refuses to set and refuses to hand out.
	var m mixed
	v := reflect.ValueOf(&m).Elem()
	for i := 0; i < v.NumField(); i++ {
		fmt.Printf("%d canset=%-5v caninterface=%-5v typeExported=%-5v agree=%v\n",
			i, v.Field(i).CanSet(), v.Field(i).CanInterface(), t.Field(i).IsExported(),
			v.Field(i).CanSet() == t.Field(i).IsExported())
	}

	// The consumer shape: a decoder that PROBES settability rather than trusting it must be able
	// to refuse before it writes. Refusing is a returned error, never a panic.
	fmt.Println("decode mixed:      ", decode(&m))
	var a allExported
	fmt.Println("decode allExported:", decode(&a), a)
}

// decode mirrors encoding/asn1's guard: refuse a struct with any unexported field BEFORE writing.
func decode(p any) string {
	v := reflect.ValueOf(p).Elem()
	t := v.Type()

	for i := 0; i < t.NumField(); i++ {
		if !t.Field(i).IsExported() {
			return "structure error: struct contains unexported fields"
		}
	}

	for i := 0; i < v.NumField(); i++ {
		v.Field(i).SetInt(int64(i + 1))
	}

	return "<nil>"
}
