#define NUSE1 5
#define NUSE2 5

void zbeschb(double x, double *gam1, double *gam2, double *gampl, 
	     double *gammi)
     /* Evaluates Gamma_1 and Gamma_2 by Chebyshev expansion for
	|x| <= 1/2.  Also returns 1/Gamma(1+x) and 1/Gamma(1-x).
	If converting to double precision, set NUSE1 = 7, NUSE2 = 8. */
{
    double zchebev(double a, double b, double c[], int m, double x);
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
