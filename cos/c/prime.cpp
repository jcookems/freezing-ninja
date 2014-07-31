#include <iostream.h>
// My new prime program in C++!
#define u_long unsigned long int

check(u_long *test,u_long diver,u_long *keep)
{
    while( !(*test % diver) ) {
	*test /= diver;
	cout << "A factor is " << diver << "!\n";
	*keep *= diver;
    }
}

fact(u_long start)
{
    u_long test = start;
    if(test) {
	u_long keep = 1;
	check(&test,2,&keep);
	check(&test,3,&keep);
	for (u_long n=1; (test>1) && (12*n*(3*n-1)<test); n++) {
	    check(&test,n*6-1,&keep);
	    check(&test,n*6+1,&keep);
	}
	if(test!=1) {
	    if(test==start)
		cout << "Your number was prime!\n";
	    else
		cout << "The last factor is " << test << "!\n";
	    keep *= test;
	}
	cout << "All the factors multiplied together is " << keep << "\n";
    }
}

main()
{
    cout << "What number to test? ";
    u_long testnumber;
    cin >> testnumber;
    cout << "\nYour number was " << testnumber << ".\n";
    fact(testnumber);
    cout << "Your number still is " << testnumber << ".\n";
}
