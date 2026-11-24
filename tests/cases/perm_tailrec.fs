let rec loop n acc = if n = 0 then acc else loop (n - 1) (acc + 2)
let main () =
    print_int (loop 100 0);
    print_newline ()
