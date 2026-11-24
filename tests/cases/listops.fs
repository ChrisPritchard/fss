let rec map f xs = match xs with | [] -> [] | h :: t -> f h :: map f t
let rec filter p xs = match xs with | [] -> [] | h :: t -> if p h then h :: filter p t else filter p t
let rec fold f acc xs = match xs with | [] -> acc | h :: t -> fold f (f acc h) t
let rec length xs = match xs with | [] -> 0 | h :: t -> 1 + length t
let rec append xs ys = match xs with | [] -> ys | h :: t -> h :: append t ys
let rec revAcc xs acc = match xs with | [] -> acc | h :: t -> revAcc t (h :: acc)

let main () =
    let xs = [1; 2; 3; 4; 5] in
    (print_bool (map (fun x -> x * 2) xs = [2; 4; 6; 8; 10]); print_newline ();
     print_bool (filter (fun x -> x > 2) xs = [3; 4; 5]); print_newline ();
     print_int (fold (fun a -> fun b -> a + b) 0 xs); print_newline ();
     print_int (length xs); print_newline ();
     print_bool (append [1; 2] [3; 4] = [1; 2; 3; 4]); print_newline ();
     print_bool (revAcc xs [] = [5; 4; 3; 2; 1]); print_newline ())
