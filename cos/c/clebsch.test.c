/* clebsch.test.c   2000/1/26   Jason Cooke
 *
 *    gcc -ggdb clebsch.test.c -o clebsch.test -lm
 *
 */

#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "/home/jrcooke/c/ylm.h"

void main() {
    double j1,m1;
    double j2,m2;
    double j ,m ;
    int test;
    /* */
    int num, i1, i2;
    double *thetas,*YlmDes,ylm;

    test=1;
    printf("clebsch.test: pid=%d, ppid=%d\n",getpid(),getppid());

    for(j1=-0.;j1<2.+0.1;j1+=1.)
	for(m2=-1.0;m2<1.0+0.1;m2+=1.) {
	    printf("C1(");
	    printinthalfint(doubleint(j1));
	    printf(",");
	    printinthalfint(doubleint(m2));
	    printf(")=%f\n",clebschgordan(j1,1-m2,1,m2,1,1) );
	};
    for(m1=-0.5;m1<0.5+0.1;m1+=1.)
	for(m=-1.0;m<1.0+0.1;m+=1.) {
	    printf("C2(");
	    printinthalfint(doubleint(m1));
	    printf(",");
	    printinthalfint(doubleint(m));
	    printf(")=%f\n",clebschgordan(0.5,m1,0.5,m-m1,1,m) );
	};

    num = 3;
    thetas = malloc(sizeof(double)*num);
    for(i1=0;i1<num;i1++)
	thetas[i1] = 0.1*i1;
    
    for(i1=0;i1<num;i1++)
	for(i2=0;i2<=i1;i2++) {
	    ylm = getYlmDe(i1,i2,0.4);
	    printf("Y[%d,%d,%f,0]=% f\n",i1,i2,0.4,ylm);
	}

    exit(0);
}

