#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "/home/jrcooke/c/numerical.recipies/gauleg.c"
#define NDIM 100

void main()
{
    double x[NDIM+1], w[NDIM+1];
    double x1, x2;
    int n,i;
    double temp1,temp2;

    printf("What value for n? ");
    scanf("%d",&n);
    printf("%d\n",n);
    x1 = -1.0;
    x2 =  1.0;
    gauleg(x1,x2,x,w,n);
    for( i=1; i<=n; i++) {
	printf("For %d: x is %f and w is %f.\n",i,x[i],w[i]);
    }
    printf("Now let's try to do some integrations! Let's do sine!\n");
    temp1 = 0.;
    temp2 = 0.;
    for( i=1; i<=n; i++) {
	temp1 += sin(x[i])*w[i];
	temp2 += cos(x[i])*w[i];
    };
    printf("The integration value for sine was: %f\n",temp1);
    printf("The integration value for cos  was: %f\n",temp2);
	
    exit(0);  
}

