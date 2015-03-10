#pragma once

class keyValueFileParser
{
    enum class States {
        skipSpaces0 = 0,
        collectKey = 1,
        skipSpaces1 = 2,
        skipSpaces2 = 3,
        collectValue = 4,
        comment = 5,
        skipSpaces3 = 6,
        collectSection = 7,
        skipSpaces4 = 8,
        skipSpaces5 = 9,
        escape = 10,
        error = 11,
        Count
    };


protected:

    typedef void(keyValueFileParser::*pActionFn) ();

    States state = States::skipSpaces0;
    States nextState = States::skipSpaces0;

    static const pActionFn actionsTable[128][12];
    static const keyValueFileParser::States transitionsTable[128][12];

    int currentInput = 0;
    bool got13 = false;
    int line = 0;
    int column = 0;

#pragma region FSM actions declaration

    virtual void fsmAction_error() = 0;
    virtual void fsmAction_collect() = 0;
    virtual void fsmAction_none() = 0;
    virtual void fsmAction_keyValueReady() = 0;
    virtual void fsmAction_keyReady() = 0;
    virtual void fsmAction_incValueSpaces() = 0;
    virtual void fsmAction_sectionReady() = 0;
    virtual void fsmAction_addSpaces() = 0;
    virtual void fsmAction_resetValueSpaces() = 0;

// FSM actions declaration
#pragma endregion

protected:

	
    bool feed(int c)
	{
		currentInput = c;
	
		if (got13 && currentInput == 10)
		{
			got13 = false;
			return true;
		}

		if (currentInput == 13)
		{
			got13 = true;
			++line;
			column = 0;
		}
		else
		{
			if (currentInput == 10)
			{
				++line;
				column = 0;
			}
			else
			{
				++column;
			}
		}

		got13 = false;

		nextState = transitionsTable[currentInput][(int)state];
		(this->*actionsTable[currentInput][(int)state])();
		state = nextState;

        return (state!=States::error);
	}

};

