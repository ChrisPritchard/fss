let rec app xs ys = match xs with [] -> ys | h :: t -> h :: app t ys
let main () =
    print_int (match app [1; 2] [3; 4] with a :: b :: c :: d :: [] -> a + b + c + d | _ -> 0);
    print_newline ()
