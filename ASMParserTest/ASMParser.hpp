#pragma once

class ASM_Parser
{
    enum class States {
        skip_WS_1 = 0,
        comment = 1,
        label = 2,
        coll_op = 3,
        skip_WS_2 = 4,
        coll_arg1 = 5,
        skip_WS_3 = 6,
        arg_2_addr = 7,
        arg2_n = 8,
        arg_2_n_f = 9,
        error = 10,
        Count
    };


protected:

    typedef void(ASM_Parser::*pActionFn) ();

    States state = States::skip_WS_1;
    States nextState = States::skip_WS_1;

    static const pActionFn actionsTable[128][11];
    static const ASM_Parser::States transitionsTable[128][11];

    int currentInput = 0;
    bool got13 = false;
    int line = 0;
    int column = 0;

#pragma region FSM actions declaration

    virtual void fsmAction_error() = 0;
    virtual void fsmAction_name_stor() = 0;
    virtual void fsmAction_instr_done() = 0;
    virtual void fsmAction_n_stor_f() = 0;
    virtual void fsmAction_addr_stor() = 0;
    virtual void fsmAction_n_stor() = 0;
    virtual void fsmAction_none() = 0;
    virtual void fsmAction_lbl_done() = 0;

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

