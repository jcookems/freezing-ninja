/* bifur.c */

#include <stdio.h>
#include <math.h>
#define u_long unsigned long int

void dotheloop(double *lo, 
	       double *hi, 
	       u_long *steps,
	       u_long *numdatapoints,
	       u_long *datastart)
{
    double stepsize,curval,x,lastx,toler;
    u_long i,j,limit;
    
    toler = 1.e-8;
    limit = *numdatapoints + *datastart;
    stepsize = (*hi-*lo)/(1.*(*steps));
    for (i=0; i<=(*steps) ; i++) {
	curval = (*lo)+i*stepsize;
	x = 0.4999;
	lastx = 0.;
	for (j=0; (j<*datastart) && !((x-lastx)<toler); j++) {
	    lastx = x;
	    x = curval*x*(1.-x);
	}
	printf("%lf, %lu \n",curval,j);
	if (j == *datastart) {
	    x = (x+lastx)*0.5;
	    lastx = 0.;
	    printf("%lf %lf\n",(x-lastx),fabs(x-lastx));
	    for (j=0; (j<*datastart) && !(fabs(x-lastx)<toler); j++) {
		lastx = x;
		x = curval*x*(1.-x);
	    }
	    printf("Second try: %lf, %lu \n",curval,j);
	}
	/*	  for (j=0; j<*numdatapoints; j++) {
		  lastx = x;
		  x = curval*x*(1.-x);
		  printf("%lf %lf\n",curval,x);
		  printf("%g\n",(x-lastx));
		  } */
    }   
}   /* dotheloop */

void main(int argc, char**argv)
{
    u_long steps,datastart,numdatapoints;
    double lo,hi;
    
    if(argc==6) {
	sscanf(argv[1],"%lf",&lo);
	sscanf(argv[2],"%lf",&hi);
	sscanf(argv[3],"%lu",&steps);
	sscanf(argv[4],"%lu",&numdatapoints);
	sscanf(argv[5],"%lu",&datastart);
    }
    else {
	printf("What number to start at? ");
	scanf("%lf",&lo);
	printf("Your number was %lf\n",lo);
	printf("What number to stop at? ");
	scanf("%lf",&hi);
	printf("Your number was %lf\n",hi);
	printf("Number of intervals (# steps-1) ? ");
	scanf("%lu",&steps);
	printf("Your number was %lu.\n",steps);
	printf("How many data points? ");
	scanf("%lu",&numdatapoints);
	printf("Your number was %lu.\n",numdatapoints);
	printf("When to start taking data? "); 
	scanf("%lu",&datastart);
	printf("Your number was %lu.\n",datastart); 
    }
    dotheloop(&lo,&hi,&steps,&numdatapoints,&datastart);
}
