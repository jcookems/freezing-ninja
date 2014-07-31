// ConsoleApplication1.cpp : main project file.

#include "stdafx.h"

using namespace System;

int main(array<System::String ^> ^args)
{
	Console::WriteLine(zlibVersion());

#ifdef HAS_ZLIB
	Console::WriteLine( "Has zlib!\n");
#else
	return 0;
#endif

	Console::WriteLine(L"Hello World");
	return 0;
}
