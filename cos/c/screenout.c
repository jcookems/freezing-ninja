#include <stdio.h>
#include <stdlib.h>

void changestyle(int n)
/*	n=0	Reset to normal
	n=1	Bold
	n=4	Underline
	n=5	Blink
	n=7	Inverse
	n=other	No change
These properties are culmative, with the exception of the reset.
*/
{
  printf("%c[%dm",27,n);
}

void subwindow(int topy, int bottomy)
/* This makes a subwindow of the screen, and this is where the scrolling occurs
*/
{
  printf("%c[%d;%dr",27,topy,bottomy);
}

void relocate(int x, int y)
/* This repositions the pointer.  If the coordinated place the pointer off the 
edge of the screen, the pointer is simply moved to the closest point on the 
screen.  If the xterm window is enlarged, more points become available.
*/
{
  printf("%c[%d;%dH",27,y,x);
}

void clearscreen()
{
  printf("%c[1;24r",27);	/* Make scrollable window whole window */
  printf("%c[H%c[J",27,27);	/* Relocate to upper left, and clear the 
				   current line and all lines below */
  printf("%c[0m",27);		/* Make normal type again */
}

void alldone()
{
  printf("%c[1;24r",27);
  printf("%c[m",27);
  printf("%c[23H",27);
}

void main() 
{
  clearscreen();
  relocate(35,4);
  changestyle(7);
  printf("            ");
  relocate(35,6);
  printf("            ");
  relocate(35,5);
  changestyle(4);
  printf(" Here I am! ");
  changestyle(5);
  relocate(41,5);
  printf("I");
  alldone();
}
