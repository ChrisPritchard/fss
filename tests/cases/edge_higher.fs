let twice f x = f (f x)
let main () =
    print_int (twice (fun x -> x + 1) 5);
    print_newline ()
