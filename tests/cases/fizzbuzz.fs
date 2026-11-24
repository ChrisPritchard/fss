let rec go i n =
    if i > n then ()
    elif i % 15 = 0 then (print_string "FizzBuzz"; print_newline (); go (i + 1) n)
    elif i % 3 = 0 then (print_string "Fizz"; print_newline (); go (i + 1) n)
    elif i % 5 = 0 then (print_string "Buzz"; print_newline (); go (i + 1) n)
    else (print_int i; print_newline (); go (i + 1) n)

let main () = go 1 15
