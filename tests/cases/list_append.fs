let rec append xs ys = match xs with | [] -> ys | h :: t -> h :: append t ys

let main () =
    print_bool (append [1; 2] [3; 4] = [1; 2; 3; 4]); print_newline ()
