#pragma once
#ifdef UNITYCOM_EXPORTS
#define UNITYCOM_API __declspec(dllexport)
#else
#define UNITYCOM_API __declspec(dllimport)
#endif // UNITYCOM_EXPORTS
#include "stdafx.h"
#include <tchar.h>
#include <string>
#include <vector>
#include <map>

constexpr auto MEGA_BYTES = 1024 * 1024;	// 50 MB
//constexpr auto MAX_BUF_SIZE = 50 * MEGA_BYTES;	// 50 MB 255000
constexpr long PARTICLE_COUNT = 100000;
constexpr long MAX_ARRAY_SIZE = 3 * PARTICLE_COUNT;
constexpr auto MAX_BUF_SIZE = MAX_ARRAY_SIZE * sizeof(float);
constexpr auto NAME_SIZE = 256;
typedef std::map<std::string, HANDLE> hMap;
typedef std::map<std::string, LPVOID> bufMap;
typedef std::map<std::string, long> arrSize;

extern "C"
{
	hMap mMapHandleArray;
	bufMap mMapBufferArray;
	arrSize mMapBufferSizeArray;

	float UNITYCOM_API Add(float a, float b);
	bool UNITYCOM_API IsOpen(char memMapName[NAME_SIZE]);
	int UNITYCOM_API OpenMemoryShare(char memMapName[NAME_SIZE], long bufSize = 1024);
	int UNITYCOM_API WriteMemoryShare(char memMapName[NAME_SIZE], float *val, long offset = 0, long length = -1);
	int UNITYCOM_API ReadMemoryShare(char memMapName[NAME_SIZE], float *val, long offset = 0, long length = -1);
	long UNITYCOM_API GetSize(char memMapName[NAME_SIZE]);
	bool UNITYCOM_API CloseAllMemoryShare();
	bool UNITYCOM_API CloseMemoryShare(char memMapName[NAME_SIZE]);
}
bool AssertExists(char memMapName[NAME_SIZE]);

