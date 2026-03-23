// Font accessor helpers for P/Invoke consumers (e.g. C#).
// P/Invoke cannot directly import extern global variables from a shared lib,
// so these thin wrappers return a pointer to each font struct.

#include "fonts.h"

const sFONT* EPD_GetFont8(void)  { return &Font8; }
const sFONT* EPD_GetFont12(void) { return &Font12; }
const sFONT* EPD_GetFont16(void) { return &Font16; }
const sFONT* EPD_GetFont20(void) { return &Font20; }
const sFONT* EPD_GetFont24(void) { return &Font24; }
