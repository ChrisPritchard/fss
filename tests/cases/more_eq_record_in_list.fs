type P = { X: int }
let main () =
    print_bool ([{ X = 1 }] = [{ X = 1 }]);
    print_newline ();
    print_bool ([{ X = 1 }] <> [{ X = 2 }]);
    print_newline ()
