#include <math.h>

#define EPS 3.0e-11   /* EPS is the relative precision */

void gauleg(double x1, double x2, double x[], double w[], int n)
     /* GIven the upper and lower limits of integration x1 and x2,
	and a given n, this routine returns arrays x[1..n] and
	w[1..n] of length n, containing the abscissas and weights
	of the Gauss-Legendre n-point quadrature formula */

     /* The way this is implemented is by first calling this routine 
      * to generate the x and w vectors.  Then,
      *
      * \int_x1^x2 f(x) dx \approx \sum_{i=1}^n f(x[i]) w[i]
      *
      * This approximation is exact if f is polynomial, and get's
      * worse the less polynomial-ish f is.  (For example, things with
      * steep slopes).  In those cases, one of the other Gauss-XXX
      * routines should be used.  In particular, one with a weight
      * function w(x) such that f(x)/w(x) is polynomial.
      *
      * This routine can be used to do semi-infinte integrals, by
      * breaking the integral up into \int_0^1, \int_1^\inf, then
      * doing a change of variables on the last one x->1/x to get
      *
      * \int_0^\inf f(x) dx = \int_0^1 (f(x) + f(1/x)/x^2) dx
      *
      * This is now ammeniable to Gauss-Legendre quadrature. */

{
    int m,j,i;
    double z1,z,xm,xl,pp,p3,p2,p1; /* High precision is a good idea here */
    
    m=(n+1)/2;
    xm=0.5*(x2+x1); /* Roots are symmertic, only calculate half of them */
    xl=0.5*(x2-x1);
    for (i=1;i<=m;i++)  { /* Loop over desired roots */
	z=cos(M_PI*(i-0.25)/(n+0.5));
	do { /* Starting with the above approx to the i^th root, we enter
		the main loop of refinement by Newton's method. */
	    p1=1.0;
	    p2=0.0;
	    for (j=1;j<=n;j++) { /* Loop up the recurrence relations to
				    get the Legendre polynomial evaluated
				    a z */
		p3=p2;
		p2=p1;
		p1=((2.0*j-1.0)*z*p2-(j-1.0)*p3)/j;
	    } /* p1 is now the desired Legendre polynomial.  We next
		 compute pp, its derivative, by a standard relation 
		 involving also p2, the polynomial of one lower order. */
	    pp=n*(z*p1-p2)/(z*z-1.0);
	    z1=z;
	    z=z1-p1/pp; /* Newton's method */
	} while (fabs(z-z1) > EPS);
	x[i]=xm-xl*z;     /* Scale the root to the desired interval, */
	x[n+1-i]=xm+xl*z; /* and put in it's symmertic counterpart   */
	w[i]=2.0*xl/((1.0-z*z)*pp*pp); /* Compute the weight and    */
	w[n+1-i]=w[i];                 /* the symmetric counterpart */
    }
}

#undef EPS
