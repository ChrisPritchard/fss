let compose f g x = f (g x)

let main () =
    print_int (compose (fun x -> x + 1) (fun x -> x * 2) 5); print_newline ()
