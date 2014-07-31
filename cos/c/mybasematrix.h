// mybasematrix.H   2001/2/3  Jason Cooke
//
// This a base class, for the derived clesses do conversion functions
//
// Note that the functions here cannot be compiled seperately, since 
// we need to know the TYPE of the matrix elements before we can do that.
// However, we can include this in other .H/.C files, and compile those
// if we specify the data type.
//
// This is written so to be able to manipulate matrices of matrices.
// Thus, T can ba a matrix, and we do not write any code here that assumes
// that T is an abelian data-type.  There are particular functions that
// the derived classes have to provide, ones that deal with the exact
// type of the data in the matrix.  Examples of these are
//   * dagger, represented by ~
//     This operator transposes and applys ~ to each of the matrix elements.
//     To complete this definition, we need to make sure that ~ is defined
//     for the base data-type of the matirx, like with
//       inline double_complex operator ~ (double_complex c) {return conj(c);}
//       inline double         operator ~ (double         c) {return      c ;}
//   * Support for auto-conversion from 1x1 matrix to T.  This is for
//     v.v = {{c}} to give c.  To get this, need
//       template <class T>
//       double_complex matrix<T>::operator double_complex (){
//         if(_m->Row != 1 || _m->Col !=1 )
//           _matrix_error( "matrix<T>::operator T: Not exactly one element!");
//         return _m->Val[0];
//       }
//   * This provided support for T*matirx<T>, but for a matrix of matrix, you
//     need to code for T*matrix<matrix<T>>.  This is pretty simple, use
//       m<m<T>> operator * (T no, m<m<T>> m) {
//         int row = m.RowNo();
//         int col = m.ColNo();
//         spinor temp(row,col);
//         for (int i=0;i<row;i++)for (int j=0;j<col;j++)
//           temp(i,j) = no*m(i,j);
//         return temp;
//       }
//   * May also want write fucntions that do explicit type conversion, like
//       inline m<T> operator * (U no, m<T> m) { return T(no)*m; }
//   * Have to provide an interface for the two friend functions, since 
//     if you create a derived class, the friends remain unchanged.  So
//     make an interface for
//       friend ostream& operator << (ostream& o, const matrix<T>& m);
//       friend matrix<T> operator * (const T& no, const matrix<T>& m);
//
//
// Based on complexmatrix.H, which was
// Based on Matrix.h: C++ matrix template class include file (Ver.1.08)
// Copyright (c) 1997-2000 Techsoft Pvt. Ltd. (See License.Txt file.)
// Author: Somnath Kundu
// Web: http://www.techsoftpl.com
// Email: info@techsoftpl.com
//
//  Operator/Method                          Description
//  ---------------                          -----------
//   operator ()   :   This function operator can be used as a
//                     two-dimensional subscript operator to get/set
//                     individual matrix elements.
//
//   operator ~    :   This operator has been used to return transpose of
//                     a matrix.
//
// Note on the internal represenatation of the matrices.  Since
// a*b*c = (a*b)*c, and we like to sandwitch matrices between two
// vectors, it would be computationally more efficient to make
// sum( v(1,i)*m(i,j), i) fast.
// However, I likt to think about M*v= e v.  So I will make that fast.
// Thus, we let m(i,j) = m[i*Col+j]
// Note 0 <= j < Col
//      0 <= i < Row
//
/////////////////////////////////////////////////////////////////

#if !defined(__STD_MYBASEMATRIX_H)  // This prevents loading twice!
#define __STD_MYBASEMATRIX_H

#include <iostream.h> // For cout

inline void _matrix_error (const char* pErrMsg){
  cout << pErrMsg << endl;  exit(1);
}

template <class T> class matrix {
public:
  matrix (const matrix<T>& m);   // copy constructor
  matrix (int row=0, int col=0); // start-up constructor
  ~matrix ();                    // Destructor
  matrix<T>& operator = (const matrix<T>& m);  // Assignment op
  matrix<T>& operator += (const matrix<T>& m); // Assignment and op
  matrix<T>& operator -= (const matrix<T>& m); // Assignment and op
  int RowNo () { return _m->Row; }  // Value extraction method
  int ColNo () { return _m->Col; }  // Value extraction method
  T*  ValVec () { return _m->Val; } // Value extraction method
  T& operator () (int row, int col);  // Subscript operator
  friend ostream& operator << (ostream& o, const matrix<T>& m); // output
  matrix<T> operator + (const matrix<T>& m2);  // Calculation ops, +
  matrix<T> operator - (const matrix<T>& m2);  // Calculation ops, -
  matrix<T> operator * (const matrix<T>& m2);  // Calculation ops, * (mat*mat)
  matrix<T> operator * (const T& no);          // Calculation ops, * (mat*num)
  friend matrix<T> operator * (const T& no, const matrix<T>& m); //* (num*mat)
  matrix<T> operator + ();  // Calculation operators, unary +
  matrix<T> operator - ();  // Calculation operators, unary -
  matrix<T> operator ~ ();  // dagger 
  void Null ();  // Miscellaneous methods, zero matrix
  void SetSize (int row, int col);   // Miscellaneous methods, set size
protected:
  struct base_mat {
    T *Val;
    int Row, Col, Num;
    base_mat (int row, int col, T* v) {    // constructor
      Row = row;
      Col = col;
      Num = Row*Col;
      Val = Num==0 ? 0 : new T[Num];
      for(int i=0;i<Num;i++) Val[i] = ( v ? v[i] : T(0) );
    }
    ~base_mat () { if(Val!=0) delete [] Val; } // destructor
  };
  base_mat *_m;
}; // end of class matrix

// ----------- Start of the function definitions --------------------

// copy constructor
template <class T> inline matrix<T>::matrix (const matrix<T>& m) {
  _m = new base_mat(m._m->Row, m._m->Col, m._m->Val);
}

// constructor
template <class T> inline matrix<T>::matrix (int row=0, int col=0) {
  _m = new base_mat(row, col, 0);
}

// destructor
template <class T> inline matrix<T>::~matrix () {
  delete _m;
} // ~

// change size
template <class T>
void matrix<T>::SetSize (int row, int col) {
  delete _m;
  _m = new base_mat(row, col, 0);
  return;
} // SetSize

// output stream function
template <class T>
ostream& operator << (ostream &ostrm, const matrix<T>& m) {
  ostrm << "(";
  for(int i=0;i<m._m->Row;i++) {
    if(i!=0) ostrm << "\n";
    for(int j=0;j<m._m->Col;j++) {
      if(j!=0) ostrm << "\t";
      ostrm << m._m->Val[i*m._m->Col+j];
    }
  }
  ostrm << ")";
  return ostrm;
} // <<

// set zero to all elements of this matrix
template <class T> void
matrix<T>::Null () {
  for (int i=0; i < _m->Row; i++)
    for (int j=0; j < _m->Col; j++)
      _m->Val[i*_m->Col+j] = T(0);
  return;
} // Null

// assignment operator
template <class T> matrix<T>&
matrix<T>::operator = (const matrix<T>& m) {
  if(&m == this) return *this;
  delete _m;  // wipe out original matrix
  _m = new base_mat( m._m->Row, m._m->Col, m._m->Val);
  return *this;
} // =

// combined operator-assignment operator, +=
template <class T> matrix<T>&
matrix<T>::operator += (const matrix<T>& m) {
  if (this->_m->Row != m._m->Row || this->_m->Col != m._m->Col)
    _matrix_error( "matrix<T>::operator+=: Inconsistent matrix size!");
  for (int i=0; i < m._m->Num; i++)
    this->_m->Val[i] += m._m->Val[i];
  return *this;
} // +=

// combined operator-assignment operator, -=
template <class T> matrix<T>&
matrix<T>::operator -= (const matrix<T>& m) {
  if (this->_m->Row != m._m->Row || this->_m->Col != m._m->Col)
    _matrix_error( "matrix<T>::operator+=: Inconsistent matrix size!");
  for (int i=0; i < m._m->Num; i++)
    this->_m->Val[i] -= m._m->Val[i];
  return *this;
} // -=

// subscript operator to get/set individual elements
template <class T> T& matrix<T>::operator () (int row, int col) {
  if (row >= _m->Row || col >= _m->Col)
    _matrix_error( "matrix<T>::operator(): Index out of range!");
  return _m->Val[row*_m->Col+col];
} // (i,j)

// binary addition operator
template <class T> matrix<T> matrix<T>::operator + (const matrix<T>& m2) {
  //  matrix<T> m1 = *this;
  if (_m->Row != m2._m->Row || _m->Col != m2._m->Col)
    _matrix_error( "matrix<T>::operator+: Inconsistent matrix size!");
  matrix<T> temp(_m->Row,_m->Col);
  for (int i=0; i < _m->Num; i++)
    temp._m->Val[i] = _m->Val[i] + m2._m->Val[i];
  return temp;
} // +

// binary subtraction operator
template <class T> matrix<T> matrix<T>::operator - (const matrix<T>& m2) {
  matrix<T> m1 = *this;
  if (m1._m->Row != m2._m->Row || m1._m->Col != m2._m->Col)
    _matrix_error( "matrix<T>::operator-: Inconsistent matrix size!");
  matrix<T> temp(m1._m->Row,m1._m->Col);
  for (int i=0; i < m1._m->Num; i++)
    temp._m->Val[i] = m1._m->Val[i] - m2._m->Val[i];
  return temp;
} // -

// binary "scalar" multiplication operator (Scalar could be a submatrix!)
template <class T> matrix<T> matrix<T>::operator * (const T& no) {
  matrix<T> m = *this;
  matrix<T> temp(m._m->Row,m._m->Col);
  for (int i=0; i < m._m->Num; i++) temp._m->Val[i] = (m._m->Val[i])*no;
  return temp;
} // *

// binary "scalar" multiplication operator
template <class T> matrix<T>
operator * (const T& no, const matrix<T>& m) {
  matrix<T> temp(m._m->Row,m._m->Col);
  for (int i=0; i < m._m->Num; i++) temp._m->Val[i] = no*(m._m->Val[i]);
  return temp;
} // *

// binary matrix multiplication operator
template <class T> matrix<T> matrix<T>::operator * (const matrix<T>& m2) {
  matrix<T> m1 = *this;
  if (m1._m->Col != m2._m->Row)
    _matrix_error( "matrix<T>::operator*: Inconsistent matrix size!");
  matrix<T> temp(m1._m->Row,m2._m->Col);
  for (int i=0; i < m1._m->Row; i++)
    for (int j=0; j < m2._m->Col; j++) {
      temp._m->Val[i*temp._m->Col+j] =
	m1._m->Val[i*  m1._m->Col+0] *
	m2._m->Val[0*  m2._m->Col+j];  // new!  Saves us when a matrix
      for (int k=1; k < m1._m->Col; k++)
	temp._m->Val[i*temp._m->Col+j] +=
	  m1._m->Val[i*  m1._m->Col+k] *
	  m2._m->Val[k*  m2._m->Col+j];
    }
  return temp;
} // *

// unary + operator
template <class T> inline matrix<T> matrix<T>::operator + () { 
  return *this;
} // +

// unary - operator
template <class T> matrix<T> matrix<T>::operator - () {
  matrix<T> m = *this;
  matrix<T> temp(m._m->Row,m._m->Col);
  for (int i=0; i < m._m->Num; i++) temp._m->Val[i] = -m._m->Val[i];
  return temp;
} // -

// unary dagger operator
template <class T> matrix<T> matrix<T>::operator ~ () {
  matrix<T> m = *this;
  matrix<T> temp(m._m->Col,m._m->Row);
  for (int i=0; i < m._m->Row; i++)
    for (int j=0; j < m._m->Col; j++)
      temp._m->Val[j*temp._m->Col+i] = ~(m._m->Val[i*m._m->Col+j]);
  return temp;
} // ~


#endif //__STD_COMPLEXMATRIX_H
