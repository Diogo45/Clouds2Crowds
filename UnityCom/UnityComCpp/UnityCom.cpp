// UnityCom.cpp : Define as funções exportadas para o aplicativo DLL.
//

// Isso aqui já está funcionando:
//		- Dicionário de memory maps:
//			- nome, handle
//			- nome, buffer
//			- nome, array size
//		- tamanho do buffer parametrizado: quantidade de floats no array
// @todo: refazer a conexão com Unity:
//		- ler/escrever arrays de float no mapa de memoria

#include "stdafx.h"

//typedef std::basic_string<TCHAR> tstring;
#include "UnityCom.h"

extern "C"
{
	float UNITYCOM_API Add(float a, float b)
	{
		return a + b;
	}

	bool UNITYCOM_API IsOpen(char memMapName[NAME_SIZE])
	{
		if (!mMapHandleArray.empty()) {
			HANDLE hMapFile = mMapHandleArray.at(memMapName);
			if (hMapFile != NULL) {
				return true;
			}
		}
		return false;
	}

	//////////////////////////////////////////////////
	// Returns:
	// 0  - Success! A new memory map is opened/created.
	// >0 - The array size of existing memory map.
	// <0 - an error has occurred:
	//			-1 = Array too large
	//			-2 = Could not create file mapping object
	//			-3 = Could not create map view of the file
	int UNITYCOM_API OpenMemoryShare(char memMapName[NAME_SIZE], long _bufSize)
	{
		if (_bufSize > MAX_ARRAY_SIZE) {
			//printf("Array too large!");
			return -1;
		}
		HANDLE hMapFile;
		LPVOID pBuf;
		bool exist = AssertExists(memMapName);
		if (exist) {
			if (mMapHandleArray.at(memMapName) != NULL) {
				hMapFile = mMapHandleArray.at(memMapName);
				pBuf = mMapBufferArray.at(memMapName);
				//printf("Mem name already mapped. (%d)", mMapHandleArray.size());
				return mMapHandleArray.size();
			}
		}

		TCHAR szName[NAME_SIZE];
		swprintf(szName, NAME_SIZE, L"%hs", memMapName);
		hMapFile = CreateFileMapping(
			INVALID_HANDLE_VALUE,    // use paging file
			NULL,                    // default security
			PAGE_READWRITE,          // read/write access
			0,                       // maximum object size (high-order DWORD)
			_bufSize*(sizeof(float)), // maximum object size (low-order DWORD)
			szName);                 // name of mapping object

		if (hMapFile == NULL)
		{
			//_tprintf(TEXT("Could not create file mapping object (%d).\n"), GetLastError());
			return -2;
		}
		pBuf = MapViewOfFile(hMapFile,   // handle to map object
			FILE_MAP_ALL_ACCESS, // read/write permission
			0,
			0,
			_bufSize*(sizeof(float)));

		if (pBuf == NULL)
		{
			//_tprintf(TEXT("Could not map view of file (%d).\n"), GetLastError());
			CloseHandle(hMapFile);
			return -3;
		}

		// Add new handler, buffer and array size;
		if (exist) {
			mMapHandleArray.at(memMapName) = hMapFile;
			mMapBufferArray.at(memMapName) = pBuf;
			mMapBufferSizeArray.at(memMapName) = _bufSize;
		}
		else {
			mMapHandleArray.insert(std::pair<char*, HANDLE>(memMapName, hMapFile));
			mMapBufferArray.insert(std::pair<char*, LPVOID>(memMapName, pBuf));
			mMapBufferSizeArray.insert(std::pair<char*, long>(memMapName, _bufSize));
		}
		return 0;
	}

	int UNITYCOM_API WriteMemoryShare(char memMapName[NAME_SIZE], float *val, long offset, long length)
	{
		if (offset < 0) {
			offset = 0;
		}

		if (AssertExists(memMapName)) {
			long size = mMapBufferSizeArray.at(memMapName);
			if (length < 0) {
				length = size;
			}
			if ((offset + length) > size) {
				// Index out of range
				return 1;
			}
			LPVOID pBuf = mMapBufferArray.at(memMapName);
			LPVOID dest = (LPVOID)((long long)pBuf + offset * sizeof(float));
			//CopyMemory(pBuf, val, length * sizeof(float));
			CopyMemory(dest, val, length * sizeof(float));
			return 0;
		}
		return -1;
	}

	int UNITYCOM_API ReadMemoryShare(char memMapName[NAME_SIZE], float *val, long offset, long length)
	{
		if (offset < 0) {
			offset = 0;
		}

		if (AssertExists(memMapName)) {
			long size = mMapBufferSizeArray.at(memMapName);
			if (length < 0) {
				length = size;
			}
			if ((offset + length) > size) {
				// Index out of range
				return 1;
			}
			LPVOID pBuf = mMapBufferArray.at(memMapName);
			LPVOID orig = (LPVOID)((long long)pBuf + (long long)(offset * sizeof(float)));
			//CopyMemory(val, pBuf, length * sizeof(float));
			try {
				CopyMemory(val, orig, length * sizeof(float));
			}
			catch (int e) {

			}

			return 0;
		}
		return -1;
	}

	long UNITYCOM_API GetSize(char memMapName[NAME_SIZE])
	{
		if (AssertExists(memMapName)) {
			return mMapBufferSizeArray.at(memMapName);
		}
		return -1;
	}

	bool UNITYCOM_API CloseMemoryShare(char memMapName[NAME_SIZE])
	{
		if (AssertExists(memMapName)) {
			LPVOID pBuf = mMapBufferArray.at(memMapName);
			HANDLE hMapFile = mMapHandleArray.at(memMapName);

			UnmapViewOfFile(pBuf);
			CloseHandle(hMapFile);
			return true;
		}
		return false;
	}

	bool UNITYCOM_API CloseAllMemoryShare()
	{
		bufMap::iterator itMemMapBufferArray;
		for (itMemMapBufferArray = mMapBufferArray.begin(); itMemMapBufferArray != mMapBufferArray.end(); ++itMemMapBufferArray) {
			LPVOID pBuf = itMemMapBufferArray->second;
			UnmapViewOfFile(pBuf);
			mMapBufferArray.erase(itMemMapBufferArray);
		}

		hMap::iterator itMemMapHandArray;
		for (itMemMapHandArray = mMapHandleArray.begin(); itMemMapHandArray != mMapHandleArray.end(); ++itMemMapHandArray) {
			HANDLE hMapFile = itMemMapHandArray->second;
			CloseHandle(hMapFile);
			mMapHandleArray.erase(itMemMapHandArray);
		}
		return true;
	}
}

bool AssertExists(char memMapName[NAME_SIZE]) {
	hMap::iterator itMemMApHandArray;
	itMemMApHandArray = mMapHandleArray.find(memMapName);
	if (itMemMApHandArray == mMapHandleArray.end()) {
		return false;
	}
	return true;
}