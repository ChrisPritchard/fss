// C interop: extern declarations, #include / #link, doubles.
#include "math.h"
#link "m"

extern sqrt(x: double) : double
extern fabs(x: double) : double
extern abs(x: int) : int

[<Include("stdlib.h")>]

let rec gcd a b = if b = 0 then a else gcd b (a % b)

let main () =
    let r = sqrt 144.0
    println r
    println (fabs (-3.5))
    println (abs (-7))
    println (sqrt 2.0 * sqrt 2.0)
    println (gcd 48 18)
    println (double_to_string (r / 5.0))
    println (double_to_int r)
    0
