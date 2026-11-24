let k x y = x
let main () =
    print_int (k 1 2);
    print_newline ();
    print_string (k "a" "b");
    print_newline ();
    print_bool (k true false);
    print_newline ()
