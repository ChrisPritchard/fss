let rec loop n = if n = 0 then 0 else loop (n - 1)

let main () =
    print_int (loop 1000000); print_newline ()
