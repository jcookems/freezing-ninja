#include <math.h>
#define EPS 3.0e-11   /* EPS is the relative precision */
#define MAXIT 10

void gaulag(double x[], double w[], int n, double alf)
     /* Given alf, the parameter \alpha of the Laguerre polynomials,
      * this routine returns arrays x[1..n] and w[1..n] containing
      * the abscissas and weights of the n-point Gauss-Laguerre
      * quadrature formula.  The smallest abscissa is returned in
      * x[1], the largest in x[n]. */

     /* Note that alf should be larger than -1, since the location
      * of the zeros are quite different for alf<-1 and alf>-1 */

     /* The way to integrate with this is like this:
      * 
      * \int_0^\inf f(x) dx \approx 
      *          \sum_{i=1}^n w[i] f(x[i])/(x[i]^alf exp(-x[i]))
      *
      * This works well if f/(x^alf e^-x) is a polynomial.
      * If f is already a polynomial or ratio of polys, then this
      * routine works rather poorly, and the Gauss-Legendre 
      * routine should be used.
      *
      * However, is f/(x^alf e^-x) is polynomial, or close to one,
      * this this routine converges with about 5 integration points. */
{
    double gammln(double xx);
    int i,its,j;
    double ai;
    double p1,p2,p3,pp,z,z1; /* High precision is good idea for this routine */

    void nerror(char error_text[])
	{
	    printf(error_text);
	    printf("\n");
	    exit(1);
	} /* I put this here to clean up my codes */

    if (alf <= -1.0) nerror("alf <= -1.0 in gaulag");

    for (i=1;i<=n;i++) {  /* Loop over the desired roots */
	if (i==1) {        /* initial guess of smallest root */
	    z=(1.0+alf)*(3.0+0.92*alf)/(1.0+2.4*n+1.8*alf);
	} else if (i==2) { /* initial guess for the second root */
	    z += (15.0+6.25*alf)/(1.0+0.9*alf+2.5*n);
	} else {           /* initial guess for other roots */
	    ai=i-2;
	    z += ((1.0+2.55*ai)/(1.9*ai)+1.26*ai*alf/
		  (1.0+3.5*ai))*(z-x[i-2])/(1.0+0.3*alf);
	}
	for (its=1;its<=MAXIT;its++) { /* Refinement by Newton's method */
	    p1=1.0;
	    p2=0.0;
	    for (j=1;j<=n;j++) { /* Loop up the recurrence relation to get */
		p3=p2;           /* the Laguerre polynomial evaluated at z */
		p2=p1;
		p1=((2*j-1+alf-z)*p2-(j-1+alf)*p3)/j;
	    }
	    /* p1 is now the desired Laguerre polynomial.  We next compute
	     * pp, its derivative, by a standard relation involving also
	     * p2, the polynomial of one lower order. */
	    pp=(n*p1-(n+alf)*p2)/z;
	    z1=z;
	    z=z1-p1/pp;   /* Newton's formula */
	    if (fabs(z-z1) <= EPS) break;
	}
	if (its > MAXIT) nerror("too many iterations in gaulag");
	x[i]=z;
	/*	w[i] = -exp(gammln(alf+n)-gammln((double)n))/(pp*n*p2); */
	w[i] = -exp(lgamma(alf+n)-lgamma((double)n))/(pp*n*p2); 
	/* I'm using the FreeBSD lgamma function to clean up my codes */
    }
}
#undef EPS
#undef MAXIT
