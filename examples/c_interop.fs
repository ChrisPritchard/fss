// Calling C from fss: #include / #link pull in headers and linker flags,
// `extern` declares a foreign function (here from libm), and doubles are
// first-class. Builtins like sqrt/pow/sin need no declaration.
#include <math.h>
#link "m"

extern cbrt(x: double) : double
extern atan2(y: double, x: double) : double

let main () =
    println (sqrt 144.0)            // builtin -> libm
    println (pow 2.0 10.0)          // builtin
    println (cbrt 27.0)             // extern
    println (atan2 1.0 1.0)         // extern
    println (double_to_string (sqrt 2.0))
    0
