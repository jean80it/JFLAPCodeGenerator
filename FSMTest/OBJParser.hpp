class OBJParser
{
    enum class States {
        wait = 0,
        comment = 1,
        v_first_char = 2,
        vt = 3,
        vn = 4,
        vp = 5,
        f = 6,
        v = 7,
        error = 8,
        Count
    };

    typedef void(OBJParser::*pActionFn) ();

    States state = States::wait;
    States nextState = States::wait;

   static const pActionFn actionsTable[128][9];
   static const OBJParser::States transitionsTable[128][9];

#pragma region FSM actions declaration

    void fsmAction_error();
    void fsmAction_none();
    void fsmAction_nextVal();
    void fsmAction_nextFace();

// FSM actions declaration
#pragma endregion
};

