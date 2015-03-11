using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JFLAPCodeGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            bool err = false;

            // initialize serializers
            XmlSerializer inputMapSerializer = new XmlSerializer(typeof(InputMap));
            XmlSerializer ser = new XmlSerializer(typeof(structure));

            // load input map
            InputMap inputMap;
            using (var fs = new FileStream("InputMap.xml", FileMode.Open))
            {
                inputMap = (InputMap)inputMapSerializer.Deserialize(fs);
            }

            // check at least input file is provided as command line parameter
            if (args.Length <= 0)
            {
                Console.WriteLine("Source .jff required.");
                Console.ReadKey(true);
                return;
            }

            // load FSM spec file
            structure objParserDom;
            using (var s = new FileStream(args[0], FileMode.Open))
            {
                objParserDom = (structure)ser.Deserialize(s);
            }

            // get machine name from file name
            string machineName = Generator.makeNiceIdentifier(Path.GetFileNameWithoutExtension(args[0]));

            #region measure input range if not specified (should be modified for sparse input)
            if (inputMap.MaxInput == 0)
            {
                foreach (var t in objParserDom.automaton[0].transition)
                {
                    if (inputMap.ResolvedInputMap.ContainsKey(t.read))
                    {
                        foreach (var v in inputMap.ResolvedInputMap[t.read])
                        {
                            inputMap.MaxInput = Math.Max(inputMap.MaxInput, v);
                        }

                    }
                }
            }
            #endregion // measure input range if not specified

            #region make sure state is encoded with subsequent codes and map new "safe" codes to original
            Dictionary<int, int> stateSafeCodeFromOriginal = new Dictionary<int, int>();
            Dictionary<int, string> stateNameFromSafeCode = new Dictionary<int, string>();
            int initialStateSafeCode = 0;
            int nStates = 0;
            foreach (var state in objParserDom.automaton[0].state)
            {
                if (state.initial != null) // initial is an empty tag that is present or not. Mapping it to a string and checking for nullity is easiest way to check presence
                {
                    initialStateSafeCode = nStates;
                }

                if (!stateSafeCodeFromOriginal.ContainsKey(state.id))
                {
                    stateNameFromSafeCode[nStates] = Generator.makeStateName(state.name);
                    stateSafeCodeFromOriginal[state.id] = nStates;
                    ++nStates;
                }
            }

            stateNameFromSafeCode[nStates] = Generator.makeStateName("error");
            int errorStateCode = nStates;
            ++nStates; // accomodate error state
            #endregion // make sure state is encoded with subsequent codes and map new "safe" codes to original

            #region encode actions and add error action
            Dictionary<int, string> actionNameByCode = new Dictionary<int, string>();
            Dictionary<string, int> actionCodeByName = new Dictionary<string, int>(); // space is not a concern
            int nActions = 0;
            int errorActionCode = 0;
            {
                string actionName = Generator.makeFSMActionName("error"); // "error" is added anyway to init non explicitly set entries, "none" is added if needed
                errorActionCode = nStates;
                actionCodeByName.Add(actionName, errorActionCode);
                actionNameByCode.Add(errorActionCode, actionName);
                ++nActions;

                foreach (var transition in objParserDom.automaton[0].transition)
                {
                    actionName = Generator.makeFSMActionName(transition.transout); // empty string will result in "none" action to be added
                    if (!actionCodeByName.ContainsKey(actionName))
                    {
                        actionCodeByName.Add(actionName, nActions);
                        actionNameByCode.Add(nActions, actionName);
                        ++nActions;
                    }
                }
            }
            #endregion // encode actions and add error action

            #region prepare transitions and actions tables
            // transitions<input, state> = next state
            List<IList<int>> actionsTable = new List<IList<int>>(nStates);

            // actions<input, state> = action
            List<IList<int>> transitionsTable = new List<IList<int>>(nStates);

            // init
            for (int i = 0; i < inputMap.MaxInput; ++i) // MaxInput rows
            {
                var actionRow = new List<int>(nStates);
                var transitionRow = new List<int>(nStates); // nStates columns

                for (int ii = 0; ii < nStates; ++ii)
                {
                    transitionRow.Add(errorStateCode); // init all to error action
                    actionRow.Add(errorActionCode); // init all to error state
                }

                transitionsTable.Add(transitionRow);
                actionsTable.Add(actionRow);
            }

            // sparsely set relevant transitions/actions
            foreach (var t in objParserDom.automaton[0].transition)
            {
                if (inputMap.ResolvedInputMap.ContainsKey(t.read))
                {
                    foreach (var i in inputMap.ResolvedInputMap[t.read])
                    {
                        string currentActionName = Generator.makeFSMActionName(t.transout);
                        if (!string.IsNullOrEmpty(currentActionName))
                        {
                            actionsTable[i][stateSafeCodeFromOriginal[t.from]] = actionCodeByName[currentActionName];
                            transitionsTable[i][stateSafeCodeFromOriginal[t.from]] = stateSafeCodeFromOriginal[t.to];
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error: input set named '{0}' doesn't exist.", t.read);
                    err = true;
                }
            }
            #endregion // prepare transitions and actions tables

            new CPPGenerator().Generate(((args.Length > 1) ? args[1] : null), machineName, initialStateSafeCode, inputMap, stateNameFromSafeCode, actionNameByCode, actionCodeByName, actionsTable, transitionsTable);

            if (err)
            {
                Console.WriteLine("Generation had errors; press a key to continue...");
                Console.ReadKey(true);
            }
        }
    }

    public abstract class Generator
    {
        public abstract void Generate(  string outName,
                                        string machineName,
                                        int initialState,
                                        InputMap inputMap,
                                        IDictionary<int, string> stateNameByCode,
                                        IDictionary<int, string> actionNameByCode,
                                        IDictionary<string, int> actionCodeByName,
                                        IList<IList<int>> actionsTable,
                                        IList<IList<int>> transitionsTable);

        public static int getLongestName(IEnumerable<string> names)
        {
            int maxLen = 0;
            foreach (var name in names) 
            {
                maxLen = Math.Max(maxLen, name.Length);
            }
            return maxLen;
        }

        public static string makeNiceIdentifier(string s)
        {
            return s; // just for now we'll rely on users to behave...
        }

        public static string makeFSMActionName(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                s = "none";
            }
            return "fsmAction_" + makeNiceIdentifier(s);
        }

        public static string makeStateName(string s)
        {
            return makeNiceIdentifier(s); // being used as an enum, for now it shouldn't require more 
        }

    }

    public class CPPGenerator : Generator
    {
        public override void Generate(  string outName,
                                        string machineName,
                                        int initialState,
                                        InputMap inputMap,
                                        IDictionary<int, string> stateNameByCode,
                                        IDictionary<int, string> actionNameByCode,
                                        IDictionary<string, int> actionCodeByName,
                                        IList<IList<int>> actionsTable,
                                        IList<IList<int>> transitionsTable)
        {
            bool textMode = true;

            using (TextWriter w = (string.IsNullOrEmpty(outName) ? Console.Out : new StreamWriter(outName + ".hpp")))
            {
                w.WriteLine("// auto generated with JFLAPCodeGenerator by Giancarlo Todone https://github.com/jean80it/JFLAPCodeGenerator");
                w.WriteLine();
                w.WriteLine("#pragma once");
                w.WriteLine();
                // generate class
                w.WriteLine("class {0}", machineName);
                w.WriteLine("{");
                
                // generate enum for State
                w.WriteLine("    enum class States {");

                foreach (var state in stateNameByCode)
                {
                    w.WriteLine("        {0} = {1},", state.Value, state.Key);
                }
                w.WriteLine("        Count");
                w.WriteLine("    };\n");
                w.WriteLine();
                w.WriteLine("protected:");
                w.WriteLine();
                w.WriteLine("    typedef void({0}::*pActionFn) ();", machineName);
                w.WriteLine();
                w.WriteLine("    States state = States::{0};", stateNameByCode[initialState]);
                w.WriteLine("    States nextState = States::{0};", stateNameByCode[initialState]);
                w.WriteLine();
                w.WriteLine("    static const pActionFn actionsTable[{0}][{1}];", inputMap.MaxInput, stateNameByCode.Count);
                w.WriteLine("    static const {0}::States transitionsTable[{1}][{2}];", machineName, inputMap.MaxInput, stateNameByCode.Count);
                w.WriteLine();
                w.WriteLine("    int currentInput = 0;");
                if (textMode)
                {
                    w.WriteLine("    bool got13 = false;");
                    w.WriteLine("    int line = 0;");
                    w.WriteLine("    int column = 0;");
                }
                w.WriteLine();
                w.WriteLine("#pragma region FSM actions declaration");
                w.WriteLine();
                foreach (var a in actionNameByCode)
                {
                    w.Write("    virtual void " + a.Value + "() = 0;"); w.WriteLine();
                }
                w.WriteLine();
                w.WriteLine("// FSM actions declaration");
                w.WriteLine("#pragma endregion");
                w.WriteLine();
                w.WriteLine("protected:");
                w.WriteLine();
                w.WriteLine(@"	
    bool feed(int c)
	{
		currentInput = c;");
                if (textMode)
                {
                    w.WriteLine(@"	
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

		got13 = false;");
                }
                w.WriteLine(@"
		nextState = transitionsTable[currentInput][(int)state];
		(this->*actionsTable[currentInput][(int)state])();
		state = nextState;

        return (state!=States::error);
	}");

                w.WriteLine();
                w.WriteLine("};");
                w.WriteLine();
            }

            using (TextWriter w = string.IsNullOrEmpty(outName) ? Console.Out : new StreamWriter(outName + ".cpp"))
            {
                // transitions
                {
                    int padLen = getLongestName(stateNameByCode.Values) + machineName.Length + 11;

                    w.WriteLine("// auto generated with JFLAPCodeGenerator by Giancarlo Todone https://github.com/jean80it/JFLAPCodeGenerator");
                    w.WriteLine();
                    w.WriteLine("#include \"{0}.hpp\"", Path.GetFileNameWithoutExtension(outName));
                    w.WriteLine();
                    w.WriteLine("#pragma region FSM state transitions table");
                    w.WriteLine("const {0}::States {0}::transitionsTable[{1}][{2}] = {{", machineName, inputMap.MaxInput, stateNameByCode.Count); // assuming states generated by JFLAP are contiguous

                    w.Write("/*            ");
                    foreach (var sn in stateNameByCode)
                    {
                        w.Write((sn.Value + ", ").PadLeft(padLen)); 
                    }
                    w.WriteLine("*/ ");
                    
                    int rowCount = 0;
                    foreach (var row in transitionsTable)
                    {
                        w.Write(" /* {0,3} {1} */ {{", rowCount, (rowCount >= 32 ? (char)rowCount : ' '));
                        foreach (var cell in row)
                        {
                            w.Write(string.Format("{0}::States::{1}, ", machineName, stateNameByCode[cell]).PadLeft(padLen));
                        }
                        w.Write(" },\n");
                        ++rowCount;
                    }

                    w.WriteLine("};");
                    w.WriteLine("// FSM state transitions table");
                    w.WriteLine("#pragma endregion");
                    w.WriteLine();
                }

                // actions
                {
                    int padLen = getLongestName(actionNameByCode.Values) + machineName.Length + 6;
                    w.WriteLine("#pragma region FSM actions table");
                    w.WriteLine("const {0}::pActionFn {0}::actionsTable[{1}][{2}] = {{", machineName, inputMap.MaxInput, stateNameByCode.Count); // assuming states generated by JFLAP are contiguous

                    w.Write("/*             ");
                    foreach (var sn in stateNameByCode)
                    {
                        w.Write((sn.Value + ", ").PadLeft(padLen));
                    }
                    w.WriteLine("*/ ");

                    int rowCount = 0;
                    foreach (var row in actionsTable)
                    {
                        w.Write(" /* {0,3} {1} */ {{", rowCount, (rowCount >= 32 ? (char)rowCount : ' '));
                        foreach (var cell in row)
                        {
                            w.Write(string.Format("&{0}::{1},", machineName, actionNameByCode[cell]).PadLeft(padLen));
                        }
                        w.Write(" },\n");
                        ++rowCount;
                    }

                    w.WriteLine("};");
                    w.WriteLine("// FSM actions table");
                    w.WriteLine("#pragma endregion");
                    w.WriteLine();
                }
            }
        }
    }
}