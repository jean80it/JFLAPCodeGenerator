// auto generated with JFLAPCodeGenerator by Giancarlo Todone https://github.com/jean80it/JFLAPCodeGenerator

#pragma once

class ASM_Parser
{
    enum class States {
        skip_WS_1 = 0,
        comment = 1,
        label = 2,
        arg2_name = 3,
        arg2_char = 4,
        coll_op = 5,
        skip_WS_2 = 6,
        coll_arg1 = 7,
        arg2_char_done = 8,
        skip_WS_3 = 9,
        arg_2_addr = 10,
        arg2_n = 11,
        arg_2_n_f = 12,
        error = 13,
        Count
    };


protected:

    typedef void(ASM_Parser::*pActionFn) ();

    States state = States::skip_WS_1;
    States nextState = States::skip_WS_1;

    static const pActionFn actionsTable[128][14];
    static const ASM_Parser::States transitionsTable[128][14];

    int currentInput = 0;
    bool got13 = false;
    int line = 0;
    int column = 0;

#pragma region FSM actions declaration

    virtual void fsmAction_error() = 0;
    virtual void fsmAction_arg1_stor() = 0;
    virtual void fsmAction_none() = 0;
    virtual void fsmAction_name_stor() = 0;
    virtual void fsmAction_n_stor() = 0;
    virtual void fsmAction_set_arg2_char() = 0;
    virtual void fsmAction_instr_done() = 0;
    virtual void fsmAction_neg_n() = 0;
    virtual void fsmAction_arg2_name_stor() = 0;
    virtual void fsmAction_n_stor_f() = 0;
    virtual void fsmAction_addr_stor() = 0;
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

