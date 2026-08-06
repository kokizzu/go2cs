package main

import (
	"errors"
	"fmt"
)

// Go permits calling a method through a nil pointer: the method RUNS, and the nil-pointer panic
// happens only where the body actually dereferences the pointee. This guards that the converted
// C# agrees on all four observable consequences -- the delegated guard that never panics, the
// unconditional deref that does, the ordering of a side effect the body performs BEFORE the
// deref, and the untouched behavior of a non-nil receiver.
//
// Both C# receiver shapes are covered, because the entry alias is emitted twice: the converter
// writes it for a direct-ж receiver (`this ж<File> Ꮡf`), and go2cs-gen's RecvGenerator writes it
// in the bridge that reaches a `ref File f` receiver through a box. An eager `.Value` in EITHER
// place moves the panic to method entry, ahead of anything the body was going to do first.

var errInvalid = errors.New("invalid argument")

type File struct {
	name string
	fd   int
}

// checkValid is the DELEGATED guard: this, not the caller, is what asks whether f is nil --
// exactly the shape os.File uses for its fifteen nil-tolerant methods.
func (f *File) checkValid(op string) error {
	if f == nil {
		return errInvalid
	}
	return nil
}

// ---- Shape A: the receiver is passed on as a pointer (box receiver in C#) ----

// Delegated guard: a nil receiver must return the error, never panic.
func (f *File) Chdir() error {
	if err := f.checkValid("chdir"); err != nil {
		return err
	}
	return fmt.Errorf("chdir %s", f.name)
}

// Unconditional deref after the receiver has been used as a pointer: a nil receiver must panic,
// and it must panic at the FIELD READ, not before.
func (f *File) BoxName() string {
	_ = f.checkValid("boxname")
	return f.name
}

// A side effect BEFORE the deref: Go prints first, then panics. The converted C# must do both,
// in that order.
func (f *File) BoxAnnounce() string {
	fmt.Println("  side effect ran (box shape)")
	_ = f.checkValid("boxannounce")
	return f.name
}

// ---- Shape B: the receiver is only ever read as a value (ref receiver in C#) ----

// Unconditional deref: a nil receiver must panic.
func (f *File) ValName() string {
	return f.name
}

// Non-nil arithmetic through the value shape, to prove the shapes agree when the receiver is real.
func (f *File) ValFd() int {
	return f.fd + 1
}

// A side effect BEFORE the deref, through the value shape -- which C# reaches by way of the
// generated pointer-receiver bridge, so this guards that half of the preamble too.
func (f *File) ValAnnounce() string {
	fmt.Println("  side effect ran (ref shape)")
	return f.name
}

func try(label string, fn func()) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("%s -> panic: %v\n", label, r)
			return
		}
	}()
	fn()
	fmt.Printf("%s -> returned\n", label)
}

func main() {
	var nilFile *File

	fmt.Println("== 1. nil receiver, DELEGATED guard: must not panic ==")
	try("nilFile.checkValid", func() {
		fmt.Printf("  checkValid -> %v (is errInvalid: %v)\n", nilFile.checkValid("x"), nilFile.checkValid("x") == errInvalid)
	})
	try("nilFile.Chdir", func() {
		err := nilFile.Chdir()
		fmt.Printf("  Chdir -> %v (is errInvalid: %v)\n", err, err == errInvalid)
	})

	fmt.Println()
	fmt.Println("== 2. nil receiver, UNCONDITIONAL deref: must panic ==")
	try("nilFile.BoxName", func() { _ = nilFile.BoxName() })
	try("nilFile.ValName", func() { _ = nilFile.ValName() })

	fmt.Println()
	fmt.Println("== 3. nil receiver, side effect BEFORE the deref: effect then panic ==")
	try("nilFile.BoxAnnounce", func() { _ = nilFile.BoxAnnounce() })
	try("nilFile.ValAnnounce", func() { _ = nilFile.ValAnnounce() })

	fmt.Println()
	fmt.Println("== 4. non-nil receiver, both shapes: values identical ==")
	real := &File{name: "real.txt", fd: 41}
	try("real.checkValid", func() {
		fmt.Printf("  checkValid -> %v\n", real.checkValid("x"))
	})
	try("real.Chdir", func() {
		fmt.Printf("  Chdir -> %v\n", real.Chdir())
	})
	try("real.BoxName", func() { fmt.Printf("  BoxName -> %s\n", real.BoxName()) })
	try("real.ValName", func() { fmt.Printf("  ValName -> %s\n", real.ValName()) })
	try("real.ValFd", func() { fmt.Printf("  ValFd -> %d\n", real.ValFd()) })
	try("real.BoxAnnounce", func() { fmt.Printf("  BoxAnnounce -> %s\n", real.BoxAnnounce()) })
	fmt.Printf("  box/ref shapes agree on the name: %v\n", real.BoxName() == real.ValName())

	fmt.Println()
	fmt.Println("== 5. the receiver survives a write through the pointer ==")
	real.name = "renamed.txt"
	fmt.Printf("  BoxName -> %s, ValName -> %s\n", real.BoxName(), real.ValName())
}
