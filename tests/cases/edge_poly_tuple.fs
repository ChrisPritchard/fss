let fst p = match p with (a, b) -> a
let main () =
    print_int (fst (1, "x"));
    print_newline ();
    print_string (fst ("y", 2));
    print_newline ()
