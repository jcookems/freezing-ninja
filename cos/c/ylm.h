// ylm.H    1/26/2000  Jason R. Cooke
//
// This is a collection of some routines from Numerical Recipies, some
// stuff I wrote myself, and a Clebsch-Gordan code I got off the web,
// credited to J.H.Gunn in 1964.
//
//
// dleg
//   a modification of the routine fleg.c from Numerical Recipies
//   This gives a list of Legendre polynomials from P_1 to P_l of x
//
// legp
//   COMPUTES THE LEGENDRE POLYNOMS, P_j(x) and P_{j-1}(x)
//   This is from Machleidt's code
//
// dlegsimp
//   COMPUTES THE LEGENDRE POLYNOM, P_j(x)
//   This is modified from Machleidt's code
//
// dplgndr
//   a modification of the routine plgndr.c from Numerical Recipies
//   Computes associated Legendre polynomial P^m_l(x).  Here 
//   m and l satisfy 0 <= m <= l,  -1 <= x <= 1 */
//
// getYlmDe
//   YlmDe = Y(l,m,\theta)
//   The complex exponential has been factored out (hence De)
//   so that the Ylm is real.
//
// getYlmDes
//   YlmDes = { {Y(l=0) for many thetas},{Y(l=1) for many thetas}, ... }
//   The complex exponential has be factored out (hence De)
//
// ---- The stuff below is for Clebsch Gordan coefficients --------------
//
// doubleint
//   given a real number that is (approx) of the form i*0.5, this returns i
//
// printinthalfint
//   prints i/2, makeing the fraction proper if possible
//
// testthejm
//   checks if the provided j and m are legal.
//
// fac2
//   does (i/2)!, fails if i is not even.
//
// clebschgordanwork
// This gives <J/2,M/2;J1/2,J2/2|J1/2,M1/2;J2/2,M2/2>
//
// clebschgordan
//   This gives <j,m;j1,j2|j1,m1;j2,m2>
//
// clebschgordanint
//   This gives <j,m;j1,j2|j1,m1;j2,m2>
//
/////////////////////////////////////////////////////////////////////

#if !defined(__STD_YLM_H)  // This prevents loading twice!
#define __STD_YLM_H

#include <iostream.h>
#include <stdlib.h>
#include <math.h>

// Start with some ordinary Legendre Polynomial functions ----------------
void dleg(double x, double *pl, int nl);
void legp(double *pj, double *pjm1, double x, int j);
double dlegsimp(int j, double x);
// Now we have Associated Legendre Polynomials ---------------------------
double dplgndr(int l,int m, double x);
void dplgndrmany(int m, double x, double *p, int nump);
double asslegpoly(int l,int m, double x);
// Now the Spherical Harmonics -------------------------------------------
double getYlmDe(int l, int m, double theta);
void getYlmDes(int m, int num, double *thetas, double **YlmDes);
void getYlmDemany(int m, double theta, double *YlmDe, int num);
// ---- The stuff below is for Clebsch Gordan coefficients ---------------
double clebschgordanwork(int J1, int M1, int J2, int M2, int J, int M);
double clebschgordan(double j1, double m1, double j2, double m2,
		     double j , double m);
double clebschgordanint(int j1, int m1, int j2, int m2, int j, int m);

#endif
