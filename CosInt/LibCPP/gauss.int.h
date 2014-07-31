// gauss.int.H   2000/8/18    Jason Cooke
//
// This file provides an interface between the user and the lower-level
// Gausian integration routines.  It defines
//
// GINTEQUAL = equal spaced nodes
// GINTGLEG  = Use Gauss-Legendre integration
// ??? More to come...
//
// The routine to use is
// 
// makearbweights(int pickmethod, int num, 
//	          double **x,  double **w,
//                double xmin, double xmax)
// - This makes the nodes (x) and weights (w) that conver the range from
//   xmin to xmax using the appropriate method.
//
// makeseminfweights(int pickmethod, int num, double **x, double **w,
//                   double delta){
// - This makes the nodes (x) and weights (w) that conver the range from
//   0 to infinity using the appropriate method.  We need to specify some
//   scale, that is given by delta.  There are several methods for converting
//   Gaussian integrations on a finite range to a finite range.  One is 
//   the inversion method I started out using, and there is also the
//   tangent method used my Machleidt.
// - Note that the meaning of delta varies quite a lot for different
//   methods.
//
////////////////////////////////////////////

#if !defined(__STD_GAUSS_INT_H)  // This prevents loading twice!
#define __STD_GAUSS_INT_H

#define GINTEQUAL   0
#define GINTGLEG    1
#define GINTGLEGTAN 2

#define _USE_MATH_DEFINES
#include <math.h>

void gauleg(double x1, double x2, double x[], double w[], int n);
void makearbweights(int pickmethod, int num, 
		    double **x,  double **w,
		    double xmin, double xmax);
void makeseminfweights(int pickmethod, int num, double **x, double **w,
		       double delta);

#endif
