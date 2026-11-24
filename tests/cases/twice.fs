let twice f x = f (f x)

let main () =
    print_int (twice (fun x -> x + 3) 10); print_newline ()
