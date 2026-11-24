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

# Full comparison-operator matrix on ints
add('perm_cmp_ops', ['print_bool (2 < 3)', 'print_newline ()', 'print_bool (2 <= 2)', 'print_newline ()', 'print_bool (3 > 2)', 'print_newline ()', 'print_bool (3 >= 3)', 'print_newline ()', 'print_bool (2 = 2)', 'print_newline ()', 'print_bool (2 <> 3)', 'print_newline ()'],
    ['true', 'true', 'true', 'true', 'true', 'true'])
add('perm_cmp_ops_false', ['print_bool (3 < 2)', 'print_newline ()', 'print_bool (3 <= 2)', 'print_newline ()', 'print_bool (2 > 3)', 'print_newline ()', 'print_bool (2 >= 3)', 'print_newline ()', 'print_bool (2 = 3)', 'print_newline ()', 'print_bool (2 <> 2)', 'print_newline ()'],
    ['false', 'false', 'false', 'false', 'false', 'false'])

# Full logic matrix
add('perm_logic_matrix', ['print_bool (true && true)', 'print_newline ()', 'print_bool (true && false)', 'print_newline ()', 'print_bool (false && true)', 'print_newline ()', 'print_bool (false && false)', 'print_newline ()', 'print_bool (true || true)', 'print_newline ()', 'print_bool (true || false)', 'print_newline ()', 'print_bool (false || true)', 'print_newline ()', 'print_bool (false || false)', 'print_newline ()'],
    ['true', 'false', 'false', 'false', 'true', 'true', 'true', 'false'])

# Arithmetic permutations
add('perm_arith_ops', ['print_int (10 + 5)', 'print_newline ()', 'print_int (10 - 5)', 'print_newline ()', 'print_int (10 * 5)', 'print_newline ()', 'print_int (10 / 5)', 'print_newline ()', 'print_int (10 % 5)', 'print_newline ()'],
    ['15', '5', '50', '2', '0'])

# Deeply nested data
add('perm_deep_mix', ['type T = A of int | B of (int * string) list',
                      'let x = B [(1, "a"); (2, "b")]',
                      'print_bool (x = B [(1, "a"); (2, "b")])', 'print_newline ()',
                      'print_bool (x <> B [(1, "a")])', 'print_newline ()'],
    ['true', 'true'])
add('perm_list_of_option', ['let xs = [Some 1; None; Some 3]',
                            'print_int (match xs with a :: b :: c :: [] -> (match a with Some n -> n | None -> 0) + (match b with Some n -> n | None -> 0) + (match c with Some n -> n | None -> 0) | _ -> 0)', 'print_newline ()'],
    ['4'])
add('perm_tuple_opt_list', ['let t = (Some [1; 2], (3, "x"))',
                            'print_int (match t with (o, (n, s)) -> (match o with Some l -> (match l with a :: b :: [] -> a + b | _ -> 0) | None -> 0) + n)', 'print_newline ()'],
    ['6'])

# Recursion permutations
add('perm_rec_nested', ['let rec f n = if n = 0 then 1 else g (n - 1)', 'and g n = if n = 0 then 1 else f (n - 1) + 1',
                        'print_int (f 5)', 'print_newline ()', 'print_int (g 5)', 'print_newline ()'],
    ['3', '4'])
add('perm_tailrec', ['let rec loop n acc = if n = 0 then acc else loop (n - 1) (acc + 2)',
                     'print_int (loop 100 0)', 'print_newline ()'],
    ['200'])

# String permutations
add('perm_str_concat_n', ['print_string ("a" + "b" + "c" + "d")', 'print_newline ()', 'print_string ("" + "x")', 'print_newline ()', 'print_string ("x" + "")', 'print_newline ()'],
    ['abcd', 'x', 'x'])
add('perm_str_cmp_len', ['print_bool (str_len "abc" = 3)', 'print_newline ()', 'print_bool (str_len "" = 0)', 'print_newline ()', 'print_bool (str_len "a" <> 2)', 'print_newline ()'],
    ['true', 'true', 'true'])
add('perm_str_builtin_chain', ['print_string (str_sub ("hello" + "world") 4 6)', 'print_newline ()', 'print_int (str_len (str_sub "abcdef" 2 3))', 'print_newline ()'],
    ['oworld', '3'])

# Polymorphism permutations
add('perm_poly_2arg', ['let pair a b = (a, b)',
                       'print_int (match pair 1 2 with (a, b) -> a + b)', 'print_newline ()',
                       'print_string (match pair "a" "b" with (a, b) -> a + b)', 'print_newline ()'],
    ['3', 'ab'])
add('perm_poly_3arg', ['let f a b c = a',
                       'print_int (f 1 2 3)', 'print_newline ()', 'print_bool (f true 1 "x")', 'print_newline ()'],
    ['1', 'true'])
add('perm_poly_reverse', ['let rec rev acc xs = match xs with [] -> acc | h :: t -> rev (h :: acc) t',
                          'print_int (match rev [] [1; 2] with a :: b :: [] -> a + b | _ -> 0)', 'print_newline ()',
                          'print_string (match rev [] ["a"; "b"] with a :: b :: [] -> a + b | _ -> "?")', 'print_newline ()'],
    ['3', 'ba'])

# Assignment/sequence permutations
add('perm_assign_swap', ['let mutable a = 1', 'let mutable b = 2',
                         '(let t = a in (a <- b; b <- t; print_int a; print_newline (); print_int b; print_newline ()))'],
    ['2', '1'])
add('perm_assign_loop', ['let mutable i = 0', 'let mutable sum = 0',
                         'while i < 10 do (sum <- sum + i; i <- i + 1)', 'print_int sum', 'print_newline ()'],
    ['45'])

# Record permutations
add('perm_record_nested', ['type Inner = { A: int }', 'type Outer = { I: Inner; B: string }',
                           'let o = { I = { A = 5 }; B = "x" }',
                           'print_int o.I.A', 'print_newline ()', 'print_string o.B', 'print_newline ()'],
    ['5', 'x'])
add('perm_record_eq_deep', ['type Inner = { A: int }', 'type Outer = { I: Inner }',
                            'print_bool ({ I = { A = 1 } } = { I = { A = 1 } })', 'print_newline ()'],
    ['true'])

# Union permutations
add('perm_union_many', ['type T = A | B | C | D | E',
                        'let f t = match t with A -> 1 | B -> 2 | C -> 3 | D -> 4 | E -> 5',
                        'print_int (f A + f B + f C + f D + f E)', 'print_newline ()'],
    ['15'])
add('perm_union_payload_nested', ['type T = A of int list | B of T',
                                  'let rec size t = match t with A xs -> (match xs with [] -> 0 | _ :: r -> 1 + (match r with [] -> 0 | _ -> 1)) | B t2 -> 1 + size t2',
                                  'print_int (size (B (A [1; 2])))', 'print_newline ()'],
    ['3'])

# Error permutations
errors = []
def add_err(name, program):
    errors.append((name, program + '\n'))

add_err('err_add_str_int', 'let main () = print_string ("a" + 1)')
add_err('err_eq_func_int', 'let main () = print_bool ((fun x -> x) = (fun y -> y))')
add_err('err_apply_unit', 'let main () = print_int (() 1)')
add_err('err_match_scrut', 'let main () = print_int (match (fun x -> x) with 1 -> 1 | _ -> 0)')
add_err('err_while_body_type', 'let main () = while true do 1')
add_err('err_let_in_missing', 'let main () = (let x = 1 in)')
add_err('err_option_eq_type', 'let main () = print_bool (Some 1 = Some "a")')
add_err('err_record_field_type', 'type R = { A: int }\nlet main () = print_int ({ A = "s" }.A)')
add_err('err_union_ctor_apply', 'type T = A of int\nlet main () = print_int (A)')
add_err('err_cons_tail_type', 'let main () = print_int (match 1 :: "a" with a :: b -> a | _ -> 0)')
add_err('err_tuple_type_mismatch', 'let main () = print_int (match (1, "a") with (a, b) -> a + b)')

for name, program, expected in cases:
    with open(f'tests/cases/{name}.fs', 'w', newline='\n') as f:
        f.write(program)
    with open(f'tests/cases/{name}.expected', 'w', newline='\n') as f:
        f.write('\n'.join(expected) + '\n')

for name, program in errors:
    with open(f'tests/errors/{name}.fs', 'w', newline='\n') as f:
        f.write(program)

print(f'wrote {len(cases)} more positive cases and {len(errors)} more error cases')
