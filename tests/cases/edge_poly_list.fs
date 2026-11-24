let singleton x = [x]
let main () =
    print_int (match singleton 5 with a :: [] -> a | _ -> 0);
    print_newline ();
    print_string (match singleton "x" with a :: [] -> a | _ -> "?");
    print_newline ()
