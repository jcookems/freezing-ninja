/* gauss.int.c  2/26/1999   Jason Cooke
 * 
 * This is a test of the gauleg and gaulag routine from Numerical Recipies.
 * Note that the functions which are ratios of polynomials do well with
 * Gauss-Legendre integration, while the one with an exponential does
 * well with the Gauss-Laguerre integration.
 *
 * To compile,
 * gcc gauss.int.c -lm
 */


#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "/home/jrcooke/c/numerical.recipies/gaulag.c"
#include "/home/jrcooke/c/numerical.recipies/gauleg.c"
#define NDIM 2000

int intans;
int whichint;

double f(double x)
{
    double t1,t2,t3;

    if(whichint==0)
	if(intans)
	    return(120.0);
	else
	    return(exp(5*log(x)-x));
    else if(whichint==1)
	if(intans)
	    return(2.0*M_PI/(3.0*sqrt(3.0)));
	else
	    return(x/(1+x*x*x));
    else if(whichint==2)
	if(intans)
	    return(-11.0*M_PI/(942*sqrt(3.0)) +
		   (444-1884*sqrt(2.0)+451*sqrt(3.0))*M_PI/77244.0 +
		   (2*sqrt(2.0)*atan(sqrt(2.0)))/41.0 - 
		   (3.0*log(6.0))/164.0 + 
		   (13.0*log(12.0))/628.0);
	else {
	    t1 = 1+ x*x;
	    t2 = 2+(x+2)*(x+2);
	    t3 = 3+(x+3)*(x+3);
	    return(x/( t1*t2*t3 ));
	}
    else {
	printf("Error!  whichint=%d\n",whichint);
	exit(1);
    };
}

void main()
{
    double xa[NDIM+1], wa[NDIM+1];
    double xe[NDIM+1], we[NDIM+1];
    int n,i,j;
    double temp1,temp2,temp3,delta;
    double alf,alfmin,alfmax;
    int alfnum;
    double xmax;
    double realint;

    whichint = 2;

    intans=1;
    realint = f(0.);
    intans=0;

    printf("Num points? ");
    scanf("%d",&n);
    printf("n=%d\n",n);

    printf("How far to integrate out? ");
    scanf("%lf",&xmax);
    printf("xmax=%f\n",xmax);

    printf("Alpha must be larger than -1.\n");

    if(1==0) {
	printf("What is alpha min? ");
	scanf("%lf",&alfmin);
	printf("alfmin=%f\n",alfmin);
	
	printf("What is alpha max? ");
	scanf("%lf",&alfmax);
	printf("alfmax=%f\n",alfmax);
	
	printf("How many alpha? ");
	scanf("%d",&alfnum);
	printf("alfnum=%d\n",alfnum);
    } else {
	printf("What is alpha? ");
	scanf("%lf",&alf);
	printf("alf=%f\n",alf);
	alfnum=1;
    };

    for( j=0; j<alfnum; j++) {
	if(alfnum!=1)
	    alf = alfmin + j*(alfmax-alfmin)/(alfnum-1);
	gaulag(xa,wa,n,alf);
	gauleg(0.,1.,xe,we,n);
	temp1 = 0.;
	temp2 = 0.;
	temp3 = 0.;
	delta = xmax/(1.0*n);
	for( i=1; i<=n; i++) {
	    temp1 += f(i*delta)*delta;
	    temp2 += wa[i]*f(xa[i])/exp(alf*log(xa[i])-xa[i]);
	    temp3 += we[i]*(f(xe[i])+f(1./xe[i])/(xe[i]*xe[i]));
	};
	printf("realans=%f, simple=%f Laguerre=%f Legendre=%f\n",
	       realint,temp1,temp2,temp3);
    };
    exit(0);  
}

