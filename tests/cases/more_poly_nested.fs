let apply2 f x y = f x y
let main () =
    print_int (apply2 (fun a -> fun b -> a + b) 3 4);
    print_newline ();
    print_int (apply2 (fun a -> fun b -> a * b) 5 6);
    print_newline ()
