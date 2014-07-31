// spherical.besselj.h   2000/6/30   Jason R. Cooke
//
// These are routines from Numerical Recipes
//
////////////////////////////////////////////////////////////////////
#if !defined(__SPHERICAL_BESSEL_H)  // This prevents loading twice!
#define __SPHERICAL_BESSEL_H

#include <math.h>

double zchebev(double a, double b, double *c, int m, double x)
{
  float d=0.0,dd=0.0,sv,y,y2;
  int j;
  
  if ((x-a)*(x-b) > 0.0) {
    cout << "x not in range in routine CHEBEV\n";
    exit(1);
  }
  y2=2.0*(y=(2.0*x-a-b)/(b-a));
  for (j=m-1;j>=1;j--) {
    sv=d;
    d=y2*d-dd+c[j];
    dd=sv;
  }
  return y*d-dd+0.5*c[0];
}

#define NUSE1 5
#define NUSE2 5

void zbeschb(double x, double *gam1, double *gam2, double *gampl, 
	     double *gammi)
     /* Evaluates Gamma_1 and Gamma_2 by Chebyshev expansion for
	|x| <= 1/2.  Also returns 1/Gamma(1+x) and 1/Gamma(1-x).
	If converting to double precision, set NUSE1 = 7, NUSE2 = 8. */
{
    double xx;
    static double c1[] = {
	-1.142022680371168e0,6.5165112670737e-3,
	3.087090173086e-4,-3.4706269649e-6,6.9437664e-9,
	3.67795e-11,-1.356e-13};
    static double c2[] = {
	1.843740587300905e0,-7.68528408447867e-2,
	1.2719271366546e-3,-4.9717367042e-6,-3.31261198e-8,
	2.423096e-10,-1.702e-13,-1.49e-15};
    xx=8.0*x*x-1.0;  /* Multiply x by 2 to make range be -1 to 1 */
    /* and then apply transformation for evaluating even Chebyshev series. */
    *gam1=zchebev(-1.0,1.0,c1,NUSE1,xx);
    *gam2=zchebev(-1.0,1.0,c2,NUSE2,xx);
    *gampl= *gam2-x*(*gam1);
    *gammi= *gam2+x*(*gam1);
}
#undefine NUSE1
#undefine NUSE2

#define EPS 1.0e-16
#define FPMIN 1.0e-30
#define MAXIT 10000
#define XMIN 2.0

void zbessjy(double x, double xnu, double *rj, double *ry, double *rjp,
	     double *ryp)
     /*
Returns the Bessel functions rj = J_\nu, ry=Y_\nu 
and  their derivatives rjp = J'_\nu, ryp = Y'_\nu, for
positive x and for xnu = \nu >= 0.  The relative accuracy
is within  one or two significant digits of EPS, except near a
zero of one of the functions, where EPS controls its absolute accuracy.
FPMIN is a number close to the machine's smallest floating-point number.
All internal arithmetic is in double precision.  To convert the entire
routine to double precision, change the float declarations above to
double and decrease EPS to 10^-16.  Also convert the function beschb.
*/
{
    int i,isign,l,nl;
    double a,b,br,bi,c,cr,ci,d,del,del1,den,di,dlr,dli,dr,e,f,fact,fact2,
	fact3,ff,gam,gam1,gam2,gammi,gampl,h,p,pimu,pimu2,q,r,rjl,
	rjl1,rjmu,rjp1,rjpl,rjtemp,ry1,rymu,rymup,rytemp,sum,sum1,
	temp,w,x2,xi,xi2,xmu,xmu2;

    if (x <= 0.0 || xnu < 0.0) {
      cout << "bad arguments in bessjy\n";
      exit(1);
    }
    nl=(x < XMIN ? (int)(xnu+0.5) : IMAX(0,(int)(xnu-x+1.5)));
    /* nl is the number of downward recurrences of the J's and upward
       recurrences of Y's.  xmu lies between -1/2 and 1/2 for x<XMIN,
       while it is chosen so that x is greater than the turning point
       for x >= XMIN. */
    xmu=xnu-nl;
    xmu2=xmu*xmu;
    xi=1.0/x;
    xi2=2.0*xi;
    w=xi2/M_PI;    /* The Wronskian. */
    isign=1;     /* Evaluate CF1 by modified Lentz's method (sec 5.2). */
    /* isign keeps track of sign changes in the denominator. */
    h=xnu*xi;
    if (h < FPMIN) h=FPMIN;
    b=xi2*xnu;
    d=0.0;
    c=h;
    for(i=1;i<=MAXIT;i++) {
	b += xi2;
	d=b-d;
	if (fabs(d) < FPMIN) d=FPMIN;
	c=b-1.0/c;
	if (fabs(c) < FPMIN) c=FPMIN;
	d=1.0/d;
	del=c*d;
	h=del*h;
	if (d < 0.0) isign = -isign;
	if (fabs(del-1.0) < EPS) break;
    }
    if (i > MAXIT) {
      cout << "x too large in bessjy; try asymptotic expansion\n";
      exit(1);
    }
    rjl=isign*FPMIN; /* Initialize J_\nu and J'_nu for downward recurrence. */
    rjpl=h*rjl;
    rjl1=rjl;        /* Store values for later rescaling. */
    rjp1=rjpl;
    fact=xnu*xi;
    for (l=nl;l>=1;l--) {
	rjtemp=fact*rjl+rjpl;
	fact -= xi;
	rjpl=fact*rjtemp-rjl;
	rjl=rjtemp;
    }
    if (rjl == 0.0) rjl=EPS;
    f=rjpl/rjl;   /* Now have unnormalized J_\mu and J'_\mu. */
    if (x < XMIN) { /* Use series. */
	x2=0.5*x;
	pimu=M_PI*xmu;
	fact = (fabs(pimu) < EPS ? 1.0 : pimu/sin(pimu));
	d = -log(x2);
	e=xmu*d;
	fact2 = (fabs(e) < EPS ? 1.0 : sinh(e)/e);
	zbeschb(xmu,&gam1,&gam2,&gampl,&gammi); 
	/* Chebyshev evaluation of Gamma_1 and Gamma_2. */
	ff=2.0/M_PI*fact*(gam1*cosh(e)+gam2*fact2*d);  /* f_0 */
	e=exp(e);
	p=e/(gampl*M_PI);     /* p_0 */
	q=1.0/(e*M_PI*gammi); /* q_0 */
	pimu2=0.5*pimu;
	fact3 = (fabs(pimu2) < EPS ? 1.0 : sin(pimu2)/pimu2);
	r=M_PI*pimu2*fact3*fact3;
	c=1.0;
	d = -x2*x2;
	sum=ff+r*q;
	sum1=p;
	for (i=1;i<=MAXIT;i++) {
	    ff=(i*ff+p+q)/(i*i-xmu2);
	    c *= (d/i);
	    p /= (i-xmu);
	    q /= (i+xmu);
	    del=c*(ff+r*q);
	    sum += del;
	    del1=c*p-i*del;
	    sum1 += del1;
	    if (fabs(del) < (1.0+fabs(sum))*EPS) break;
	}
	if (i > MAXIT) {
	  cout << "bessy series failed to converge\n";
	  exit(1);
	}
	rymu = -sum;
	ry1 = -sum1*xi2;
	rymup=xmu*xi*rymu-ry1;
	rjmu=w/(rymup-f*rymu); /* Equation (6.7.13). */
    } else { /* Evaluate CF2 by modified Lentz's method (sec 5.2) */
	a=0.25-xmu2;
	p = -0.5*xi;
	q=1.0;
	br=2.0*x;
	bi=2.0;
	fact=a*xi/(p*p+q*q);
	cr=br+q*fact;
	ci=bi+p*fact;
	den=br*br+bi*bi;
	dr=br/den;
	di = -bi/den;
	dlr=cr*dr-ci*di;
	dli=cr*di+ci*dr;
	temp=p*dlr-q*dli;
	q=p*dli+q*dlr;
	p=temp;
	for (i=2;i<=MAXIT;i++) {
	    a += 2*(i-1);
	    bi += 2.0;
	    dr=a*dr+br;
	    di=a*di+bi;
	    if (fabs(dr)+fabs(di) < FPMIN) dr=FPMIN;
	    fact=a/(cr*cr+ci*ci);
	    cr=br+cr*fact;
	    ci=bi-ci*fact;
	    if (fabs(cr)+fabs(ci) < FPMIN) cr=FPMIN;
	    den=dr*dr+di*di;
	    dr /= den;
	    di /= -den;
	    dlr=cr*dr-ci*di;
	    dli=cr*di+ci*dr;
	    temp=p*dlr-q*dli;
	    q=p*dli+q*dlr;
	    p=temp;
	    if (fabs(dlr-1.0)+fabs(dli) < EPS) break;
	}
	if (i > MAXIT) {
	  cout << "cf2 failed in bessjy\n";
	  exit(1);
	}
	gam=(p-f)/q;  /* Equations (6.7.6) - (6.7.10). */
	rjmu=sqrt(w/((p-f)*gam+q));
	rjmu=SIGN(rjmu,rjl);
	rymu=rjmu*gam;
	rymup=rymu*(p+q/gam);
	ry1=xmu*xi*rymu-rymup;
    }
    fact=rjmu/rjl;
    *rj=rjl1*fact;   /* Scale original J_nu and J'_nu. */
    *rjp=rjp1*fact;
    for (i=1;i<=nl;i++) {   /* Upward recurrence of Y_\nu */
	rytemp=(xmu+i)*xi2*ry1-rymu;
	rymu=ry1;
	ry1=rytemp;
    }
    *ry=rymu;
    *ryp=xnu*xi*rymu-ry1;
}

#undefine EPS
#undefine FPMIN
#undefine MAXIT
#undefine XMIN

#define RTPIO2 1.25331413731550025120788264241

void zsphbes(int n, double x, double *sj, double *sy, double *sjp, double *syp)
     /* Returns spherical Bessel functions j_n(x), y_n(x), and their
	derivatives j'_n(x), y'_n(x) for integer n.  Note that
	j_n(x) = sqrt(pi/2x) J_{n+1/2}(x)
	y_n(x) = sqrt(pi/2x) Y_{n+1/2}(x)     */
{
    double factor,order,rj,rjp,ry,ryp;
    
    if (n < 0 || x <= 0.0) {
      cout << "bad arguments in sphbes\n";
      exit(1);
    }
    order=n+0.5;
    zbessjy(x,order,&rj,&ry,&rjp,&ryp);
    factor=RTPIO2/sqrt(x);
    *sj =factor*rj;
    *sy =factor*ry;
    *sjp=factor*rjp-(*sj)/(2.0*x);
    *syp=factor*ryp-(*sy)/(2.0*x);
}
#undefine RTPIO2

double sphericalbesselj(int n, double x) {
  double sj,sy,sjp,syp;
  zsphbes(n,x,&sj,&sy,&sjp,&syp);
  return sj;
} // sphericalbesselj

#endif
