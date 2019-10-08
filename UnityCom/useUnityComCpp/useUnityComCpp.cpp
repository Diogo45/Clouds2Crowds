// useUnityComCpp.cpp : Este arquivo contém a função 'main'. A execução do programa começa e termina ali.
//

#include "pch.h"
#include <iostream>
#include <time.h>
#include "../UnityComCpp/UnityCom.h"

void PrintOptions();
void UsePreset();

char mmName[256] = "unityMemMap";
constexpr long ARRAY_SIZE = 1;
//constexpr long ARRAY_SIZE = 255000;

int main()
{
	int op;
	float v[ARRAY_SIZE];
	float val;
	//long bufSize = ARRAY_SIZE * sizeof(float);
	long bufSize = ARRAY_SIZE;
	std::string strOp;
	long size;

	while (true) {
		PrintOptions();
		std::cin >> strOp;
		op = std::atoi(strOp.c_str());

		switch (op)
		{
		case 1:
			//std::cout << "entre nome do mapa de memória:";
			//std::cin >> mmName;
			//std::cout << "entre com o tamanho em quantidade de floats:";
			//std::cin >> bufSize;
			std::cout << "OpenMemoryShare(" << mmName << ", " << bufSize << ") return:" << OpenMemoryShare(mmName, bufSize) << std::endl;
			system("pause");
			break;
		case 2:
			//std::cout << "entre nome do mapa de memória:";
			//std::cin >> mmName;
			size = GetSize(mmName);
			for (int i = 0; i < ARRAY_SIZE; i++) {
				std::cout << "entre valor para escrever:";
				std::cin >> val;
				v[i] = val;
			}
			WriteMemoryShare(mmName, v);
			system("pause");
			break;
		case 3:
			//std::cout << "entre nome do mapa de memória:";
			//std::cin >> mmName;
			for (int i = 0; i < ARRAY_SIZE; i++)
			{
				v[i] = 0;
			}
			ReadMemoryShare(mmName, v);
			size = GetSize(mmName);
			for (long i = 0; i < ARRAY_SIZE; i++)
			{
				std::cout << "valor lido:" << v[i] << std::endl;
			}
			system("pause");
			break;
		case 4:
			UsePreset();
			system("pause");
			break;
		case 5:
			std::cout << "Entre nome do preset:";
			std::cin >> mmName;
			system("pause");
			break;
		case 6:
			CloseAllMemoryShare();
			return 0;
			break;
		default:
			std::cout << "Opção inválida!";
			system("pause");
			break;
		}
	}


}

void PrintOptions()
{
	system("CLS");
	std::cout << "Opções:" << std::endl << std::endl;
	std::cout << "1- Abrir novo mapa de memória compartilhada." << std::endl;
	std::cout << "2- Escrever dado na memória compartilhada." << std::endl;
	std::cout << "3- Ler dado da memória compartilhada." << std::endl;
	std::cout << "4- Usar preset." << std::endl;
	std::cout << "5- Mudar nome do preset.(" << mmName << ")" << std::endl << std::endl;
	std::cout << "6- Fechar e sair." << std::endl << std::endl;
	std::cout << "Sua escolha:";
}

void UsePreset() {
	time_t start_time;
	time_t lap;
	time_t now;
	double seconds;
	std::vector<float> fwList;
	std::vector<float> frList(ARRAY_SIZE);
	int i;
	time(&start_time); lap = start_time;
	std::cout << "Opening with settings:\n Name:" << mmName << "\n Size:" << ARRAY_SIZE << "\nReturned:";
	std::cout << OpenMemoryShare(mmName, ARRAY_SIZE) << "\n";
	time(&now);
	seconds = difftime(lap, now);
	lap = now;
	std::cout << "done!  t: " << seconds << " s\n";

	std::cout << "Generating vector of data...";
	for (i = 0; i < ARRAY_SIZE; i++) {
		float f = (float)i + 0.5;
		fwList.push_back(f);
	}
	time(&now);
	seconds = difftime(lap, now);
	lap = now;
	std::cout << "done!  t: " << seconds << " s\n";

	std::cout << "Writing to shared memory...";
	WriteMemoryShare(mmName, fwList.data());
	time(&now);
	seconds = difftime(lap, now);
	lap = now;
	std::cout << "done!  t: " << seconds << " s\n";
	std::cout << "Reading from shared memory...";
	ReadMemoryShare(mmName, frList.data());
	time(&now);
	seconds = difftime(lap, now);
	lap = now;
	std::cout << "done!  t: " << seconds << " s\n";
	std::cout << "Print output:\n";
	for (i = 0; i < ARRAY_SIZE; i=i+(ARRAY_SIZE/20)) {
		std::cout << "f[" << i << "]:\t" << frList[i] << "\n";
	}
	time(&now);
	seconds = difftime(lap, now);
	lap = now;
	std::cout << "done!  t: " << seconds << " s\n";
}
