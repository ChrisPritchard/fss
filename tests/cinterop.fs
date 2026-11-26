// C interop: #include / #link directives, extern declarations, double math.
#include "math.h"
#link "m"

extern sqrt(x: double) : double
extern pow(x: double, y: double) : double
extern abs(x: int) : int

let rec sumTo n acc =
    if n <= 0 then acc else sumTo (n - 1) (acc + n)

let main () =
    let r = sqrt 16.0
    println r
    let p = pow 2.0 10.0
    println p
    println (double_to_int p)
    println (str_len "hello")
    println (abs (-42))
    println (r * 2.0 + 1.0)
    println (sumTo 100 0)
    println (double_to_string 3.25)
    println (double_to_string (r / 2.0))
    0
