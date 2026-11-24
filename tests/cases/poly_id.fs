let id x = x

let main () =
    (print_int (id 5); print_newline ();
     print_bool (id true); print_newline ();
     print_string (id "hi"); print_newline ())
