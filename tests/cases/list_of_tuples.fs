let rec sumPairs xs = match xs with | [] -> 0 | (a, b) :: t -> a + b + sumPairs t

let main () =
    print_int (sumPairs [(1, 2); (3, 4); (5, 6)]); print_newline ()
