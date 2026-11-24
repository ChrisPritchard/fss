let applyTwice f x = f (f x)

let main () =
    print_int (applyTwice (fun x -> x * x) 3); print_newline ()
