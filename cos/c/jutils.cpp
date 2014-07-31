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

double Power(double a, int    n) { return pow(a,n); } // a^n
double Power(double a, double n) { return pow(a,n); } // a^n

double factrl(double n) { return gamma(n+1.0); }  // returns n!
double factrl(int n) {
  // returns n!, from Numerical recipies
  static int ntop=4;
  static double a[33]={1.0,1.0,2.0,6.0,24.0};

  if (n < 0) {
    cout << "\aNegative factorial in routine FACTRL\n";
    return log((double)(n)); // This produces a trapable error
  }
  if (n > 32) return factrl(double(n));
  while (ntop<n) { int j = ntop++; a[ntop]=a[j]*ntop; }
  return a[n];
} // factrl

double atan(double x, double y) { return atan2(y,x); } // atan, (-pi,pi]

int checkeq(double x, double y, double err) {
  if(x==y)       return 1;
  else if(y==0.) return 0;
  else           return (fabs(x/y-1.)<err);
} // checkeq
double checkfracdiff(double x, double y, double err) {
  if(x==y)       return 0.;
  else if(y==0.) return 1.e30;
  else if(x==0.) return 0.;
  else           return (fabs(x/y-1.)<err ? 0. : x/y-1.);
} // checkfracdiff

#endif
