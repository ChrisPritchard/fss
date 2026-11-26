#pragma once
int apply(int (*f)(int), int x);
int apply2(int (*f)(int, int), int a, int b);
void each(void (*f)(int), int n);
