// see cos.int.C for comments.
////////////////////////////////////////////////////////////////////

#if !defined(__STD_COS_INT_H)  // This prevents loading twice!
#define __STD_COS_INT_H

#include <iostream.h> // for cout
#include <math.h>     // for sqrt

void cosintminiwork(double  a, double b,    int    absm,
		    double *c, double *cint);
double facforderivs(int n, double a, double b, double c, int absm);
void cosintwork(double a, double b, int m, double *int1, double *c);
double cos12intwork(double a1, double a2, double b, int absm);
double cos14intwork(double a1, double a2, double b, int absm);
double cosint(double a, double b, int m);
void cosint(double a, double b, int m,
	    double *int1, double *int2, double *int3);
double cos12int(double a1, double a2, double b, int m);
double cos14int(double a1, double a2, double b, int m);

#endif

