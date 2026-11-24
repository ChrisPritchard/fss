let comp f g x = f (g x)
let inc x = x + 1
let double x = x * 2
let main () =
    print_int (comp (fun x -> x + 1) (fun x -> x * 2) 5);
    print_newline ();
    print_int (comp (fun x -> x * 2) (fun x -> x + 1) 5);
    print_newline ()
