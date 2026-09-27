package main

import "fmt"

// Go evaluates narrow-integer arithmetic (int8/uint8/int16/uint16) at the operand's own width, so an
// overflowing intermediate wraps: int8 100+100 is -56 and uint8 200+200 is 144. C# promotes sub-int
// arithmetic to int, so the converter narrows the result wherever its consumer is not wrap-invariant.
// A typed narrow destination already did (NarrowArithmeticArg); each arm below is a consumer that did
// not, printed under its class name, and run under recover so one class's panic cannot hide the next.
// NarrowArithmeticCompileSinks holds the consumers whose unnarrowed form did not compile at all.

type I any

type holder struct{ v any }

var (
	a  int8   = 100
	u  uint8  = 200
	w  int16  = 30000
	z  uint16 = 60000
	c  byte   = '/'
	d  uint8  = 250
	m8 int8   = -128
	n1 int8   = -1
)

func show(x any) string { return fmt.Sprintf("%v %T", x, x) }

func ret(a int8) any { return a + a }

func variadic(xs ...any) string { return fmt.Sprint(xs...) }

func gen[T any](x T) string { return fmt.Sprintf("%v %T", x, x) }

func arm(class string, f func() string) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Println(class+": panic:", r)
		}
	}()

	fmt.Println(class+":", f())
}

func main() {
	// Interface sinks carry the value AND the dynamic type.
	arm("fmt arg", func() string { return fmt.Sprint(a+a, " ", u+u, " ", w+w, " ", z+z) })
	arm("fmt %T", func() string { return fmt.Sprintf("%T %T %T %T", a+a, u+u, -a-a, ^u) })
	arm("any var", func() string {
		var x any = a + a
		return show(x)
	})
	arm("any param", func() string { return show(u*2) + " " + show(-u) })
	arm("any return", func() string { return show(ret(a)) })
	arm("variadic any", func() string { return variadic(a+a, u+u) })
	arm("composite any", func() string {
		s := []any{a + a, u + u}
		m := map[string]any{"k": w + w}
		h := holder{v: z + z}
		return fmt.Sprint(s, m, h.v, any(a+a))
	})
	arm("chan any", func() string {
		ch := make(chan any, 1)
		ch <- u + u
		return show(<-ch)
	})
	arm("type only", func() string { return show(u/3) + " " + show(a%7) + " " + show(u>>1) + " " + show(+a) + " " + show(^a) })

	// A named empty interface.
	arm("named any", func() string {
		var i I = a + a
		return show(i)
	})

	// Generic inference instantiates at Go's type.
	arm("generic", func() string { return gen(a+a) + " " + gen(u+u) })

	// Widening conversions read the whole value.
	arm("widening", func() string {
		return fmt.Sprint(int(u+u), " ", int64(a+a), " ", float64(a+a), " ", int(c-'0'), " ", uint32(z+z), " ", int32(w+w))
	})

	// Index and slice bounds.
	tbl := make([]int, 256)
	for k := range tbl {
		tbl[k] = k
	}
	arr := [256]int{144: 7}
	str := "0123456789"
	arm("slice index", func() string { return fmt.Sprint(tbl[u+u]) })
	arm("array index", func() string { return fmt.Sprint(arr[u+u]) })
	arm("string index", func() string { return fmt.Sprint(str[d+10]) })
	arm("slice bound", func() string { return fmt.Sprint(len(tbl[u+u:]), " ", len(tbl[:u+u]), " ", str[d+10:]) })

	// Shift counts, including a compound shift-assign.
	arm("shift count", func() string {
		y := uint32(1)
		y <<= d + 10
		return fmt.Sprint(1<<(u+u-140), " ", uint32(1)<<(d+10), " ", y)
	})

	// Division, remainder and right-shift operands after an intermediate overflow, and MinInt8 / -1.
	arm("divrem operand", func() string { return fmt.Sprint((a+a)/2, " ", (a+a)%7, " ", (u+u)/3) })
	arm("shr operand", func() string { return fmt.Sprint((a+a)>>1, " ", (u+u)>>4) })
	arm("minint8 / -1", func() string { return fmt.Sprint(m8/n1, " ", m8%n1) })

	// Parenthesized comparison operands.
	arm("paren compare", func() string { return fmt.Sprint((a+a) < 0, " ", (u+u) == 144) })

	// Switch tag and case value.
	arm("switch tag", func() string {
		switch u + u {
		case 144:
			return "case 144"
		}
		return "default"
	})
	arm("case value", func() string {
		switch {
		case u+u < 150:
			return "wrapped"
		}
		return "unwrapped"
	})

	// Builtin arguments: min/max already narrowed, append's later elements did not.
	arm("builtin arg", func() string {
		b := append([]byte{}, u+u, u*2, u-1)
		return fmt.Sprint(min(a+a, 0), " ", max(u+u, 0), " ", b)
	})

	// A parenthesized short variable declaration takes Go's type.
	arm("paren define", func() string {
		t2 := (a + a)
		return show(t2)
	})
}
