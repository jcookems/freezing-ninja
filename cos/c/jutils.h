// jutils.H
//
// This is a collection of mathematical routines that are nice to have
// around.  They are
//
// uniformdouble    - Gives a random double precision number in the range [0,1)
// Power(x,n)       - gives x^n, for both n integer and double.
//                    This is for easy compatablity with output from
//                    Mathematica.
// factrl(n)        - gives n!,  for both n integer and double.
// atan(x,y)        - gives angle in range (-pi,pi] to point (x,y).  Again,
//                    Mathematica gives this function.
// checkeq(x,y,err) - checks if x and y are equal to within frac. error err
// checkfracdiff(x,y,err) 
//                  - gives fractional diff. of x and y if greater than err
//
// 2000/11/3  Added atan(x,y)
// 2000/11/8  Replaced my versions of the functions with math.h's version
//
////////////////////////////////////////////////////////////////
#if !defined(__STD_JUTILS_H)  // This prevents loading twice!
#define __STD_JUTILS_H

#include <iostream.h>
#include <math.h>
#include <stdlib.h> // for random

#define uniformdouble (random()/2147483648.) // since 0 <= random <= 2^31-1

double Power(double a, int    n);
double Power(double a, double n);
double factrl(double n);
double factrl(int n);
double atan(double x, double y);
int checkeq(double x, double y, double err);
double checkfracdiff(double x, double y, double err);

#endif
