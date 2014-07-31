#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#define MAXPATH 64
#define NDIM 1024
#define pi 3.14159265
#define twopi (2.0*pi)

/* =====================================
This function is an interpolator function that assumes that the function
is zero beyond the range of definition, and lineraly interpoleats
elsewhere, including toward zero.
   ===================================== */
float
cont(float f[NDIM],int num,float np)
{
  float frac,ret;
  int n;

  n = np;
  frac = np-n;
  if(n>(num-1))
    ret = 0.;
  else if(n<1)
    ret = f[1] + ( f[2]-f[1] )*(np-1);
  else 
    ret = f[n] + ( f[n+1]-f[n] )*frac;
  /*   printf("np is %f and ret is %f\n",np,ret); */
  return(ret);
} /* cont */

/* =========================================          
This function calcualtes the form factor.
   ========================================= */
float
momformfactor(float psi[NDIM],int num,float q,float deltap)
{
  static float xzero[]={0.0,0.1488743389,0.4333953941,
		    0.6794095682,0.8650633666,0.9739065285};
  static float weights[]={0.0,0.2955242247,0.2692667193,
			  0.2190863625,0.1494513491,0.0666713443};
  float sum,np,intnorm;
  int n;

  intnorm = deltap*deltap*deltap*2.0/(twopi*twopi);
  sum = 0.0;
  for( np=1.0; np<=num; np++)
    for( n=1; n<=5;n++) {
      /* printf("sum is %f\n",sum);
	      printf("np  is %f\n",np);
	      printf("n   is %d\n",n);
	      printf("xzero is %f\n",xzero[n]);
	      printf("q   is %f\n",q); */
      sum += intnorm*np*np*weights[n]*
	cont(psi,num,sqrtf(np*np + 0.25*q*q + np*q*xzero[n])) *
	cont(psi,num,sqrtf(np*np + 0.25*q*q - np*q*xzero[n]));
    }
  return(sum);
} /* momformfactor */

/* ======================================= */

void main()
{
  int ch,i;
  FILE *fp1,*fp2;
  static char pathname1[MAXPATH] = { "mass.mom2.out" };
  static char pathname2[MAXPATH] = { "momformfactor.out" };
  /* Loop Variables */
  int j;
  /* Input parametrs */
  float deltaq,maxq;
  /* Stuff associated with the wavefunction */
  float psitilde[NDIM],p[NDIM],norm,deltap;
  float ff;
  int num,q;

  /*  printf("Input filename: ");
      gets(pathname1); */
  if( *pathname1 == '\0' )
    exit(0);

  /*   printf("Output filename: "); 
       gets(pathname2); */
  if( *pathname2 == '\0' )
    exit(0);

  printf("Maximum momentum to tabulate up to? ");
  scanf("%f",&maxq);
  printf("Step size for q? ");
  scanf("%f",&deltaq);

  /* Input the data from the file */
  fp1 = fopen(pathname1, "r");
  if (fp1 == NULL) {
    printf("cannot open %s\n", pathname1);
    exit(1);
  }
  printf("Input file (%s) opened OK.\n", pathname1);

  fscanf(fp1,"%d",&num);
  printf("The number in your file is %d.\n",num);

  norm = 0.0;
  for( i=1; i<=num; i++ ) {
    fscanf(fp1,"%f %f",&p[i],&psitilde[i]);
    norm += i*i*psitilde[i]*psitilde[i];
  }
  deltap = p[2]-p[1];
  norm = twopi/sqrtf(norm*deltap*deltap*deltap*2.0);
  fclose(fp1);

  /* Normalize the wavefunction */
  for( i=1; i<=num; i++)
    psitilde[i] = psitilde[i]*norm;

  fp2 = fopen(pathname2, "w");
  if (fp2 == NULL) {
    printf("cannot open %s\n", pathname2);
    exit(1);
  }
  printf("Output file (%s) opened OK.\n", pathname2);

  /* Generate the table of the Form Factors */
  for( q=0; q<=maxq/deltaq; q++) {
    ff = momformfactor(psitilde,num,q*deltaq/deltap,deltap);
    printf("q is %d (%f) and ff is %f.\n",q,q*deltaq,ff);
    fprintf(fp2,"%f   %f\n",q*deltaq,ff);
  }
  fclose(fp2);

  exit(0);  
}

