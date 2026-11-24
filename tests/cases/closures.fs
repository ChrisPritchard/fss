let main () =
    let a = 10 in
    let f = (fun x -> x + a) in
    print_int (f 5); print_newline ();
    let mutable acc = 0 in
    let addacc x = (acc <- acc + x; acc) in
    (print_int (addacc 3); print_newline ();
     print_int (addacc 4); print_newline ())
