import os

os.makedirs('tests/cases', exist_ok=True)
os.makedirs('tests/errors', exist_ok=True)

cases = []

def add(name, stmts, expected):
    defs = [s for s in stmts if s.startswith(('type ', 'let ', 'and '))]
    body = [s for s in stmts if s not in defs]
    top = '\n'.join(defs) + ('\n' if defs else '')
    main = ('    ' + ';\n    '.join(body) + '\n') if body else ''
    cases.append((name, top + 'let main () =\n' + main, expected))

# --- More arithmetic ---
add('more_arith_mul', ['print_int (6 * 7)', 'print_newline ()', 'print_int ((-6) * 7)', 'print_newline ()', 'print_int (6 * (-7))', 'print_newline ()', 'print_int ((-6) * (-7))', 'print_newline ()'],
    ['42', '-42', '-42', '42'])
add('more_arith_div_all', ['print_int (100 / 3)', 'print_newline ()', 'print_int (100 % 3)', 'print_newline ()', 'print_int ((-100) / 3)', 'print_newline ()', 'print_int ((-100) % 3)', 'print_newline ()', 'print_int (100 / (-3))', 'print_newline ()', 'print_int (100 % (-3))', 'print_newline ()'],
    ['33', '1', '-33', '-1', '-33', '1'])
add('more_arith_chained', ['print_int (1 + 2 + 3 + 4 + 5)', 'print_newline ()', 'print_int (2 * 3 * 4)', 'print_newline ()', 'print_int (100 - 50 - 25)', 'print_newline ()'],
    ['15', '24', '25'])
add('more_arith_parens', ['print_int ((1 + 2) * (3 + 4))', 'print_newline ()', 'print_int (2 * (3 + 4 * 5))', 'print_newline ()', 'print_int (((1)))', 'print_newline ()'],
    ['21', '46', '1'])
add('more_arith_large', ['print_int (1000000 * 1000)', 'print_newline ()', 'print_int (2000000000 + 2000000000)', 'print_newline ()'],
    ['1000000000', '-294967296'])

# --- More comparison / logic ---
add('more_cmp_mixed', ['print_bool (1 + 1 = 2)', 'print_newline ()', 'print_bool (2 * 3 <> 5)', 'print_newline ()', 'print_bool (10 / 2 = 5)', 'print_newline ()', 'print_bool (7 % 3 = 1)', 'print_newline ()'],
    ['true', 'true', 'true', 'true'])
add('more_logic_prec', ['print_bool (true && false || true)', 'print_newline ()', 'print_bool (true || false && false)', 'print_newline ()', 'print_bool (not (false && true))', 'print_newline ()'],
    ['true', 'true', 'true'])
add('more_cmp_chain', ['print_bool (1 < 2 && 2 < 3 && 3 < 4)', 'print_newline ()', 'print_bool (1 = 1 && 2 = 2 && 3 = 3)', 'print_newline ()'],
    ['true', 'true'])

# --- More control flow ---
add('more_while_countdown', ['let mutable i = 5', 'let mutable r = 1', 'while i > 0 do (r <- r * i; i <- i - 1)', 'print_int r', 'print_newline ()'],
    ['120'])
add('more_while_nested', ['let mutable i = 0', 'let mutable c = 0', 'while i < 3 do (let mutable j = 0 in (while j < 3 do (c <- c + 1; j <- j + 1); i <- i + 1))', 'print_int c', 'print_newline ()'],
    ['9'])
add('more_if_expr', ['let abs x = if x < 0 then -x else x', 'print_int (abs (-5))', 'print_newline ()', 'print_int (abs 5)', 'print_newline ()', 'print_int (abs 0)', 'print_newline ()'],
    ['5', '5', '0'])
add('more_elif_many', ['let f x = if x = 0 then 10 elif x = 1 then 20 elif x = 2 then 30 elif x = 3 then 40 elif x = 4 then 50 else 60',
                       'print_int (f 0 + f 1 + f 2 + f 3 + f 4 + f 5)', 'print_newline ()'],
    ['210'])

# --- More functions / recursion ---
add('more_ack', ['let rec ack m n = if m = 0 then n + 1 elif n = 0 then ack (m - 1) 1 else ack (m - 1) (ack m (n - 1))',
                 'print_int (ack 2 3)', 'print_newline ()'],
    ['9'])
add('more_gcd', ['let rec gcd a b = if b = 0 then a else gcd b (a % b)',
                 'print_int (gcd 48 36)', 'print_newline ()', 'print_int (gcd 17 5)', 'print_newline ()', 'print_int (gcd 100 10)', 'print_newline ()'],
    ['12', '1', '10'])
add('more_tail', ['let rec sum n acc = if n = 0 then acc else sum (n - 1) (acc + n)',
                  'print_int (sum 100 0)', 'print_newline ()'],
    ['5050'])
add('more_closure_chain', ['let add a b = a + b', 'let add1 = add 1', 'let add10 = add 10',
                           'print_int (add10 (add1 5))', 'print_newline ()'],
    ['16'])

# --- More lists ---
add('more_list_append', ['let rec app xs ys = match xs with [] -> ys | h :: t -> h :: app t ys',
                         'print_int (match app [1; 2] [3; 4] with a :: b :: c :: d :: [] -> a + b + c + d | _ -> 0)', 'print_newline ()'],
    ['10'])
add('more_list_reverse', ['let rec rev acc xs = match xs with [] -> acc | h :: t -> rev (h :: acc) t',
                          'print_int (match rev [] [1; 2; 3; 4] with a :: b :: c :: d :: [] -> a * 1000 + b * 100 + c * 10 + d | _ -> 0)', 'print_newline ()'],
    ['4321'])
add('more_list_filter', ['let rec filter p xs = match xs with [] -> [] | h :: t -> if p h then h :: filter p t else filter p t',
                         'print_int (match filter (fun x -> x % 2 = 0) [1; 2; 3; 4; 5; 6] with a :: b :: c :: [] -> a + b + c | _ -> 0)', 'print_newline ()'],
    ['12'])
add('more_list_fold', ['let rec fold f acc xs = match xs with [] -> acc | h :: t -> fold f (f acc h) t',
                       'print_int (fold (fun a -> fun b -> a + b) 0 [1; 2; 3; 4; 5])', 'print_newline ()'],
    ['15'])
add('more_list_of_strings', ['let rec join xs = match xs with [] -> "" | h :: [] -> h | h :: t -> h + "," + join t',
                             'print_string (join ["a"; "b"; "c"])', 'print_newline ()'],
    ['a,b,c'])

# --- More tuples ---
add('more_tuple_nested2', ['let t = (1, (2, (3, 4)))',
                           'print_int (match t with (a, (b, (c, d))) -> a + b + c + d)', 'print_newline ()'],
    ['10'])
add('more_tuple_of_list', ['let t = ([1; 2], "x", Some 3)',
                           'print_int (match t with (xs, s, o) -> (match xs with a :: b :: [] -> a + b | _ -> 0) + (match o with Some n -> n | None -> 0))', 'print_newline ()'],
    ['6'])

# --- More options ---
add('more_option_default', ['let default d o = match o with Some v -> v | None -> d',
                            'print_int (default 0 (Some 5))', 'print_newline ()', 'print_int (default 9 None)', 'print_newline ()'],
    ['5', '9'])
add('more_option_map', ['let opt_map f o = match o with Some v -> Some (f v) | None -> None',
                        'print_int (match opt_map (fun x -> x * 2) (Some 21) with Some n -> n | None -> 0)', 'print_newline ()'],
    ['42'])

# --- More records / unions ---
add('more_record_update', ['type P = { X: int; Y: int }', 'let p = { X = 1; Y = 2 }',
                           'let q = { X = p.X + 10; Y = p.Y + 20 }', 'print_int (q.X + q.Y)', 'print_newline ()'],
    ['33'])
add('more_union_nested', ['type A = B of int | C of A',
                          'let rec depth a = match a with B _ -> 0 | C a2 -> 1 + depth a2',
                          'print_int (depth (C (C (B 1))))', 'print_newline ()'],
    ['2'])
add('more_union_option', ['type T = S of int | Empty',
                          'let f t = match t with S n -> n | Empty -> 0',
                          'print_int (f (S 42))', 'print_newline ()', 'print_int (f Empty)', 'print_newline ()'],
    ['42', '0'])

# --- More polymorphism ---
add('more_poly_three', ['let k x y = x',
                        'print_int (k 1 2)', 'print_newline ()', 'print_string (k "a" "b")', 'print_newline ()', 'print_bool (k true false)', 'print_newline ()'],
    ['1', 'a', 'true'])
add('more_poly_nested', ['let apply2 f x y = f x y',
                         'print_int (apply2 (fun a -> fun b -> a + b) 3 4)', 'print_newline ()', 'print_int (apply2 (fun a -> fun b -> a * b) 5 6)', 'print_newline ()'],
    ['7', '30'])
add('more_poly_rec', ['let rec map f xs = match xs with [] -> [] | h :: t -> f h :: map f t',
                      'print_int (match map (fun x -> x + 1) [1; 2] with a :: b :: [] -> a + b | _ -> 0)', 'print_newline ()',
                      'print_string (match map (fun x -> x + "!") ["a"; "b"] with a :: b :: [] -> a + b | _ -> "?")', 'print_newline ()'],
    ['5', 'a!b!'])

# --- More strings ---
add('more_str_cmp', ['print_bool ("abc" = "abc")', 'print_newline ()', 'print_bool ("a" <> "aa")', 'print_newline ()', 'print_bool ("ab" = "ab")', 'print_newline ()'],
    ['true', 'true', 'true'])
add('more_str_get_all', ['let s = "abc"', 'print_int (str_get s 0)', 'print_newline ()', 'print_int (str_get s 1)', 'print_newline ()', 'print_int (str_get s 2)', 'print_newline ()'],
    ['97', '98', '99'])
add('more_str_sub_edge', ['print_string (str_sub "hello" 0 5)', 'print_newline ()', 'print_string (str_sub "hello" 5 0)', 'print_newline ()', 'print_string (str_sub "hello" 0 0)', 'print_newline ()'],
    ['hello', '', ''])
add('more_str_int_roundtrip', ['print_int (int_of_string (int_to_string 12345))', 'print_newline ()', 'print_string (int_to_string (-42))', 'print_newline ()'],
    ['12345', '-42'])

# --- More sequences ---
add('more_seq_let', ['let mutable r = 0', '(r <- 1; let y = r + 1 in (r <- y; print_int r; print_newline ()))'],
    ['2'])
add('more_seq_nested', ['print_int ((1; 2; 3))', 'print_newline ()', 'print_int ((print_int 9; 10))', 'print_newline ()'],
    ['3', '910'])

# --- More deep equality ---
add('more_eq_record_in_list', ['type P = { X: int }',
                               'print_bool ([{ X = 1 }] = [{ X = 1 }])', 'print_newline ()', 'print_bool ([{ X = 1 }] <> [{ X = 2 }])', 'print_newline ()'],
    ['true', 'true'])
add('more_eq_tuple_in_opt', ['print_bool (Some (1, "a") = Some (1, "a"))', 'print_newline ()', 'print_bool (None = None)', 'print_newline ()'],
    ['true', 'true'])
add('more_eq_nested_union', ['type T = A of T option | B',
                             'print_bool (A (Some (A None)) = A (Some (A None)))', 'print_newline ()', 'print_bool (A (Some (B)) <> A None)', 'print_newline ()'],
    ['true', 'true'])

# More error cases
errors = []
def add_err(name, program):
    errors.append((name, program + '\n'))

add_err('err_add_bool', 'let main () = print_int (1 + true)')
add_err('err_sub_str', 'let main () = print_int (1 - "a")')
add_err('err_and_int', 'let main () = print_bool (1 && true)')
add_err('err_or_int', 'let main () = print_bool (1 || true)')
add_err('err_lt_bool', 'let main () = print_bool (true < false)')
add_err('err_if_cond_int', 'let main () = print_int (if 1 then 2 else 3)')
add_err('err_match_int_str', 'let main () = print_int (match 1 with "a" -> 1 | _ -> 0)')
add_err('err_option_apply', 'let main () = print_int (Some 1)')
add_err('err_record_dup_field', 'type R = { A: int; A: int }\nlet main () = print_int 0')
add_err('err_union_payload_arity', 'type T = A of int\nlet main () = print_int (match A 1 with A -> 1 | _ -> 0)')
add_err('err_list_head_int', 'let main () = print_int (match 1 with a :: b -> a | _ -> 0)')
add_err('err_tuple_arity', 'let main () = print_int (match (1, 2) with (a, b, c) -> a + b + c | _ -> 0)')
add_err('err_nested_fn_apply', 'let main () = print_int ((fun x -> x) 1 2)')
add_err('err_while_cond', 'let main () = while 1 do print_int 1')
add_err('err_assign_to_arg', 'let f x = (x <- 1; x)\nlet main () = print_int (f 0)')
add_err('err_cons_int', 'let main () = print_int (match 1 :: 2 with a :: b -> a | _ -> 0)')

# Write
for name, program, expected in cases:
    with open(f'tests/cases/{name}.fs', 'w', newline='\n') as f:
        f.write(program)
    with open(f'tests/cases/{name}.expected', 'w', newline='\n') as f:
        f.write('\n'.join(expected) + '\n')

for name, program in errors:
    with open(f'tests/errors/{name}.fs', 'w', newline='\n') as f:
        f.write(program)

print(f'wrote {len(cases)} more positive cases and {len(errors)} more error cases')
