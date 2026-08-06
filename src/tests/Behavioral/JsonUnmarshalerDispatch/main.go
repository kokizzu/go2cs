package main

import (
	"encoding/json"
	"fmt"
	"time"
)

// Celsius has a POINTER-receiver UnmarshalJSON, so json must discover it through
// the *Celsius method set (reflect NumMethod > 0, then the Unmarshaler assert).
type Celsius float64

func (c *Celsius) UnmarshalJSON(data []byte) error {
	var f float64
	if err := json.Unmarshal(data, &f); err != nil {
		return err
	}
	*c = Celsius(f) + 100
	return nil
}

func main() {
	// Direct pointer target: ValueOf(&t) is already Pointer-kind, so indirect()
	// gates on v.Type().NumMethod() > 0 before asserting Unmarshaler.
	var t time.Time
	err := json.Unmarshal([]byte(`"2020-01-02T03:04:05Z"`), &t)
	fmt.Println(err, t.UTC().Format(time.RFC3339))

	// Whole-value dispatch: a non-string JSON value must still reach
	// Time.UnmarshalJSON, which rejects it.
	err = json.Unmarshal([]byte(`{}`), &t)
	fmt.Println(err)

	// Marshal round trip (value-receiver MarshalJSON via Implements).
	b, err := json.Marshal(t)
	fmt.Println(string(b), err)

	// Local named type with a pointer-receiver unmarshaler.
	var c Celsius
	err = json.Unmarshal([]byte(`42`), &c)
	fmt.Println(err, float64(c))

	// Struct fields: indirect() takes the v.Kind() != Pointer && Name() != "" &&
	// CanAddr() branch, so the FIELD value must be addressable and Addr() must
	// surface a pointer whose method set finds the unmarshaler.
	var s struct {
		T time.Time
		C Celsius
	}
	err = json.Unmarshal([]byte(`{"T":"2021-06-07T08:09:10Z","C":1}`), &s)
	fmt.Println(err, s.T.UTC().Format(time.RFC3339), float64(s.C))
}
