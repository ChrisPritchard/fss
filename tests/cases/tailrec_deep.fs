let rec count n = if n = 0 then 0 else count (n - 1)

let main () =
    print_int (count 10000000); print_newline ()
