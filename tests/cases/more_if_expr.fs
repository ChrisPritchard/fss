let abs x = if x < 0 then -x else x
let main () =
    print_int (abs (-5));
    print_newline ();
    print_int (abs 5);
    print_newline ();
    print_int (abs 0);
    print_newline ()
