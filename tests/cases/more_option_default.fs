let default d o = match o with Some v -> v | None -> d
let main () =
    print_int (default 0 (Some 5));
    print_newline ();
    print_int (default 9 None);
    print_newline ()
