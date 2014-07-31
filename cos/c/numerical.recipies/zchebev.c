double zchebev(double a, double b, double *c, int m, double x)
{
    float d=0.0,dd=0.0,sv,y,y2;
    int j;
    void nrerror(char *);
    
    if ((x-a)*(x-b) > 0.0) nrerror("x not in range in routine CHEBEV");
    y2=2.0*(y=(2.0*x-a-b)/(b-a));
    for (j=m-1;j>=1;j--) {
	sv=d;
	d=y2*d-dd+c[j];
	dd=sv;
    }
    return y*d-dd+0.5*c[0];
}
