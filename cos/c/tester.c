#include <stdio.h>
#include <stdlib.h>

void main(int argc, char**argv)
{
    int specchar;
    int ch,i,j;
    int x1,x2,x3,x4,x5;
   
    if(argc>1) {
	printf("Pookie rules!\a\n");
	exit(1);
    }

    specchar=0xb6;
    specchar=0x0d;
    specchar=0xe0;
    i=j=0;
    x1=x2=x3=x4=x5=0;
    while ((ch = fgetc(stdin)) != EOF) {
	i++;
	x1=x2;
	x2=x3;
	x3=x4;
	x4=x5;
	x5=ch;
	if(x3==specchar) {
	    j++;
	    printf("%02x %02x %02x %02x %02x\n",x1,x2,x3,x4,x5);
	}
	/*	if(ch!=specchar)printf("%c",ch); */
    }
    printf("There were %d characters processed.\n",i);
    printf("There were %d of characters %02x.\n",j,specchar);
    exit(0);
}
