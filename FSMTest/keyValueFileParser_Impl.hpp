#define _CRT_SECURE_NO_WARNINGS

#include <iostream>
#include <sstream>
#include <string>
#include <cstdio>
#include <unordered_map>
#include "keyValueFileParser.hpp"

class keyValueFileParserImpl : public keyValueFileParser
{
private:
	std::stringstream ss;
	std::stringstream whitespaces;
	std::string key;
	std::string value;
	std::string section;
	
	void fsmAction_resetValueSpaces() { whitespaces.str(std::string()); whitespaces.clear(); whitespaces << (char)currentInput; }
	void fsmAction_incValueSpaces() { whitespaces << (char)currentInput; }
	virtual void fsmAction_addSpaces() { ss << whitespaces.str(); whitespaces.str(std::string()); whitespaces.clear(); ss << ((char)currentInput); }

	void fsmAction_error() override
	{
		std::cout << "Unexpected input '"<< ((char)currentInput) <<"' (#"<< currentInput<<") at line " << line << ", column "<< column << " while in state " << (int)(this->state) << "." << std::endl;
	}

	void fsmAction_none() override
	{
		// do nothing
	}

	void fsmAction_collect() override
	{
		ss << ((char)currentInput);
	}

	void fsmAction_keyReady() override
	{
		key = ss.str();
		ss.str(std::string());
		ss.clear();
	}

	void fsmAction_keyValueReady() override
	{
		value = ss.str();
		ss.str(std::string());
		ss.clear();

		(*values)[section][key] = value;

		// std::cout << "pair found: " << key << " =' " << value << "'" << std::endl;
	}

	void fsmAction_sectionReady()
	{
		section = ss.str();
		ss.str(std::string());
		ss.clear();
	}

public:
	typedef std::unordered_map < std::string, std::unordered_map<std::string, std::string> > SectionKeyValue;

	SectionKeyValue* values;

	SectionKeyValue* parseFile(char* filename)
	{
		values = new std::unordered_map<std::string, std::unordered_map<std::string, std::string>>();

		const int bufSize = 65535;
		char buf[bufSize] = {};

		FILE *f;
		f = fopen(filename, "r");
		int bytesRead = 0;
		do
		{
			bytesRead = fread(&buf, 1, bufSize, f);
			for (int i = 0; i < bytesRead; ++i)
			{
				if (!feed(buf[i]))
				{
					return values; // return null ??
				}
			}
		} while (bytesRead == bufSize);
		
		return values;
	}
};