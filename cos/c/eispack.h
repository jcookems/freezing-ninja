#if !defined(__STD_EISPACK_H_CPP)  // This prevents loading twice!
#define __STD_EISPACK_H_CPP

double d_sign(double *x, double *y) {
    return(copysign(*x,*y));
} /* d_sign */

extern "C" void rs_(int*,int*,double*,double*,int*,double*,double*,double*,int*);
/* This is the real symmetric matrix diag. routine */
extern "C" void rsm_(int*,int*,double*,double*,int*,double*,double*,int*,int*);
/* Real symmetric matrix, rets. all evals, and _m_ evecs */
extern "C" void imtql1_(int*,double*,double*,int*);
/* Real symmetric tridiagonal eigenvalues */
extern "C" void imtql2_(int*,int*,double*,double*,double*,int*);
/* Real symmetric tridiagonal eigenvalues and eigenvectors */
#endif
