using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace JFLAPCodeGenerator
{
    public class InputMapSet 
    {
        [XmlAttribute("name")]
        public string Name { get; set; }


        private string _codedValues = null;

        [XmlAttribute("values")]
        public string CodedValues 
        {
            get { return _codedValues; }
            set 
            { 
                _codedValues = value;
                Values = getValues(_codedValues).AsReadOnly();
            } 
        }

        [XmlIgnore]
        public IList<int> Values { get; private set; }

        [XmlElement("addset")]
        public List<string> AddSet { get; set; } // TODO: should invalidate InputMap._resolvedInputMap on set/mod

        [XmlElement("remset")]
        public List<string> RemSet { get; set; } // TODO: should invalidate InputMap._resolvedInputMap on set/mod

        private static List<int> getValues(string s)
        {
            List<int> codes = new List<int>();
            int code = 0;
            bool escMode = false;
            bool numMode = false;
            
            foreach (char c in s)
            {
                if (escMode)
                {
                    if (!codes.Contains((int)c)) codes.Add((int)c);
                    code = 0;
                    escMode = false; // just in case :D
                }
                else
                {
                    switch (c)
                    {
                        case '\\':
                            escMode = true;
                            break;

                        case '#':
                            numMode = true;
                            break;

                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            if (numMode)
                            {
                                code = code * 10 + (c - '0');
                            }
                            else
                            {
                                if (!codes.Contains((int)c)) codes.Add((int)c);
                                code = 0;
                                numMode = false; // just in case :D
                            }
                            break;

                        case ' ':
                        case ',':
                            // reset 
                            if (numMode)
                            {
                                if (!codes.Contains(code)) codes.Add(code);
                            }
                           
                            code = 0;
                            numMode = false;
                            break;

                        //case '-':
                        //    // reset 
                        //    if (numMode)
                        //    {
                        //        rangeStart = code;
                        //    }
                            
                        //    code = 0;
                        //    numMode = false;
                        //    break;

                        default:
                            if (!codes.Contains((int)c)) codes.Add((int)c);
                            code = 0;
                            numMode = false; // just in case :D
                            break;
                    }
                }
            }

            if (numMode)
            {
                if (!codes.Contains(code)) codes.Add(code);
            }
            code = 0;
            numMode = false;

            return codes;
        }
    }

    [XmlRoot("inputmap")]
    public class InputMap
    {
        [XmlAttribute("maxinput")]
        [DefaultValueAttribute(0)]
        public int MaxInput { get; set; }

        [XmlElement("set")]
        public List<InputMapSet> Set { get; set; }

        private Dictionary<string, List<int>> _resolvedInputMap = null;

        private Dictionary<string, List<int>> resolveAbsoluteInputMap()
        {
            Dictionary<string, List<int>> resolved = new Dictionary<string, List<int>>();
            foreach (var s in Set)
            {
                resolved[s.Name] = (s.Values == null) ? new List<int>() : new List<int>(s.Values);
            }

            // sets are just visited in declaration order, no loop/recursion happening
            // could add some code to abort if loop is detected
            foreach (var s in Set)
            {
                foreach (var addSetName in s.AddSet)
                {
                    if (resolved.ContainsKey(addSetName))
                    {
                        var ar = resolved[addSetName];
                        if (ar != null)
                        {
                            resolved[s.Name].AddRange(ar);
                        }
                    }
                    else
                    {
                        Console.WriteLine("{0} specified in add set is not recognized", addSetName);
                    }
                }
                
                foreach (var remSetName in s.RemSet)
                {
                    if (resolved.ContainsKey(remSetName))
                    {
                        var ar = resolved[remSetName];
                        if (ar != null)
                        {
                            resolved[s.Name].RemoveAll(delegate(int item) { return ar.Contains(item); });
                        }
                    }
                    else
                    {
                        Console.WriteLine("{0} specified in add set is not recognized", remSetName);
                    }
                }
            }

            return resolved;
        }

        [XmlIgnore]
        public Dictionary<string,List<int>> ResolvedInputMap
        {
            get
            {
                if (_resolvedInputMap == null)
                {
                    _resolvedInputMap = resolveAbsoluteInputMap();
                }
                return _resolvedInputMap;
            }
        }
    }
}
