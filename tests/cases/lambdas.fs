let apply f x = f x

let main () =
    print_int (apply (fun x -> x + 1) 41); print_newline ();
    print_int ((fun x -> x * x) 7); print_newline ()
