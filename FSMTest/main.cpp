#include "keyValueFileParser_Impl.hpp"

void main()
{
	keyValueFileParserImpl p;
	auto values = p.parseFile("Test.txt");

	if (values != nullptr)
	{
		for (auto s : (*values))
		{
			for (auto k : s.second)
			{
				std::cout << s.first << "." << k.first << "='" << k.second << "'" << std::endl;
			}
		}
	}
}