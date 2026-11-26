// A fss shared library: no `main`, functions marked [<Export>] are callable
// from C with the C types declared in the generated mathlib.h.
#include <math.h>
#link "m"


[<Export>]
let add x y = x + y

[<Export>]
let fib n =
    let mutable a = 0
    let mutable b = 1
    let mutable k = 0
    while k < n do
        let t = a + b
        a <- b
        b <- t
        k <- k + 1
    a

[<Export>]
let sumTo n =
    let mutable i = 1
    let mutable acc = 0
    while i <= n do
        acc <- acc + i
        i <- i + 1
    acc

[<Export("greet")>]
let greeting (name: cstring) =
    cstring_of_string ("hello, " + string_of_cstring name + "!")

[<Export>]
let hyp (a: double) (b: double) = sqrt (a * a + b * b)
