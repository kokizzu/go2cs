package main

import "fmt"

func init() {
	order = append(order, "main#1")
}

func main() {
	fmt.Println(len(order))

	for _, name := range order {
		fmt.Println(name)
	}
}
