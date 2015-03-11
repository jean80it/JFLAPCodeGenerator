#pragma once

#ifdef _DEBUG
#define DEBUG
#endif

#ifdef DEBUG
#define dbgPrintf(...) fprintf(stderr, __VA_ARGS__)
#else
inline void dbgPrintf(...){}
#endif