type T = A of int | B of (int * string) list
let x = B [(1, "a"); (2, "b")]
let main () =
    print_bool (x = B [(1, "a"); (2, "b")]);
    print_newline ();
    print_bool (x <> B [(1, "a")]);
    print_newline ()
