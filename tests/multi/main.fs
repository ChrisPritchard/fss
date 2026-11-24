let main () =
    (print_bool (map (fun x -> x * 2) [1; 2] = [2; 4]); print_newline ();
     print_bool (map (fun b -> not b) [true] = [false]); print_newline ())
