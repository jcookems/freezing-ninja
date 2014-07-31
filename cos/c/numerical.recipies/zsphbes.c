#include <math.h>
#define RTPIO2 1.25331413731550025120788264241

void zsphbes(int n, double x, double *sj, double *sy, double *sjp, double *syp)
     /* Returns spherical Bessel functions j_n(x), y_n(x), and their
	derivatives j'_n(x), y'_n(x) for integer n.  Note that
	j_n(x) = sqrt(pi/2x) J_{n+1/2}(x)
	y_n(x) = sqrt(pi/2x) Y_{n+1/2}(x)     */
{
    void zbessjy(double x, double xnu, double *rj, double *ry, double *rjp,
		 double *ryp);
    void nrerror(char error_text[]);
    
    double factor,order,rj,rjp,ry,ryp;
    
    if (n < 0 || x <= 0.0) nrerror("bad arguments in sphbes");
    order=n+0.5;
    zbessjy(x,order,&rj,&ry,&rjp,&ryp);
    factor=RTPIO2/sqrt(x);
    *sj =factor*rj;
    *sy =factor*ry;
    *sjp=factor*rjp-(*sj)/(2.0*x);
    *syp=factor*ryp-(*sy)/(2.0*x);
}
