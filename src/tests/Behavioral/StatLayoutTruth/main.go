package main

import (
	"fmt"
	"io/fs"
	"os"
	"path/filepath"
	"time"
)

// StatLayoutTruth guards the syscall struct-passing seam on the Linux flavor: Fstat/fstatat hand
// the kernel a blittable mirror of struct stat and copy it back, so os.Stat/Lstat/File.Stat answer
// the truth (IsDir, Size, Mode, ModTime) and everything built on them (Glob, ReadDir's Info,
// WalkDir) walks a real tree. It also exercises the NoError syscall family (Getpid/Getppid/Getuid/
// Getgid) behind rawSyscallNoError. Every line prints a predicate or a count, never a host value,
// so the output is identical on Windows (where the Go paths never hit these seams) and Linux.
func main() {
	dir, err := os.MkdirTemp("", "statlayouttruth")
	if err != nil {
		fmt.Println("MkdirTemp error:", err)
		return
	}
	defer os.RemoveAll(dir)

	sub := filepath.Join(dir, "sub")
	if err := os.Mkdir(sub, 0o755); err != nil {
		fmt.Println("Mkdir error:", err)
		return
	}
	payload := []byte("0123456789abcdef")
	file := filepath.Join(dir, "a.txt")
	if err := os.WriteFile(file, payload, 0o644); err != nil {
		fmt.Println("WriteFile error:", err)
		return
	}
	if err := os.WriteFile(filepath.Join(sub, "b.txt"), payload[:5], 0o644); err != nil {
		fmt.Println("WriteFile error:", err)
		return
	}

	fi, err := os.Stat(dir)
	if err != nil {
		fmt.Println("Stat dir error:", err)
		return
	}
	fmt.Println("stat dir: IsDir =", fi.IsDir(), "IsRegular =", fi.Mode().IsRegular())

	fi, err = os.Stat(file)
	if err != nil {
		fmt.Println("Stat file error:", err)
		return
	}
	fmt.Println("stat file: IsDir =", fi.IsDir(), "IsRegular =", fi.Mode().IsRegular(), "Size =", fi.Size(), "Name =", fi.Name())
	fmt.Println("stat file: modtime within a day =", time.Since(fi.ModTime()) < 24*time.Hour, "nonzero =", !fi.ModTime().IsZero())

	fi, err = os.Lstat(file)
	if err != nil {
		fmt.Println("Lstat file error:", err)
		return
	}
	fmt.Println("lstat file: Size =", fi.Size(), "IsRegular =", fi.Mode().IsRegular())

	f, err := os.Open(file)
	if err != nil {
		fmt.Println("Open error:", err)
		return
	}
	fi, err = f.Stat()
	f.Close()
	if err != nil {
		fmt.Println("File.Stat error:", err)
		return
	}
	fmt.Println("fstat: Size =", fi.Size(), "IsRegular =", fi.Mode().IsRegular())

	matches, err := filepath.Glob(filepath.Join(dir, "*.txt"))
	fmt.Println("glob *.txt:", len(matches), err)

	entries, err := os.ReadDir(dir)
	fmt.Println("readdir:", len(entries), err)
	for _, e := range entries {
		info, ierr := e.Info()
		fmt.Println("  entry:", e.Name(), "IsDir =", e.IsDir(), "Info agrees =", ierr == nil && info.IsDir() == e.IsDir())
	}

	visited, files := 0, 0
	werr := filepath.WalkDir(dir, func(p string, d fs.DirEntry, err error) error {
		if err != nil {
			return err
		}
		visited++
		if !d.IsDir() {
			files++
		}
		return nil
	})
	fmt.Println("walkdir: visited =", visited, "files =", files, "err =", werr)

	fmt.Println("pid > 0:", os.Getpid() > 0, "ppid >= 0:", os.Getppid() >= 0)
	fmt.Println("uid callable:", os.Getuid() >= -1, "gid callable:", os.Getgid() >= -1)
}
