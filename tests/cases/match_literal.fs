let f x = match x with | 0 -> 10 | 1 -> 20 | _ -> 30

let main () =
    (print_int (f 0); print_newline ();
     print_int (f 1); print_newline ();
     print_int (f 2); print_newline ())
