// C callbacks: an fss top-level function passed where C expects a function
// pointer. `#link "file.c"` adds a C source directly to the compile.
#include "tests/lib/callbacks.h"
#link "tests/lib/callbacks.c"

extern apply(f: (int -> int), x: int) : int
extern apply2(f: (int -> int -> int), a: int, b: int) : int
extern each(f: (int -> unit), n: int) : unit

let doubleIt x = x * 2
let add a b = a + b
let show i =
    print_int i
    print_string " "

let main () =
    println (apply doubleIt 21)
    println (apply2 add 20 22)
    each show 4
    print_newline ()
    0
