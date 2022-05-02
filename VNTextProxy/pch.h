#pragma once

#define WIN32_LEAN_AND_MEAN

#include <windows.h>
#include <d2d1.h>
#include <d2d1_3.h>
#include <Mmreg.h>
#include <msctf.h>
#include <ctffunc.h>
#include <ddraw.h>
#include <dsound.h>
#include <dwrite.h>
#include <d3d11.h>
#include <cstdlib>
#include <algorithm>
#include <functional>
#include <map>
#include <ranges>
#include <set>
#include <string>
#include <sstream>
#include <vector>
#include "../external/Detours/detours.h"

#include "Util/ComPtr.h"
#include "Util/Path.h"
#include "Util/MemoryUtil.h"
#include "Util/MemoryUnprotector.h"
#include "Util/StringUtil.h"

#include "Proxy.h"
#include "ImportHooker.h"
#include "Font.h"
#include "FontManager.h"
#include "SjisTunnelEncoding.h"
#include "Win32AToWAdapter.h"
#include "Proportionalizer.h"
#include "GdiProportionalizer.h"
#include "D2DProportionalizer.h"
#include "EnginePatches.h"
#include "LocaleEmulator.h"

#include "ImeListener.h"
