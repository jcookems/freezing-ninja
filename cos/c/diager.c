#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <eispack.h>

double d_sign(double *x, double *y)
{
    return(copysign(*x,*y));
}

void P(int i,int *ri,int *rj,int *rk, int n)
{
    *ri = i%n;
    *rj = i/n%n;
    *rk = (i/n)/n%n;
}

int Pinv(int i, int j, int k, int n)
{
    return(i+n*j+n*n*k);
}

void main()
{
  int NDIM,num,numcube,i,j,k,ierr,matz;
  int ri,rj,rk;
  double *ham,*evals,*evecs,*fv1,*fv2;
  double *testvec,*testvec2,prod,*norm;
  double delta,moverdeltasq,r;

  /*  printf("Max Matrix size? ");
  scanf("%d",&NDIM); */ NDIM = 100;

  printf("Matrix size? ");
  scanf("%d",&num);
  numcube = num*num*num;
  NDIM = numcube;
  if(numcube>NDIM) {
      printf("num^3 = %d > %d = NDIM\n",numcube,NDIM);
      exit(1);
  };

  ham   = malloc(sizeof(double)*NDIM*NDIM);
  evals = malloc(sizeof(double)*NDIM);
  evecs = malloc(sizeof(double)*NDIM*NDIM);
  fv1   = malloc(sizeof(double)*NDIM);
  fv2   = malloc(sizeof(double)*NDIM);
  testvec  = malloc(sizeof(double)*NDIM);
  testvec2 = malloc(sizeof(double)*NDIM);

  /*  printf("What is delta? ");
  scanf("%lf",&delta); */ delta = 1.;
  printf("delta= %f\n",delta);

  for( i=0; i<numcube; i++ )
      for( j=0; j<numcube; j++ )
	  ham[i+j*NDIM] = 0;

  moverdeltasq = -1./(delta*delta);
  printf("moverdeltasq= %f\n",moverdeltasq);
  for( i=0; i<numcube; i++ ) {
      P(i,&ri,&rj,&rk,num);
      /*     printf("i:%d j:%d k:%d \n",ri,rj,rk); */
      r = delta*sqrt( (ri-0.5*(num-1))*(ri-0.5*(num-1))+
		      (rj-0.5*(num-1))*(rj-0.5*(num-1))+
		      (rj-0.5*(num-1))*(rj-0.5*(num-1)) );
      ham[i+i*NDIM] = -6.*moverdeltasq-1./r;
      if( ri   >0  ) ham[i+(i-1      )*NDIM] = moverdeltasq;
      if((ri+1)<num) ham[i+(i+1      )*NDIM] = moverdeltasq;
      if( rj   >0  ) ham[i+(i-num    )*NDIM] = moverdeltasq;
      if((rj+1)<num) ham[i+(i+num    )*NDIM] = moverdeltasq;
      if( rk   >0  ) ham[i+(i-num*num)*NDIM] = moverdeltasq;
      if((rk+1)<num) ham[i+(i+num*num)*NDIM] = moverdeltasq;
  };

  /*  for( i=0; i<numcube; i++ ) {
      for( j=0; j <numcube; j++)
      printf("%f ",ham[i+j*NDIM]);
      printf("\n");
  } */

  /* subroutine rs(nm,n,a,w,matz,z,fv1,fv2,ierr)

     double precision a(nm,n),w(n),z(nm,n),fv1(n),fv2(n)
     integer n,nm,ierr,matz

     nm  must be set to the row dimension of the two-dimensional
     array parameters as declared in the calling program
     dimension statement.

     n  is the order of the matrix  a.

     a  contains the real symmetric matrix.
     
 Out w  contains the eigenvalues in ascending order.

     matz  is an integer variable set equal to zero if
     only eigenvalues are desired.  otherwise it is set to
     any non-zero integer for both eigenvalues and eigenvectors.

 Out z  contains the eigenvectors if matz is not zero.

 Tmp fv1
     fv2  are temporary storage arrays.

 Out ierr  is an integer output variable set equal to an error
     completion code described in the documentation for tqlrat
     and tql2.  the normal completion code is zero.
     */

  matz = 1;
  rs_(&NDIM,&numcube,ham,evals,&matz,evecs,fv1,fv2,&ierr);
  printf("Errors? Ierr:%d\n",ierr);

  /*  for( i=0; i<numcube; i++ ) { */
  for( i=0; i<10; i++ ) {
      printf("Eval: %f  Evec: ",evals[i]);
      /*     for( j=0; j <numcube; j++)
	  printf("%f ",evecs[j+i*NDIM]); */
      printf("\n");
  };

  for ( i=0; i<numcube; i++ ) {
      /*      printf("evecs[%d] = %f\n",i,evecs[i]); */
      testvec[i] = evecs[i];
  };

  prod = 0.;
  norm = malloc(sizeof(double)*10*10);
  printf("blah\n");
  for ( i=0; i<10; i++ )
      for ( j=0; j<10; j++) 
	  norm[i + 10*j] = 0.;

  printf("Lets test this with the first eigenvector:\n");
  for( i=0; i<numcube; i++ ) {
      testvec2[i] = 0.;
      for( j=0; j <numcube; j++)
	  testvec2[i] += ham[j+i*NDIM]*testvec[j];
      prod += testvec2[i]*testvec[i];
      for (j=0;j<10;j++)
       	  for (k=0;k<10;k++)
	      norm[j+10*k] += evecs[i+j*NDIM]*evecs[i+k*NDIM];
      /*      printf("testvec[%i] = %f, norm = %f \n",i,testvec[i],norm); */
  };
  for (j=0;j<10;j++) {
      for (k=0;k<10;k++)
	  printf("%f ",norm[j+10*k]);
      printf("\n");
  }

  printf("Eval (from blah): %f\n",prod);

/*  printf("All done!  Give me a number to leave: ");
  scanf("%f",&delta); */

  exit(0);  
}

