type T = C0 | C1 | C2 | C3 | C4 | C5

let tag t = match t with | C0 -> 0 | C1 -> 1 | C2 -> 2 | C3 -> 3 | C4 -> 4 | C5 -> 5

let main () =
    (print_int (tag C0); print_newline ();
     print_int (tag C3); print_newline ();
     print_int (tag C5); print_newline ())
