let rec divides d n =
    if d * d > n then false
    elif n % d = 0 then true
    else divides (d + 1) n

let isPrime n = if n < 2 then false else not (divides 2 n)

let rec countPrimes i n acc =
    if i > n then acc
    elif isPrime i then countPrimes (i + 1) n (acc + 1)
    else countPrimes (i + 1) n acc

let main () = print_int (countPrimes 2 100 0); print_newline ()
