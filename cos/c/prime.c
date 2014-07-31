/* My new prime program in C! */

#include <stdio.h>
#define u_long unsigned long int

void check(u_long *test,u_long diver,u_long *keep)
{
    while( !(*test % diver) ) {
      *test /= diver;
      printf("A factor is %lu!\n",diver);
      *keep *= diver;
    }
}

void fact(u_long start)
{
  u_long test,n,keep;

  test = start;
  if(test!=0) {
    keep = 1;
    check(&test,2,&keep);
    check(&test,3,&keep);
    for (n=1; (test>1) && (12*n*(3*n-1)<test); n++) {
      check(&test,n*6-1,&keep);
      check(&test,n*6+1,&keep);
    }
    if(test!=1) {
      if(test==start)
	printf("Your number was prime!\n");
      else
	printf("The last factor is %lu!\n",test);
      keep *= test;
    }
    printf("All the factors multiplied together is %lu\n",keep);
  }
}

void main()
{
	u_long testnumber;

	printf("What number to test? ");
	scanf("%lu",&testnumber);
	printf("\nYour number was %lu.\n",testnumber);
	fact(testnumber);
	printf("Your number still is %lu.\n",testnumber);
}
