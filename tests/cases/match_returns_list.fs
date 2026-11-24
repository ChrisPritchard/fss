let f x = match x with | 0 -> [] | 1 -> [1] | _ -> [1; 2]

let main () =
    (print_bool (f 0 = []); print_newline ();
     print_bool (f 1 = [1]); print_newline ();
     print_bool (f 2 = [1; 2]); print_newline ())
