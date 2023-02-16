#pragma once

#define _SILENCE_CXX17_CODECVT_HEADER_DEPRECATION_WARNING
#define _ITERATOR_DEBUG_LEVEL 0

#include <windows.h>
#include <ctffunc.h>
#include <d2d1.h>
#include <d2d1_3.h>
#include <d3d11.h>
#include <ddraw.h>
#include <dsound.h>
#include <dwrite.h>
#include <gdiplus.h>
#include <Mmreg.h>
#include <msctf.h>
#include <MSAcm.h>

#include <codecvt>
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
#include "Util/membuf.h"
#include "Util/MemoryUtil.h"
#include "Util/MemoryUnprotector.h"
#include "Util/StringUtil.h"

#include "PE/PE.h"

#include "CompilerSpecific/Enumerations.h"
#include "CompilerSpecific/CompilerHelper.h"
#include "CompilerSpecific/Rtti/BorlandTypeDescriptor.h"
#include "CompilerSpecific/Rtti/MsvcRttiCompleteObjectLocator.h"

#include "Subtitles/SubtitleLine.h"
#include "Subtitles/SubtitleDocument.h"
#include "Subtitles/SubtitleRenderer.h"

#include "Patches/BabelPatch.h"
#include "Patches/EnginePatches.h"

#include "Proxy.h"
#include "ImportHooker.h"
#include "Font.h"
#include "FontManager.h"
#include "SjisTunnelEncoding.h"
#include "Win32AToWAdapter.h"
#include "Proportionalizer.h"
#include "GdiProportionalizer.h"
#include "D2DProportionalizer.h"
#include "LocaleEmulator.h"

#include "ImeListener.h"
