let main () =
    (print_int (match (1, 2) with | (_, y) -> y); print_newline ();
     print_int (match (1, 2) with | (x, _) -> x); print_newline ())
