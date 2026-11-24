let f b = match b with | true -> 1 | false -> 0

let main () =
    (print_int (f true); print_newline ();
     print_int (f false); print_newline ())
