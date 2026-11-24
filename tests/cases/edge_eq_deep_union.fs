type T = A of int list | B of T option
let main () =
    print_bool (A [1; 2] = A [1; 2]);
    print_newline ();
    print_bool (B (Some (A [1])) = B (Some (A [1])));
    print_newline ();
    print_bool (A [1] <> A [2]);
    print_newline ()
