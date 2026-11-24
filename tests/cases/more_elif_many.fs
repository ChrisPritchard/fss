let f x = if x = 0 then 10 elif x = 1 then 20 elif x = 2 then 30 elif x = 3 then 40 elif x = 4 then 50 else 60
let main () =
    print_int (f 0 + f 1 + f 2 + f 3 + f 4 + f 5);
    print_newline ()
