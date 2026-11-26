#include <math.h>
#link "m"

extern sqrt(x: double) : double
extern pow(x: double, y: double) : double
extern sin(x: double) : double
extern abs(x: int) : int

let main () =
    println (sqrt 144.0)
    println (pow 2.0 10.0)
    println (abs (-42))
    println (sin 0.0)
    println (double_to_string (sqrt 2.0))
    0
