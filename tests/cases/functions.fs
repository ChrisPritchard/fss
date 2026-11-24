let add x y = x + y
let double x = x * 2
let rec fact n = if n = 0 then 1 else n * fact (n - 1)

let main () =
    print_int (add 3 4); print_newline ();
    print_int (double 21); print_newline ();
    print_int (fact 5); print_newline ()
