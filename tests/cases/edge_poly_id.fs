let id x = x
let main () =
    print_int (id 1 + id 2);
    print_newline ();
    print_string (id "a");
    print_newline ();
    print_bool (id true);
    print_newline ()
