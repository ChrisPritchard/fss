let rec revAcc xs acc = match xs with | [] -> acc | h :: t -> revAcc t (h :: acc)

let main () =
    print_bool (revAcc [1; 2; 3] [] = [3; 2; 1]); print_newline ()
