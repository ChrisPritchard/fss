let apply f x = f x
let inc x = x + 1
let double x = x * 2
let main () =
    print_int (apply (fun x -> x + 1) 41);
    print_newline ();
    print_int (apply (fun x -> x * 2) 21);
    print_newline ()
