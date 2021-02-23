using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;

namespace ResultProject
{
    public static class Program
    {
        [Serializable]
        public class InstructionInfo
        {
            public long Offset { get; set; }
            public int Size { get; set; }
            public OperandInfo OperandInfo { get; set; }
        }

        [Serializable]
        public class OperandInfo
        {
            public OperandTypeInfo? OperandType { get; set; }
            public int? OperandToken { get; set; }
            public string OperandName { get; set; }
            public string[] ParametersTypesNames { get; set; }
            public string[] GenericTypesNames { get; set; }
            public bool? IsExtensionMethod { get; set; }
        }

        [Serializable]
        public enum OperandTypeInfo
        {
            Type,
            Method,
            Field,
            String,
            Signature
        }

        [Serializable]
        public class SerializableLocalVariableInfo
        {
            public string TypeName { get; set; }
            public bool IsPinned { get; set; }
        }

        [Serializable]
        public class SerializableMethodBody
        {
            public byte[] IlCode { get; set; }
            public List<InstructionInfo> Instructions { get; set; }
            public int MaxStackSize { get; set; }
            public List<SerializableLocalVariableInfo> LocalVariables { get; set; }
        }

        private static readonly Dictionary<string, object> _methods = new Dictionary<string, object> {
            {@"TestAssembly.Class1#Factorial", @"eyJJbENvZGUiOiJGeWdOQUFBS0NoZ0xLeEVHQnlnTkFBQUtLQTRBQUFvS0J4ZFlDd2NDTWVzR0tnPT0iLCJJbnN0cnVjdGlvbnMiOlt7Ik9mZnNldCI6MCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmRUb2tlbiI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MSwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjEsIk9wZXJhbmRUb2tlbiI6MTY3NzcyMTczLCJPcGVyYW5kTmFtZSI6IlN5c3RlbS5OdW1lcmljcy5CaWdJbnRlZ2VyI29wX0ltcGxpY2l0IiwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOlsiU3lzdGVtLkludDMyIl0sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiSXNFeHRlbnNpb25NZXRob2QiOmZhbHNlfX0seyJPZmZzZXQiOjYsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjcsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjgsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjksIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOjI4LCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0IjoxMSwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmRUb2tlbiI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MTIsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjEzLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MSwiT3BlcmFuZFRva2VuIjoxNjc3NzIxNzMsIk9wZXJhbmROYW1lIjoiU3lzdGVtLk51bWVyaWNzLkJpZ0ludGVnZXIjb3BfSW1wbGljaXQiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6WyJTeXN0ZW0uSW50MzIiXSwiR2VuZXJpY1R5cGVzTmFtZXMiOltdLCJJc0V4dGVuc2lvbk1ldGhvZCI6ZmFsc2V9fSx7Ik9mZnNldCI6MTgsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoxLCJPcGVyYW5kVG9rZW4iOjE2Nzc3MjE3NCwiT3BlcmFuZE5hbWUiOiJTeXN0ZW0uTnVtZXJpY3MuQmlnSW50ZWdlciNvcF9NdWx0aXBseSIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpbIlN5c3RlbS5OdW1lcmljcy5CaWdJbnRlZ2VyIiwiU3lzdGVtLk51bWVyaWNzLkJpZ0ludGVnZXIiXSwiR2VuZXJpY1R5cGVzTmFtZXMiOltdLCJJc0V4dGVuc2lvbk1ldGhvZCI6ZmFsc2V9fSx7Ik9mZnNldCI6MjMsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjI0LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0IjoyNSwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmRUb2tlbiI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MjYsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjI3LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0IjoyOCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmRUb2tlbiI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MjksIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjMwLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjoxMSwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MzIsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjMzLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19XSwiTWF4U3RhY2tTaXplIjoyLCJMb2NhbFZhcmlhYmxlcyI6W3siVHlwZU5hbWUiOiJTeXN0ZW0uTnVtZXJpY3MuQmlnSW50ZWdlciIsIklzUGlubmVkIjpmYWxzZX0seyJUeXBlTmFtZSI6IlN5c3RlbS5JbnQzMiIsIklzUGlubmVkIjpmYWxzZX1dfQ=="},
{@"TestAssembly.Class1#SumFactorial", @"eyJJbENvZGUiOiJGd01vRHdBQUNoWW9EUUFBQ240RUFBQUVKUzBYSm40REFBQUUvZ1lLQUFBR2N4QUFBQW9sZ0FRQUFBUW9BUUFBS3lvPSIsIkluc3RydWN0aW9ucyI6W3siT2Zmc2V0IjowLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0IjoxLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0IjoyLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MSwiT3BlcmFuZFRva2VuIjoxNjc3NzIxNzUsIk9wZXJhbmROYW1lIjoiU3lzdGVtLkxpbnEuRW51bWVyYWJsZSNSYW5nZSIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpbIlN5c3RlbS5JbnQzMiIsIlN5c3RlbS5JbnQzMiJdLCJHZW5lcmljVHlwZXNOYW1lcyI6W10sIklzRXh0ZW5zaW9uTWV0aG9kIjpmYWxzZX19LHsiT2Zmc2V0Ijo3LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0Ijo4LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MSwiT3BlcmFuZFRva2VuIjoxNjc3NzIxNzMsIk9wZXJhbmROYW1lIjoiU3lzdGVtLk51bWVyaWNzLkJpZ0ludGVnZXIjb3BfSW1wbGljaXQiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6WyJTeXN0ZW0uSW50MzIiXSwiR2VuZXJpY1R5cGVzTmFtZXMiOltdLCJJc0V4dGVuc2lvbk1ldGhvZCI6ZmFsc2V9fSx7Ik9mZnNldCI6MTMsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoyLCJPcGVyYW5kVG9rZW4iOjY3MTA4ODY4LCJPcGVyYW5kTmFtZSI6IlRlc3RBc3NlbWJseS5DbGFzczFcdTAwMkJcdTAwM0NcdTAwM0VjI1x1MDAzQ1x1MDAzRTlfXzNfMCIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MTgsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjE5LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjo0NCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MjEsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjIyLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MiwiT3BlcmFuZFRva2VuIjo2NzEwODg2NywiT3BlcmFuZE5hbWUiOiJUZXN0QXNzZW1ibHkuQ2xhc3MxXHUwMDJCXHUwMDNDXHUwMDNFYyNcdTAwM0NcdTAwM0U5IiwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0IjoyNywiU2l6ZSI6MiwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjEsIk9wZXJhbmRUb2tlbiI6MTAwNjYzMzA2LCJPcGVyYW5kTmFtZSI6IlRlc3RBc3NlbWJseS5DbGFzczFcdTAwMkJcdTAwM0NcdTAwM0VjI1x1MDAzQ1N1bUZhY3RvcmlhbFx1MDAzRWJfXzNfMCIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpbIlN5c3RlbS5OdW1lcmljcy5CaWdJbnRlZ2VyIiwiU3lzdGVtLkludDMyIl0sIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6ZmFsc2V9fSx7Ik9mZnNldCI6MzMsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoxLCJPcGVyYW5kVG9rZW4iOjE2Nzc3MjE3NiwiT3BlcmFuZE5hbWUiOiJTeXN0ZW0uRnVuY1x1MDA2MDNbW1N5c3RlbS5OdW1lcmljcy5CaWdJbnRlZ2VyLCBTeXN0ZW0uUnVudGltZS5OdW1lcmljcywgVmVyc2lvbj00LjEuMi4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWIwM2Y1ZjdmMTFkNTBhM2FdLFtTeXN0ZW0uSW50MzIsIFN5c3RlbS5Qcml2YXRlLkNvcmVMaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj03Y2VjODVkN2JlYTc3OThlXSxbU3lzdGVtLk51bWVyaWNzLkJpZ0ludGVnZXIsIFN5c3RlbS5SdW50aW1lLk51bWVyaWNzLCBWZXJzaW9uPTQuMS4yLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49YjAzZjVmN2YxMWQ1MGEzYV1dIy5jdG9yIiwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOlsiU3lzdGVtLk9iamVjdCIsIlN5c3RlbS5JbnRQdHIiXSwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpmYWxzZX19LHsiT2Zmc2V0IjozOCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmRUb2tlbiI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MzksIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoyLCJPcGVyYW5kVG9rZW4iOjY3MTA4ODY4LCJPcGVyYW5kTmFtZSI6IlRlc3RBc3NlbWJseS5DbGFzczFcdTAwMkJcdTAwM0NcdTAwM0VjI1x1MDAzQ1x1MDAzRTlfXzNfMCIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6NDQsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoxLCJPcGVyYW5kVG9rZW4iOjcyMTQyMDI4OSwiT3BlcmFuZE5hbWUiOiJTeXN0ZW0uTGlucS5FbnVtZXJhYmxlI0FnZ3JlZ2F0ZSIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpbIlN5c3RlbS5Db2xsZWN0aW9ucy5HZW5lcmljLklFbnVtZXJhYmxlXHUwMDYwMVtbU3lzdGVtLkludDMyLCBTeXN0ZW0uUHJpdmF0ZS5Db3JlTGliLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49N2NlYzg1ZDdiZWE3Nzk4ZV1dIiwiU3lzdGVtLk51bWVyaWNzLkJpZ0ludGVnZXIiLCJTeXN0ZW0uRnVuY1x1MDA2MDNbW1N5c3RlbS5OdW1lcmljcy5CaWdJbnRlZ2VyLCBTeXN0ZW0uUnVudGltZS5OdW1lcmljcywgVmVyc2lvbj00LjEuMi4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPWIwM2Y1ZjdmMTFkNTBhM2FdLFtTeXN0ZW0uSW50MzIsIFN5c3RlbS5Qcml2YXRlLkNvcmVMaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj03Y2VjODVkN2JlYTc3OThlXSxbU3lzdGVtLk51bWVyaWNzLkJpZ0ludGVnZXIsIFN5c3RlbS5SdW50aW1lLk51bWVyaWNzLCBWZXJzaW9uPTQuMS4yLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49YjAzZjVmN2YxMWQ1MGEzYV1dIl0sIkdlbmVyaWNUeXBlc05hbWVzIjpbIlN5c3RlbS5JbnQzMiIsIlN5c3RlbS5OdW1lcmljcy5CaWdJbnRlZ2VyIl0sIklzRXh0ZW5zaW9uTWV0aG9kIjp0cnVlfX0seyJPZmZzZXQiOjQ5LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19XSwiTWF4U3RhY2tTaXplIjo4LCJMb2NhbFZhcmlhYmxlcyI6W119"},
{@"TestAssembly.Class1#get", @"eyJJbENvZGUiOiJBbnNCQUFBRUtnPT0iLCJJbnN0cnVjdGlvbnMiOlt7Ik9mZnNldCI6MCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmRUb2tlbiI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MSwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjIsIk9wZXJhbmRUb2tlbiI6NjcxMDg4NjUsIk9wZXJhbmROYW1lIjoiVGVzdEFzc2VtYmx5LkNsYXNzMSNfYSIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6NiwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmRUb2tlbiI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fV0sIk1heFN0YWNrU2l6ZSI6OCwiTG9jYWxWYXJpYWJsZXMiOltdfQ=="},
{@"TestAssembly.Class2#get", @"eyJJbENvZGUiOiJBaWdFQUFBR0Fuc0NBQUFFV0NvPSIsIkluc3RydWN0aW9ucyI6W3siT2Zmc2V0IjowLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0IjoxLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MSwiT3BlcmFuZFRva2VuIjoxMDA2NjMzMDAsIk9wZXJhbmROYW1lIjoiVGVzdEFzc2VtYmx5LkNsYXNzMSNnZXQiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6W10sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiSXNFeHRlbnNpb25NZXRob2QiOmZhbHNlfX0seyJPZmZzZXQiOjYsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kVG9rZW4iOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjcsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoyLCJPcGVyYW5kVG9rZW4iOjY3MTA4ODY2LCJPcGVyYW5kTmFtZSI6IlRlc3RBc3NlbWJseS5DbGFzczIjX2IiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpudWxsfX0seyJPZmZzZXQiOjEyLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0IjoxMywiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmRUb2tlbiI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fV0sIk1heFN0YWNrU2l6ZSI6OCwiTG9jYWxWYXJpYWJsZXMiOltdfQ=="},
{@"TestAssembly.Program#Main", @"eyJJbENvZGUiOiJHQmx6QlFBQUJpVnZCQUFBQmlnU0FBQUtId3B2QXdBQUJvd05BQUFCS0JNQUFBb3EiLCJJbnN0cnVjdGlvbnMiOlt7Ik9mZnNldCI6MCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmRUb2tlbiI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MSwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmRUb2tlbiI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MiwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjEsIk9wZXJhbmRUb2tlbiI6MTAwNjYzMzAxLCJPcGVyYW5kTmFtZSI6IlRlc3RBc3NlbWJseS5DbGFzczIjLmN0b3IiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6WyJTeXN0ZW0uSW50MzIiLCJTeXN0ZW0uSW50MzIiXSwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpmYWxzZX19LHsiT2Zmc2V0Ijo3LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0Ijo4LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MSwiT3BlcmFuZFRva2VuIjoxMDA2NjMzMDAsIk9wZXJhbmROYW1lIjoiVGVzdEFzc2VtYmx5LkNsYXNzMSNnZXQiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6W10sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiSXNFeHRlbnNpb25NZXRob2QiOmZhbHNlfX0seyJPZmZzZXQiOjEzLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MSwiT3BlcmFuZFRva2VuIjoxNjc3NzIxNzgsIk9wZXJhbmROYW1lIjoiU3lzdGVtLkNvbnNvbGUjV3JpdGVMaW5lIiwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOlsiU3lzdGVtLkludDMyIl0sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiSXNFeHRlbnNpb25NZXRob2QiOmZhbHNlfX0seyJPZmZzZXQiOjE4LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjoxMCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MjAsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoxLCJPcGVyYW5kVG9rZW4iOjEwMDY2MzI5OSwiT3BlcmFuZE5hbWUiOiJUZXN0QXNzZW1ibHkuQ2xhc3MxI1N1bUZhY3RvcmlhbCIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpbIlN5c3RlbS5JbnQzMiJdLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOmZhbHNlfX0seyJPZmZzZXQiOjI1LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MCwiT3BlcmFuZFRva2VuIjoxNjc3NzIyOSwiT3BlcmFuZE5hbWUiOiJTeXN0ZW0uTnVtZXJpY3MuQmlnSW50ZWdlciIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiSXNFeHRlbnNpb25NZXRob2QiOm51bGx9fSx7Ik9mZnNldCI6MzAsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoxLCJPcGVyYW5kVG9rZW4iOjE2Nzc3MjE3OSwiT3BlcmFuZE5hbWUiOiJTeXN0ZW0uQ29uc29sZSNXcml0ZUxpbmUiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6WyJTeXN0ZW0uT2JqZWN0Il0sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiSXNFeHRlbnNpb25NZXRob2QiOmZhbHNlfX0seyJPZmZzZXQiOjM1LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19XSwiTWF4U3RhY2tTaXplIjo4LCJMb2NhbFZhcmlhYmxlcyI6W119"},
{@"TestAssembly.Class1+<>c#<SumFactorial>b__3_0", @"eyJJbENvZGUiOiJBd1FvQWdBQUJpZ1VBQUFLS2c9PSIsIkluc3RydWN0aW9ucyI6W3siT2Zmc2V0IjowLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0IjoxLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19LHsiT2Zmc2V0IjoyLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MSwiT3BlcmFuZFRva2VuIjoxMDA2NjMyOTgsIk9wZXJhbmROYW1lIjoiVGVzdEFzc2VtYmx5LkNsYXNzMSNGYWN0b3JpYWwiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6WyJTeXN0ZW0uSW50MzIiXSwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIklzRXh0ZW5zaW9uTWV0aG9kIjpmYWxzZX19LHsiT2Zmc2V0Ijo3LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MSwiT3BlcmFuZFRva2VuIjoxNjc3NzIxODAsIk9wZXJhbmROYW1lIjoiU3lzdGVtLk51bWVyaWNzLkJpZ0ludGVnZXIjb3BfQWRkaXRpb24iLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6WyJTeXN0ZW0uTnVtZXJpY3MuQmlnSW50ZWdlciIsIlN5c3RlbS5OdW1lcmljcy5CaWdJbnRlZ2VyIl0sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiSXNFeHRlbnNpb25NZXRob2QiOmZhbHNlfX0seyJPZmZzZXQiOjEyLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZFRva2VuIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJJc0V4dGVuc2lvbk1ldGhvZCI6bnVsbH19XSwiTWF4U3RhY2tTaXplIjo4LCJMb2NhbFZhcmlhYmxlcyI6W119"},

        };

        private static readonly List<string> _referencedAssemblies = new List<string> {
            "System.Runtime",
"System.Runtime.Numerics",
"System.Linq",
"System.Console",

        };
        private static readonly Dictionary<string, Type> _cachedTypes = new Dictionary<string, Type>();
        private static readonly Dictionary<string, MethodBase> _cachedMethods = new Dictionary<string, MethodBase>();

        private static Type GetTypeByName(string name) {
            if (_cachedTypes.TryGetValue(name, out var type)) {
                return type;
            }
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                var foundedType = assembly.GetType(name);
                if (foundedType != null) {
                    _cachedTypes[name] = foundedType;
                    return foundedType;
                }
            }

            return null;
        }

        private static MethodBase GetMethodByName(string name, IReadOnlyCollection<string> parametersNames) {
            var index = name.LastIndexOf('#');
            var methodName = name.Substring(index + 1);
            var cachedName = $"{methodName}({string.Join(',', parametersNames)})";
            if (_cachedMethods.TryGetValue(cachedName, out var method)) {
                return method;
            }
            var typeName = name.Remove(index);
            var type = GetTypeByName(typeName);
            var parametersTypes = parametersNames.Count != 0
                ? (from parameter in parametersNames select GetTypeByName(parameter)).ToArray()
                : Type.EmptyTypes;
            method = methodName == ".ctor" || methodName == ".cctor"
                ? (MethodBase)type.GetConstructor(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic, null, parametersTypes, null)
                : type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic, null, parametersTypes, null);
            _cachedMethods[cachedName] = method;
            return method;
        }

        private static MethodInfo GetGenericMethod(OperandInfo info) {
            var index = info.OperandName.LastIndexOf('#');
            var methodName = info.OperandName.Substring(index + 1);
            var typeName = info.OperandName.Remove(index);
            var type = GetTypeByName(typeName);
            var genericParametersTypes = info.GenericTypesNames.Select(GetTypeByName).ToArray();
            var parametersTypes = info.ParametersTypesNames.Length != 0
                ? (from parameter in info.ParametersTypesNames select GetTypeByName(parameter)).ToArray()
                : Type.EmptyTypes;
            var possibleMethods = type
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance)
                .Where(m =>
                    m.Name == methodName &&
                    m.IsGenericMethod &&
                    m.GetGenericArguments().Length == genericParametersTypes.Length &&
                    m.GetParameters().Length == parametersTypes.Length
                )
                .ToList();

            return (from method in possibleMethods
                    select method.MakeGenericMethod(genericParametersTypes)
                into exactMethod
                    let parameters = exactMethod.GetParameters()
                    let isGood = !parameters.Where((t, i) => t.ParameterType != parametersTypes[i]).Any()
                    where isGood
                    select exactMethod).FirstOrDefault();
        }

        private static FieldInfo GetFieldByName(string name) {
            var index = name.LastIndexOf('#');
            var fieldName = name.Substring(index + 1);
            var typeName = name.Remove(index);
            var type = GetTypeByName(typeName);
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static void OverwriteTokens(SerializableMethodBody body, DynamicILInfo ilInfo) {
            foreach (var instruction in body.Instructions.Where(instruction => instruction.OperandInfo.OperandType != null)) {
                int token;
                switch (instruction.OperandInfo.OperandType) {
                    case OperandTypeInfo.Method: {
                            MethodBase method;
                            if (instruction.OperandInfo.GenericTypesNames != null && instruction.OperandInfo.GenericTypesNames.Length != 0) {
                                method = GetGenericMethod(instruction.OperandInfo);
                            }
                            else {
                                method = GetMethodByName(instruction.OperandInfo.OperandName,
                                instruction.OperandInfo.ParametersTypesNames);
                            }
                            token = method.DeclaringType.IsGenericType
                                ? ilInfo.GetTokenFor(method.MethodHandle, method.DeclaringType.TypeHandle)
                                : ilInfo.GetTokenFor(method.MethodHandle);
                            break;
                        }
                    case OperandTypeInfo.Field: {
                            var field = GetFieldByName(instruction.OperandInfo.OperandName);
                            token = field.DeclaringType.IsGenericType
                                ? ilInfo.GetTokenFor(field.FieldHandle, field.DeclaringType.TypeHandle)
                                : ilInfo.GetTokenFor(field.FieldHandle);
                            break;
                        }
                    default:
                        token = instruction.OperandInfo.OperandType switch {
                            OperandTypeInfo.String => ilInfo.GetTokenFor(instruction.OperandInfo.OperandName),
                            OperandTypeInfo.Type => ilInfo.GetTokenFor(GetTypeByName(instruction.OperandInfo.OperandName).TypeHandle)
                        };
                        break;
                }
                OverwriteInt32(token, (int)instruction.Offset + instruction.Size, body.IlCode);
            }
        }

        private static void OverwriteInt32(int value, int pos, IList<byte> array) {
            array[pos++] = (byte)value;
            array[pos++] = (byte)(value >> 8);
            array[pos++] = (byte)(value >> 16);
            array[pos++] = (byte)(value >> 24);
        }

        private static Type GetDelegateType(int parametersCount, bool returnValue) {
            return parametersCount switch {
                0 when returnValue => typeof(Func<>),
                0 when !returnValue => typeof(Action),
                1 when returnValue => typeof(Func<,>),
                1 when !returnValue => typeof(Action<>),
                2 when returnValue => typeof(Func<,,>),
                2 when !returnValue => typeof(Action<,>),
                3 when returnValue => typeof(Func<,,,>),
                3 when !returnValue => typeof(Action<,,>),
                4 when returnValue => typeof(Func<,,,,>),
                4 when !returnValue => typeof(Action<,,,>),
                5 when returnValue => typeof(Func<,,,,,>),
                5 when !returnValue => typeof(Action<,,,,>),
                6 when returnValue => typeof(Func<,,,,,,>),
                6 when !returnValue => typeof(Action<,,,,,>),
                7 when returnValue => typeof(Func<,,,,,,,>),
                7 when !returnValue => typeof(Action<,,,,,,>),
                8 when returnValue => typeof(Func<,,,,,,,,>),
                8 when !returnValue => typeof(Action<,,,,,,,>),
                9 when returnValue => typeof(Func<,,,,,,,,,>),
                9 when !returnValue => typeof(Action<,,,,,,,,>),
                10 when returnValue => typeof(Func<,,,,,,,,,,>),
                10 when !returnValue => typeof(Action<,,,,,,,,,>),
                11 when returnValue => typeof(Func<,,,,,,,,,,,>),
                11 when !returnValue => typeof(Action<,,,,,,,,,,>),
                12 when returnValue => typeof(Func<,,,,,,,,,,,,>),
                12 when !returnValue => typeof(Action<,,,,,,,,,,,>),
                13 when returnValue => typeof(Func<,,,,,,,,,,,,,>),
                13 when !returnValue => typeof(Action<,,,,,,,,,,,,>),
                14 when returnValue => typeof(Func<,,,,,,,,,,,,,,>),
                14 when !returnValue => typeof(Action<,,,,,,,,,,,,,>),
                15 when returnValue => typeof(Func<,,,,,,,,,,,,,,,>),
                15 when !returnValue => typeof(Action<,,,,,,,,,,,,,,>),
                16 when returnValue => typeof(Func<,,,,,,,,,,,,,,,,>),
                16 when !returnValue => typeof(Action<,,,,,,,,,,,,,,,>),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static Type CloseDelegateType(Type delegateType, Type[] genericTypes) {
            return delegateType.MakeGenericType(genericTypes);
        }

        private static Delegate GetMethod(string name, MethodInfo methodInfo, object target) {
            if (_methods[name] is (DynamicMethod method, Type delegateType)) {
                return method.CreateDelegate(delegateType, target);
            }
            var parameters = methodInfo.GetParameters().Select(p => p.ParameterType).ToList();
            var hasReturnType = methodInfo.ReturnType != typeof(void);
            delegateType = CloseDelegateType(
                GetDelegateType(parameters.Count, hasReturnType),
                (hasReturnType
                    ? parameters.Concat(new[] { methodInfo.ReturnType })
                    : parameters).ToArray()
            );

            if (!methodInfo.IsStatic) {
                parameters.Insert(0, methodInfo.DeclaringType);
            }
            method = new DynamicMethod(
                methodInfo.Name,
                methodInfo.ReturnType,
                parameters.ToArray(),
                methodInfo.DeclaringType,
                false
            );
            var encodedMethodBody = _methods[name] as string;
            var serializedMethodBody = Encoding.ASCII.GetString(Convert.FromBase64String(encodedMethodBody));
            var methodBody = JsonSerializer.Deserialize<SerializableMethodBody>(serializedMethodBody);
            var ilInfo = method.GetDynamicILInfo();

            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in methodBody.LocalVariables) {
                localVarSigHelper.AddArgument(GetTypeByName(local.TypeName), local.IsPinned);
            }
            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());

            OverwriteTokens(methodBody, ilInfo);
            ilInfo.SetCode(methodBody.IlCode, methodBody.MaxStackSize);
            _methods[name] = (method, delegateType);
            return method.CreateDelegate(delegateType, target);
        }

        private static void LoadAssemblies() {
            foreach (var assemblyName in _referencedAssemblies) {
                Assembly.Load(assemblyName);
            }
        }

        public static Delegate GetMethod(object target) {
            var stackTrace = new StackTrace();
            var caller = stackTrace.GetFrame(1).GetMethod();
            var name = $"{caller.DeclaringType.FullName}#{caller.Name}";
            var callerMethodInfo = GetMethodByName(
                name,
                caller
                    .GetParameters()
                    .Select(p => p.ParameterType.FullName)
                    .ToList()
                );
            return GetMethod(name, callerMethodInfo as MethodInfo, target);
        }

        public static void Main(string[] args) {
            LoadAssemblies();

            var assembly_name = new AssemblyName("$ASSEMBLY_NAME");
            var assembly_builder = AssemblyBuilder.DefineDynamicAssembly(assembly_name, AssemblyBuilderAccess.Run);
            var module_builder = assembly_builder.DefineDynamicModule(assembly_name.Name + ".dll");

            var type_Class1_builder = module_builder.DefineType(
                "TestAssembly.Class1",
                System.Reflection.TypeAttributes.AutoLayout | System.Reflection.TypeAttributes.AnsiClass | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.BeforeFieldInit,
                typeof(object));
            var type_Class2_builder = module_builder.DefineType(
                            "TestAssembly.Class2",
                            System.Reflection.TypeAttributes.AutoLayout | System.Reflection.TypeAttributes.AnsiClass | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.BeforeFieldInit,
                            type_Class1_builder);
            var type_Program_builder = module_builder.DefineType(
                            "TestAssembly.Program",
                            System.Reflection.TypeAttributes.AutoLayout | System.Reflection.TypeAttributes.AnsiClass | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Abstract | System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.BeforeFieldInit,
                            typeof(object));
            var type_c_builder = type_Class1_builder.DefineNestedType(
                            "<>c",
                            System.Reflection.TypeAttributes.AutoLayout | System.Reflection.TypeAttributes.AnsiClass | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.NestedPrivate | System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.Serializable | System.Reflection.TypeAttributes.BeforeFieldInit,
                            typeof(object));



            var type_Class1_field__a_builder = type_Class1_builder.DefineField(
                        "_a",
                        typeof(int),
                        System.Reflection.FieldAttributes.Private
                        );
            var type_Class2_field__b_builder = type_Class2_builder.DefineField(
                                    "_b",
                                    typeof(int),
                                    System.Reflection.FieldAttributes.Private
                                    );
            var type_c_field_9_builder = type_c_builder.DefineField(
                                    "<>9",
                                    type_c_builder,
                                    System.Reflection.FieldAttributes.Public | System.Reflection.FieldAttributes.Static | System.Reflection.FieldAttributes.InitOnly
                                    );
            var type_c_field_9__3_0_builder = type_c_builder.DefineField(
                                    "<>9__3_0",
                                    typeof(System.Func<System.Numerics.BigInteger, int, System.Numerics.BigInteger>),
                                    System.Reflection.FieldAttributes.Public | System.Reflection.FieldAttributes.Static
                                    );



            var type_Class1_method_ctor_builder = type_Class1_builder.DefineConstructor(
                        System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig | System.Reflection.MethodAttributes.SpecialName | System.Reflection.MethodAttributes.RTSpecialName,
                        CallingConventions.Standard | CallingConventions.HasThis,
                        new[] { typeof(int) }
                        );
            var type_Class2_method_ctor_builder = type_Class2_builder.DefineConstructor(
                                    System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig | System.Reflection.MethodAttributes.SpecialName | System.Reflection.MethodAttributes.RTSpecialName,
                                    CallingConventions.Standard | CallingConventions.HasThis,
                                    new[] { typeof(int), typeof(int) }
                                    );
            var type_c_method_cctor_builder = type_c_builder.DefineConstructor(
                                    System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Private | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.HideBySig | System.Reflection.MethodAttributes.SpecialName | System.Reflection.MethodAttributes.RTSpecialName,
                                    CallingConventions.Standard,
                                    Type.EmptyTypes
                                    );
            var type_c_method_ctor_builder = type_c_builder.DefineConstructor(
                                    System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig | System.Reflection.MethodAttributes.SpecialName | System.Reflection.MethodAttributes.RTSpecialName,
                                    CallingConventions.Standard | CallingConventions.HasThis,
                                    Type.EmptyTypes
                                    );


            var type_Class1_il_method_ctor_builder = type_Class1_method_ctor_builder.GetILGenerator();
            type_Class1_il_method_ctor_builder.Emit(OpCodes.Ldarg_0);
            type_Class1_il_method_ctor_builder.Emit(OpCodes.Call, typeof(object).GetConstructor(
                                       Type.EmptyTypes));
            type_Class1_il_method_ctor_builder.Emit(OpCodes.Ldarg_0);
            type_Class1_il_method_ctor_builder.Emit(OpCodes.Ldarg_1);
            type_Class1_il_method_ctor_builder.Emit(OpCodes.Stfld, type_Class1_field__a_builder);
            type_Class1_il_method_ctor_builder.Emit(OpCodes.Ret);

            var type_Class2_il_method_ctor_builder = type_Class2_method_ctor_builder.GetILGenerator();
            type_Class2_il_method_ctor_builder.Emit(OpCodes.Ldarg_0);
            type_Class2_il_method_ctor_builder.Emit(OpCodes.Ldarg_1);
            type_Class2_il_method_ctor_builder.Emit(OpCodes.Call, type_Class1_method_ctor_builder);
            type_Class2_il_method_ctor_builder.Emit(OpCodes.Ldarg_0);
            type_Class2_il_method_ctor_builder.Emit(OpCodes.Ldarg_2);
            type_Class2_il_method_ctor_builder.Emit(OpCodes.Stfld, type_Class2_field__b_builder);
            type_Class2_il_method_ctor_builder.Emit(OpCodes.Ret);

            var type_c_il_method_cctor_builder = type_c_method_cctor_builder.GetILGenerator();
            type_c_il_method_cctor_builder.Emit(OpCodes.Newobj, type_c_method_ctor_builder);
            type_c_il_method_cctor_builder.Emit(OpCodes.Stsfld, type_c_field_9_builder);
            type_c_il_method_cctor_builder.Emit(OpCodes.Ret);

            var type_c_il_method_ctor_builder = type_c_method_ctor_builder.GetILGenerator();
            type_c_il_method_ctor_builder.Emit(OpCodes.Ldarg_0);
            type_c_il_method_ctor_builder.Emit(OpCodes.Call, typeof(object).GetConstructor(
                                       Type.EmptyTypes));
            type_c_il_method_ctor_builder.Emit(OpCodes.Ret);




            var type_Class1_method_Factorial_builder = type_Class1_builder.DefineMethod(
                        "Factorial",
                        System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.HideBySig,
                        typeof(System.Numerics.BigInteger),
                        new[] { typeof(int) }
                        );
            var type_Class1_method_SumFactorial_builder = type_Class1_builder.DefineMethod(
                                    "SumFactorial",
                                    System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig,
                                    typeof(System.Numerics.BigInteger),
                                    new[] { typeof(int) }
                                    );
            var type_Class1_method_get_builder = type_Class1_builder.DefineMethod(
                                    "get",
                                    System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Virtual | System.Reflection.MethodAttributes.HideBySig | System.Reflection.MethodAttributes.VtableLayoutMask,
                                    typeof(int),
                                    Type.EmptyTypes
                                    );
            var type_Class2_method_get_builder = type_Class2_builder.DefineMethod(
                                    "get",
                                    System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Virtual | System.Reflection.MethodAttributes.HideBySig,
                                    typeof(int),
                                    Type.EmptyTypes
                                    );
            var type_Program_method_Main_builder = type_Program_builder.DefineMethod(
                                    "Main",
                                    System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.HideBySig,
                                    typeof(void),
                                    new[] { typeof(string[]) }
                                    );
            var type_c_method_SumFactorialb__3_0_builder = type_c_builder.DefineMethod(
                                    "<SumFactorial>b__3_0",
                                    System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Assembly | System.Reflection.MethodAttributes.HideBySig,
                                    typeof(System.Numerics.BigInteger),
                                    new[] { typeof(System.Numerics.BigInteger), typeof(int) }
                                    );



            var type_Class1_il_method_Factorial_builder = type_Class1_method_Factorial_builder.GetILGenerator();
            var delegate_type_Class1_Factorial_BigInteger = typeof(Func<,>).MakeGenericType(typeof(int), typeof(System.Numerics.BigInteger));
            var type_Class1_Factorial_field = type_Class1_builder.DefineField("type_Class1_Factorial_field", delegate_type_Class1_Factorial_BigInteger, FieldAttributes.Private | FieldAttributes.Static);
            type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ldnull);
            type_Class1_il_method_Factorial_builder.Emit(OpCodes.Call, typeof(Program).GetMethod("GetMethod", new[] { typeof(object) }));
            type_Class1_il_method_Factorial_builder.Emit(OpCodes.Castclass, delegate_type_Class1_Factorial_BigInteger);
            type_Class1_il_method_Factorial_builder.Emit(OpCodes.Stsfld, type_Class1_Factorial_field);
            type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ldsfld, type_Class1_Factorial_field);
            type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ldarg, 0);
            type_Class1_il_method_Factorial_builder.Emit(OpCodes.Callvirt, delegate_type_Class1_Factorial_BigInteger.GetMethod("Invoke"));
            type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ldnull);
            type_Class1_il_method_Factorial_builder.Emit(OpCodes.Stsfld, type_Class1_Factorial_field);
            type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ret);

            var type_Class1_il_method_SumFactorial_builder = type_Class1_method_SumFactorial_builder.GetILGenerator();
            var delegate_type_Class1_SumFactorial_BigInteger = typeof(Func<,>).MakeGenericType(typeof(int), typeof(System.Numerics.BigInteger));
            var type_Class1_SumFactorial_field = type_Class1_builder.DefineField("type_Class1_SumFactorial_field", delegate_type_Class1_SumFactorial_BigInteger, FieldAttributes.Private);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Ldarg_0);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Ldarg_0);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Call, typeof(Program).GetMethod("GetMethod", new[] { typeof(object) }));
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Castclass, delegate_type_Class1_SumFactorial_BigInteger);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Stfld, type_Class1_SumFactorial_field);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Ldarg_0);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Ldfld, type_Class1_SumFactorial_field);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Ldarg, 1);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Callvirt, delegate_type_Class1_SumFactorial_BigInteger.GetMethod("Invoke"));
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Ldarg_0);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Ldnull);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Stfld, type_Class1_SumFactorial_field);
            type_Class1_il_method_SumFactorial_builder.Emit(OpCodes.Ret);

            var type_Class1_il_method_get_builder = type_Class1_method_get_builder.GetILGenerator();
            var delegate_type_Class1_get_Int32 = typeof(Func<>).MakeGenericType(typeof(int));
            var type_Class1_get_field = type_Class1_builder.DefineField("type_Class1_get_field", delegate_type_Class1_get_Int32, FieldAttributes.Private);
            type_Class1_il_method_get_builder.Emit(OpCodes.Ldarg_0);
            type_Class1_il_method_get_builder.Emit(OpCodes.Ldarg_0);
            type_Class1_il_method_get_builder.Emit(OpCodes.Call, typeof(Program).GetMethod("GetMethod", new[] { typeof(object) }));
            type_Class1_il_method_get_builder.Emit(OpCodes.Castclass, delegate_type_Class1_get_Int32);
            type_Class1_il_method_get_builder.Emit(OpCodes.Stfld, type_Class1_get_field);
            type_Class1_il_method_get_builder.Emit(OpCodes.Ldarg_0);
            type_Class1_il_method_get_builder.Emit(OpCodes.Ldfld, type_Class1_get_field);
            type_Class1_il_method_get_builder.Emit(OpCodes.Callvirt, delegate_type_Class1_get_Int32.GetMethod("Invoke"));
            type_Class1_il_method_get_builder.Emit(OpCodes.Ldarg_0);
            type_Class1_il_method_get_builder.Emit(OpCodes.Ldnull);
            type_Class1_il_method_get_builder.Emit(OpCodes.Stfld, type_Class1_get_field);
            type_Class1_il_method_get_builder.Emit(OpCodes.Ret);

            var type_Class2_il_method_get_builder = type_Class2_method_get_builder.GetILGenerator();
            var delegate_type_Class2_get_Int32 = typeof(Func<>).MakeGenericType(typeof(int));
            var type_Class2_get_field = type_Class2_builder.DefineField("type_Class2_get_field", delegate_type_Class2_get_Int32, FieldAttributes.Private);
            type_Class2_il_method_get_builder.Emit(OpCodes.Ldarg_0);
            type_Class2_il_method_get_builder.Emit(OpCodes.Ldarg_0);
            type_Class2_il_method_get_builder.Emit(OpCodes.Call, typeof(Program).GetMethod("GetMethod", new[] { typeof(object) }));
            type_Class2_il_method_get_builder.Emit(OpCodes.Castclass, delegate_type_Class2_get_Int32);
            type_Class2_il_method_get_builder.Emit(OpCodes.Stfld, type_Class2_get_field);
            type_Class2_il_method_get_builder.Emit(OpCodes.Ldarg_0);
            type_Class2_il_method_get_builder.Emit(OpCodes.Ldfld, type_Class2_get_field);
            type_Class2_il_method_get_builder.Emit(OpCodes.Callvirt, delegate_type_Class2_get_Int32.GetMethod("Invoke"));
            type_Class2_il_method_get_builder.Emit(OpCodes.Ldarg_0);
            type_Class2_il_method_get_builder.Emit(OpCodes.Ldnull);
            type_Class2_il_method_get_builder.Emit(OpCodes.Stfld, type_Class2_get_field);
            type_Class2_il_method_get_builder.Emit(OpCodes.Ret);

            var type_Program_il_method_Main_builder = type_Program_method_Main_builder.GetILGenerator();
            var delegate_type_Program_Main_Void = typeof(Action<>).MakeGenericType(typeof(string[]));
            var type_Program_Main_field = type_Program_builder.DefineField("type_Program_Main_field", delegate_type_Program_Main_Void, FieldAttributes.Private | FieldAttributes.Static);
            type_Program_il_method_Main_builder.Emit(OpCodes.Ldnull);
            type_Program_il_method_Main_builder.Emit(OpCodes.Call, typeof(Program).GetMethod("GetMethod", new[] { typeof(object) }));
            type_Program_il_method_Main_builder.Emit(OpCodes.Castclass, delegate_type_Program_Main_Void);
            type_Program_il_method_Main_builder.Emit(OpCodes.Stsfld, type_Program_Main_field);
            type_Program_il_method_Main_builder.Emit(OpCodes.Ldsfld, type_Program_Main_field);
            type_Program_il_method_Main_builder.Emit(OpCodes.Ldarg, 0);
            type_Program_il_method_Main_builder.Emit(OpCodes.Callvirt, delegate_type_Program_Main_Void.GetMethod("Invoke"));
            type_Program_il_method_Main_builder.Emit(OpCodes.Ldnull);
            type_Program_il_method_Main_builder.Emit(OpCodes.Stsfld, type_Program_Main_field);
            type_Program_il_method_Main_builder.Emit(OpCodes.Ret);

            var type_c_il_method_SumFactorialb__3_0_builder = type_c_method_SumFactorialb__3_0_builder.GetILGenerator();
            var delegate_type_c_SumFactorialb__3_0_BigInteger = typeof(Func<,,>).MakeGenericType(typeof(System.Numerics.BigInteger), typeof(int), typeof(System.Numerics.BigInteger));
            var type_c_SumFactorialb__3_0_field = type_c_builder.DefineField("type_c_SumFactorialb__3_0_field", delegate_type_c_SumFactorialb__3_0_BigInteger, FieldAttributes.Private);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Ldarg_0);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Ldarg_0);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Call, typeof(Program).GetMethod("GetMethod", new[] { typeof(object) }));
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Castclass, delegate_type_c_SumFactorialb__3_0_BigInteger);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Stfld, type_c_SumFactorialb__3_0_field);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Ldarg_0);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Ldfld, type_c_SumFactorialb__3_0_field);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Ldarg, 1);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Ldarg, 2);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Callvirt, delegate_type_c_SumFactorialb__3_0_BigInteger.GetMethod("Invoke"));
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Ldarg_0);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Ldnull);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Stfld, type_c_SumFactorialb__3_0_field);
            type_c_il_method_SumFactorialb__3_0_builder.Emit(OpCodes.Ret);




            var type_Class1 = type_Class1_builder.CreateType();
            var type_Class2 = type_Class2_builder.CreateType();
            var type_Program = type_Program_builder.CreateType();
            var type_c = type_c_builder.CreateType();
            type_Program_builder.GetMethod("Main").Invoke(null, new[] { args });

        }
    }
}