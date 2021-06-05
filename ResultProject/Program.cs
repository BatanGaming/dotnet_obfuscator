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
            public string OperandName { get; set; }
            public string[] ParametersTypesNames { get; set; }
            public string[] GenericTypesNames { get; set; }
            public string[] DeclaringTypeGenericTypesNames { get; set; }
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
            public string[] GenericTypesNames { get; set; }
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
            {@"TestAssembly.TestClass`2#View", @"eyJJbENvZGUiOiJBQUFQQWY0V0FnQUFHMjhMQUFBS0Npc1ZCbThNQUFBS0N3QUhqQVFBQUJzb0RRQUFDZ0FBQm04T0FBQUtMZVBlQ3dZc0J3WnZEd0FBQ2dEY0tnPT0iLCJJbnN0cnVjdGlvbnMiOlt7Ik9mZnNldCI6MCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0IjoxLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX0seyJPZmZzZXQiOjIsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJEZWNsYXJpbmdUeXBlR2VuZXJpY1R5cGVzTmFtZXMiOm51bGx9fSx7Ik9mZnNldCI6NCwiU2l6ZSI6MiwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjAsIk9wZXJhbmROYW1lIjoiVCIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6W10sIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0IjoxMCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjEsIk9wZXJhbmROYW1lIjoiU3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWMuSUVudW1lcmFibGVcdTAwNjAxI0dldEVudW1lcmF0b3IiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6W10sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpbIlRFbnRpdHkiXX19LHsiT2Zmc2V0IjoxNSwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0IjoxNiwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0IjoxOCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0IjoxOSwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjEsIk9wZXJhbmROYW1lIjoiU3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWMuSUVudW1lcmF0b3JcdTAwNjAxI2dldF9DdXJyZW50IiwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOltdLCJHZW5lcmljVHlwZXNOYW1lcyI6W10sIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6WyJURW50aXR5Il19fSx7Ik9mZnNldCI6MjQsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJEZWNsYXJpbmdUeXBlR2VuZXJpY1R5cGVzTmFtZXMiOm51bGx9fSx7Ik9mZnNldCI6MjUsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJEZWNsYXJpbmdUeXBlR2VuZXJpY1R5cGVzTmFtZXMiOm51bGx9fSx7Ik9mZnNldCI6MjYsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjpudWxsLCJPcGVyYW5kTmFtZSI6bnVsbCwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJEZWNsYXJpbmdUeXBlR2VuZXJpY1R5cGVzTmFtZXMiOm51bGx9fSx7Ik9mZnNldCI6MjcsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjowLCJPcGVyYW5kTmFtZSI6IlRFbnRpdHkiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOltdLCJEZWNsYXJpbmdUeXBlR2VuZXJpY1R5cGVzTmFtZXMiOm51bGx9fSx7Ik9mZnNldCI6MzIsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoxLCJPcGVyYW5kTmFtZSI6IlN5c3RlbS5Db25zb2xlI1dyaXRlTGluZSIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpbIlN5c3RlbS5PYmplY3QiXSwiR2VuZXJpY1R5cGVzTmFtZXMiOltdLCJEZWNsYXJpbmdUeXBlR2VuZXJpY1R5cGVzTmFtZXMiOltdfX0seyJPZmZzZXQiOjM3LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX0seyJPZmZzZXQiOjM4LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX0seyJPZmZzZXQiOjM5LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX0seyJPZmZzZXQiOjQwLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MSwiT3BlcmFuZE5hbWUiOiJTeXN0ZW0uQ29sbGVjdGlvbnMuSUVudW1lcmF0b3IjTW92ZU5leHQiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6W10sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpbXX19LHsiT2Zmc2V0Ijo0NSwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0Ijo0NywiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0Ijo0OSwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0Ijo1MCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0Ijo1MiwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0Ijo1MywiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjEsIk9wZXJhbmROYW1lIjoiU3lzdGVtLklEaXNwb3NhYmxlI0Rpc3Bvc2UiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6W10sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpbXX19LHsiT2Zmc2V0Ijo1OCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0Ijo1OSwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0Ijo2MCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19XSwiTWF4U3RhY2tTaXplIjoxLCJMb2NhbFZhcmlhYmxlcyI6W3siVHlwZU5hbWUiOiJTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYy5JRW51bWVyYXRvclx1MDA2MDEiLCJJc1Bpbm5lZCI6ZmFsc2UsIkdlbmVyaWNUeXBlc05hbWVzIjpbIlRFbnRpdHkiXX0seyJUeXBlTmFtZSI6IlRFbnRpdHkiLCJJc1Bpbm5lZCI6ZmFsc2UsIkdlbmVyaWNUeXBlc05hbWVzIjpbXX1dfQ=="},
{@"TestAssembly.Program#Main", @"eyJJbENvZGUiOiJBSE1SQUFBS0NuSUJBQUJ3S0JJQUFBb0FCaFlmQ2lnVEFBQUtLQUVBQUN0dkZRQUFDZ0J5RVFBQWNDZ1NBQUFLQUNvPSIsIkluc3RydWN0aW9ucyI6W3siT2Zmc2V0IjowLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX0seyJPZmZzZXQiOjEsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoxLCJPcGVyYW5kTmFtZSI6IlRlc3RBc3NlbWJseS5UZXN0Q2xhc3NcdTAwNjAyW1tTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYy5MaXN0XHUwMDYwMVtbU3lzdGVtLkludDMyLCBTeXN0ZW0uUHJpdmF0ZS5Db3JlTGliLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49N2NlYzg1ZDdiZWE3Nzk4ZV1dLCBTeXN0ZW0uUHJpdmF0ZS5Db3JlTGliLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49N2NlYzg1ZDdiZWE3Nzk4ZV0sW1N5c3RlbS5JbnQzMiwgU3lzdGVtLlByaXZhdGUuQ29yZUxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPTdjZWM4NWQ3YmVhNzc5OGVdXSMuY3RvciIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpbXSwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6WyJTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYy5MaXN0XHUwMDYwMVtbU3lzdGVtLkludDMyLCBTeXN0ZW0uUHJpdmF0ZS5Db3JlTGliLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49N2NlYzg1ZDdiZWE3Nzk4ZV1dIiwiU3lzdGVtLkludDMyIl19fSx7Ik9mZnNldCI6NiwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0Ijo3LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MywiT3BlcmFuZE5hbWUiOiJDYWxsaW5nIiwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJEZWNsYXJpbmdUeXBlR2VuZXJpY1R5cGVzTmFtZXMiOm51bGx9fSx7Ik9mZnNldCI6MTIsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoxLCJPcGVyYW5kTmFtZSI6IlN5c3RlbS5Db25zb2xlI1dyaXRlTGluZSIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpbIlN5c3RlbS5TdHJpbmciXSwiR2VuZXJpY1R5cGVzTmFtZXMiOltdLCJEZWNsYXJpbmdUeXBlR2VuZXJpY1R5cGVzTmFtZXMiOltdfX0seyJPZmZzZXQiOjE3LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX0seyJPZmZzZXQiOjE4LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX0seyJPZmZzZXQiOjE5LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX0seyJPZmZzZXQiOjIwLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX0seyJPZmZzZXQiOjIyLCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6MSwiT3BlcmFuZE5hbWUiOiJTeXN0ZW0uTGlucS5FbnVtZXJhYmxlI1JhbmdlIiwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOlsiU3lzdGVtLkludDMyIiwiU3lzdGVtLkludDMyIl0sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpbXX19LHsiT2Zmc2V0IjoyNywiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjEsIk9wZXJhbmROYW1lIjoiU3lzdGVtLkxpbnEuRW51bWVyYWJsZSNUb0xpc3QiLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6WyJTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYy5JRW51bWVyYWJsZVx1MDA2MDFbW1N5c3RlbS5JbnQzMiwgU3lzdGVtLlByaXZhdGUuQ29yZUxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPTdjZWM4NWQ3YmVhNzc5OGVdXSJdLCJHZW5lcmljVHlwZXNOYW1lcyI6WyJTeXN0ZW0uSW50MzIiXSwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpbXX19LHsiT2Zmc2V0IjozMiwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjEsIk9wZXJhbmROYW1lIjoiVGVzdEFzc2VtYmx5LlRlc3RDbGFzc1x1MDA2MDJbW1N5c3RlbS5Db2xsZWN0aW9ucy5HZW5lcmljLkxpc3RcdTAwNjAxW1tTeXN0ZW0uSW50MzIsIFN5c3RlbS5Qcml2YXRlLkNvcmVMaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj03Y2VjODVkN2JlYTc3OThlXV0sIFN5c3RlbS5Qcml2YXRlLkNvcmVMaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj03Y2VjODVkN2JlYTc3OThlXSxbU3lzdGVtLkludDMyLCBTeXN0ZW0uUHJpdmF0ZS5Db3JlTGliLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49N2NlYzg1ZDdiZWE3Nzk4ZV1dI1ZpZXciLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6WyJTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYy5MaXN0XHUwMDYwMVtbU3lzdGVtLkludDMyLCBTeXN0ZW0uUHJpdmF0ZS5Db3JlTGliLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49N2NlYzg1ZDdiZWE3Nzk4ZV1dIl0sIkdlbmVyaWNUeXBlc05hbWVzIjpbXSwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpbIlN5c3RlbS5Db2xsZWN0aW9ucy5HZW5lcmljLkxpc3RcdTAwNjAxW1tTeXN0ZW0uSW50MzIsIFN5c3RlbS5Qcml2YXRlLkNvcmVMaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj03Y2VjODVkN2JlYTc3OThlXV0iLCJTeXN0ZW0uSW50MzIiXX19LHsiT2Zmc2V0IjozNywiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOm51bGwsIk9wZXJhbmROYW1lIjpudWxsLCJQYXJhbWV0ZXJzVHlwZXNOYW1lcyI6bnVsbCwiR2VuZXJpY1R5cGVzTmFtZXMiOm51bGwsIkRlY2xhcmluZ1R5cGVHZW5lcmljVHlwZXNOYW1lcyI6bnVsbH19LHsiT2Zmc2V0IjozOCwiU2l6ZSI6MSwiT3BlcmFuZEluZm8iOnsiT3BlcmFuZFR5cGUiOjMsIk9wZXJhbmROYW1lIjoiQ2FsbGVkIiwiUGFyYW1ldGVyc1R5cGVzTmFtZXMiOm51bGwsIkdlbmVyaWNUeXBlc05hbWVzIjpudWxsLCJEZWNsYXJpbmdUeXBlR2VuZXJpY1R5cGVzTmFtZXMiOm51bGx9fSx7Ik9mZnNldCI6NDMsIlNpemUiOjEsIk9wZXJhbmRJbmZvIjp7Ik9wZXJhbmRUeXBlIjoxLCJPcGVyYW5kTmFtZSI6IlN5c3RlbS5Db25zb2xlI1dyaXRlTGluZSIsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpbIlN5c3RlbS5TdHJpbmciXSwiR2VuZXJpY1R5cGVzTmFtZXMiOltdLCJEZWNsYXJpbmdUeXBlR2VuZXJpY1R5cGVzTmFtZXMiOltdfX0seyJPZmZzZXQiOjQ4LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX0seyJPZmZzZXQiOjQ5LCJTaXplIjoxLCJPcGVyYW5kSW5mbyI6eyJPcGVyYW5kVHlwZSI6bnVsbCwiT3BlcmFuZE5hbWUiOm51bGwsIlBhcmFtZXRlcnNUeXBlc05hbWVzIjpudWxsLCJHZW5lcmljVHlwZXNOYW1lcyI6bnVsbCwiRGVjbGFyaW5nVHlwZUdlbmVyaWNUeXBlc05hbWVzIjpudWxsfX1dLCJNYXhTdGFja1NpemUiOjMsIkxvY2FsVmFyaWFibGVzIjpbeyJUeXBlTmFtZSI6IlRlc3RBc3NlbWJseS5UZXN0Q2xhc3NcdTAwNjAyW1tTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYy5MaXN0XHUwMDYwMVtbU3lzdGVtLkludDMyLCBTeXN0ZW0uUHJpdmF0ZS5Db3JlTGliLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49N2NlYzg1ZDdiZWE3Nzk4ZV1dLCBTeXN0ZW0uUHJpdmF0ZS5Db3JlTGliLCBWZXJzaW9uPTQuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49N2NlYzg1ZDdiZWE3Nzk4ZV0sW1N5c3RlbS5JbnQzMiwgU3lzdGVtLlByaXZhdGUuQ29yZUxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPTdjZWM4NWQ3YmVhNzc5OGVdXSIsIklzUGlubmVkIjpmYWxzZSwiR2VuZXJpY1R5cGVzTmFtZXMiOlsiU3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWMuTGlzdFx1MDA2MDFbW1N5c3RlbS5JbnQzMiwgU3lzdGVtLlByaXZhdGUuQ29yZUxpYiwgVmVyc2lvbj00LjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPTdjZWM4NWQ3YmVhNzc5OGVdXSIsIlN5c3RlbS5JbnQzMiJdfV19"},

        };

        private static readonly List<string> _referencedAssemblies = new List<string> {
            "System.Runtime",
"System.Collections",
"System.Console",
"System.Linq",

        };
        
        private static Type GetTypeByName(string name) {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                var foundedType = assembly.GetType(name);
                if (foundedType != null) {
                    return foundedType;
                }
            }
            return null;
        }

        private static MethodBase GetMethodByName(string name, IReadOnlyCollection<string> parametersNames, IReadOnlyDictionary<string, Type> genericTypes, IEnumerable<string> genericArguments) {
            var index = name.LastIndexOf('#');
            var methodName = name.Substring(index + 1);
            var cachedName = $"{name}({string.Join(',', parametersNames)})";
            var typeName = name.Remove(index);
            var type = GetTypeByName(typeName);
            if (type.IsGenericTypeDefinition) {
                type = type.MakeGenericType(genericArguments.Select(a => genericTypes[a]).ToArray());
            }
            var parametersTypes = parametersNames.Count != 0
                ? (from parameter in parametersNames select GetTypeByName(parameter)).ToArray()
                : Type.EmptyTypes;
            MethodBase method;
            if (methodName.Contains("ctor")) {
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | (methodName == ".ctor" ? BindingFlags.Instance : BindingFlags.Static);
                method = type.GetConstructor(bindingFlags, null, parametersTypes, null);
            }
            else {
                method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic, null, parametersTypes, null);
            }
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

        private static FieldInfo GetFieldByName(string name, IReadOnlyDictionary<string, Type> genericTypes, IEnumerable<string> genericArguments) {
            var index = name.LastIndexOf('#');
            var fieldName = name.Substring(index + 1);
            var typeName = name.Remove(index);
            var type = GetTypeByName(typeName);
            if (type.IsGenericTypeDefinition) {
                type = type.MakeGenericType(genericArguments.Select(t => genericTypes[t]).ToArray());
            }
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static void OverwriteTokens(SerializableMethodBody body, DynamicILInfo ilInfo, IReadOnlyDictionary<string, Type> genericTypes = null) {
            foreach (var instruction in body.Instructions.Where(instruction => instruction.OperandInfo.OperandType != null)) {
                int token = 0;
                switch (instruction.OperandInfo.OperandType) {
                    case OperandTypeInfo.Method:
                    {
                        MethodBase method;
                        if (instruction.OperandInfo.GenericTypesNames != null && instruction.OperandInfo.GenericTypesNames.Length != 0) {
                            if (genericTypes != null) {
                                for (var i = 0; i < instruction.OperandInfo.GenericTypesNames.Length; ++i) {
                                    if (genericTypes.ContainsKey(instruction.OperandInfo.GenericTypesNames[i])) {
                                        instruction.OperandInfo.GenericTypesNames[i] = genericTypes[instruction.OperandInfo.GenericTypesNames[i]].FullName;
                                    }
                                }
                            }
                            method = GetGenericMethod(instruction.OperandInfo);
                        }
                        else {
                            method = GetMethodByName(instruction.OperandInfo.OperandName,
                            instruction.OperandInfo.ParametersTypesNames, genericTypes, instruction.OperandInfo.DeclaringTypeGenericTypesNames);
                        }
                        token = method.DeclaringType.IsGenericType
                            ? ilInfo.GetTokenFor(method.MethodHandle, method.DeclaringType.TypeHandle)
                            : ilInfo.GetTokenFor(method.MethodHandle);
                        break;
                    }
                    case OperandTypeInfo.Field:
                    {
                        var field = GetFieldByName(instruction.OperandInfo.OperandName, genericTypes, instruction.OperandInfo.DeclaringTypeGenericTypesNames);
                        token = field.DeclaringType.IsGenericType
                            ? ilInfo.GetTokenFor(field.FieldHandle, field.DeclaringType.TypeHandle)
                            : ilInfo.GetTokenFor(field.FieldHandle);
                        break;
                    }
                    case OperandTypeInfo.String:
                    {
                        token = ilInfo.GetTokenFor(instruction.OperandInfo.OperandName);
                        break;
                    }
                    case OperandTypeInfo.Type:
                    {
                        if (genericTypes != null) {
                            if (genericTypes.ContainsKey(instruction.OperandInfo.OperandName)) {
                                token = ilInfo.GetTokenFor(genericTypes[instruction.OperandInfo.OperandName].TypeHandle);
                                break;
                            }
                        }
                        token = ilInfo.GetTokenFor(GetTypeByName(instruction.OperandInfo.OperandName).TypeHandle);
                        break;
                    }
                }
                OverwriteInt32(token, (int)instruction.Offset + instruction.Size, body.IlCode);
            }
        }
        
        private static void OverwriteInt32(int value, int pos, IList<byte> array) {
            array[pos++] = (byte) value;
            array[pos++] = (byte) (value >> 8);
            array[pos++] = (byte) (value >> 16);
            array[pos++] = (byte) (value >> 24);
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
            return delegateType.IsGenericTypeDefinition
                ? delegateType.MakeGenericType(genericTypes)
                : delegateType;
        }

        private static Delegate GetGenericMethod(MethodInfo methodInfo, IReadOnlyDictionary<string, Type> genericTypes, object target) {
            var methodName = $"{methodInfo.DeclaringType.FullName}#{methodInfo.Name}";
            var parameters = methodInfo.GetParameters().Select(p =>
                p.ParameterType.IsGenericParameter ? genericTypes[p.ParameterType.Name] : p.ParameterType).ToList();
            var returnType = methodInfo.ReturnType == typeof(void)
                ? null
                : methodInfo.ReturnType.IsGenericParameter
                    ? genericTypes[methodInfo.ReturnType.Name]
                    : methodInfo.ReturnType;
            var delegateType = CloseDelegateType(
                GetDelegateType(parameters.Count, returnType != null),
                (returnType == null ? parameters : parameters.Concat(new[] {returnType})).ToArray()
                );
            var declaringType = methodInfo.DeclaringType.IsGenericTypeDefinition
                ? methodInfo.DeclaringType.MakeGenericType(methodInfo.DeclaringType.GetGenericArguments().Select(t => genericTypes[t.Name]).ToArray())
                : methodInfo.DeclaringType;
            if (!methodInfo.IsStatic) {
                parameters.Insert(0, declaringType);
            }
            var method = new DynamicMethod(
                methodInfo.Name, 
                returnType, 
                parameters.ToArray(),
                target?.GetType() ?? declaringType,
                false
            );
            var encodedMethodBody = _methods[methodName] as string;
            var serializedMethodBody = Encoding.ASCII.GetString(Convert.FromBase64String(encodedMethodBody));
            var methodBody = JsonSerializer.Deserialize<SerializableMethodBody>(serializedMethodBody);
            var ilInfo = method.GetDynamicILInfo();
            
            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in methodBody.LocalVariables) {
                var type = GetTypeByName(local.TypeName) ?? genericTypes[local.TypeName];
                if (type.IsGenericTypeDefinition) {
                    type = type.MakeGenericType(local.GenericTypesNames.Select(a => genericTypes[a]).ToArray());
                }
                localVarSigHelper.AddArgument(type, local.IsPinned);
            }
            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());
            
            OverwriteTokens(methodBody, ilInfo, genericTypes);
            ilInfo.SetCode(methodBody.IlCode, methodBody.MaxStackSize);
            return method.CreateDelegate(delegateType, target);
        }

        public static Delegate GetMethod(MethodInfo methodInfo, Dictionary<string, Type> genericTypes, object target) {
            if (genericTypes != null) {
                return GetGenericMethod(methodInfo, genericTypes, target);
            }
            var methodName = $"{methodInfo.DeclaringType.FullName}#{methodInfo.Name}";
            if (_methods[methodName] is (DynamicMethod method, Type delegateType)) {
                return method.CreateDelegate(delegateType, target);
            }

            var parameters = methodInfo.GetParameters().Select(p => p.ParameterType).ToList();
            var hasReturnType = methodInfo.ReturnType != typeof(void);
            delegateType = CloseDelegateType(
                GetDelegateType(parameters.Count, hasReturnType),
                (hasReturnType
                    ? parameters.Concat(new[] {methodInfo.ReturnType})
                    : parameters).ToArray()
            );
            
            if (!methodInfo.IsStatic) {
                parameters.Insert(0, methodInfo.DeclaringType);
            }
            method = new DynamicMethod(
                methodInfo.Name, 
                methodInfo.ReturnType, 
                parameters.ToArray(),
                target?.GetType() ?? methodInfo.DeclaringType,
                false
            );
            var encodedMethodBody = _methods[methodName] as string;
            var serializedMethodBody = Encoding.ASCII.GetString(Convert.FromBase64String(encodedMethodBody));
            var methodBody = JsonSerializer.Deserialize<SerializableMethodBody>(serializedMethodBody);
            var ilInfo = method.GetDynamicILInfo();
            
            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in methodBody.LocalVariables) {
                var type = GetTypeByName(local.TypeName);
                localVarSigHelper.AddArgument(type, local.IsPinned);
            }
            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());
            
            OverwriteTokens(methodBody, ilInfo);
            ilInfo.SetCode(methodBody.IlCode, methodBody.MaxStackSize);
            _methods[methodName] = (method, delegateType);
            return method.CreateDelegate(delegateType, target);
        }
        
        private static void LoadAssemblies() {
            foreach (var assemblyName in _referencedAssemblies) {
                Assembly.Load(assemblyName);
            }
        }

        public static void Main(string[] args) {
            LoadAssemblies();

            var assembly_name = new AssemblyName("$ASSEMBLY_NAME");
            var assembly_builder = AssemblyBuilder.DefineDynamicAssembly(assembly_name, AssemblyBuilderAccess.Run);
            var module_builder = assembly_builder.DefineDynamicModule(assembly_name.Name + ".dll");
                
            var class_TestClass2_builder = module_builder.DefineType("TestAssembly.TestClass`2", System.Reflection.TypeAttributes.AutoLayout | System.Reflection.TypeAttributes.AnsiClass | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.BeforeFieldInit);
var class_Program_builder = module_builder.DefineType("TestAssembly.Program", System.Reflection.TypeAttributes.AutoLayout | System.Reflection.TypeAttributes.AnsiClass | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Abstract | System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.BeforeFieldInit);

                
            
                
            

            var class_TestClass2_builder_generic_parameters = class_TestClass2_builder.DefineGenericParameters("T","TEntity");
var class_T_builder = class_TestClass2_builder_generic_parameters[0];
var class_TEntity_builder = class_TestClass2_builder_generic_parameters[1];
class_T_builder.SetInterfaceConstraints(typeof(System.Collections.Generic.IEnumerable<>).MakeGenericType(class_TEntity_builder));



            
                
            
                
            
                
            var type_TestClass2_method_ctor_builder = class_TestClass2_builder.DefineConstructor(System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig | System.Reflection.MethodAttributes.SpecialName | System.Reflection.MethodAttributes.RTSpecialName, CallingConventions.Standard | CallingConventions.HasThis, Type.EmptyTypes);

                
            var type_TestClass2_il_method_ctor_builder = type_TestClass2_method_ctor_builder.GetILGenerator();
type_TestClass2_il_method_ctor_builder.Emit(OpCodes.Ldarg_0);
type_TestClass2_il_method_ctor_builder.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
type_TestClass2_il_method_ctor_builder.Emit(OpCodes.Nop);
type_TestClass2_il_method_ctor_builder.Emit(OpCodes.Ret);


                
            
            var type_TestClass2_method_View_builder = class_TestClass2_builder.DefineMethod(
                        "View",
                        System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig
                        );
var type_Program_method_Main_builder = class_Program_builder.DefineMethod(
                        "Main",
                        System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.HideBySig
                        );

                
            
                
            type_TestClass2_method_View_builder.SetParameters(class_T_builder);
type_Program_method_Main_builder.SetParameters(typeof(string[]));

                
            
                
            
                
            
            var type_TestClass2_il_method_View_builder = type_TestClass2_method_View_builder.GetILGenerator();
var delegate_type_TestClass2_View_Void = typeof(Action<>);
var delegate_type_TestClass2_View_Void_closed = delegate_type_TestClass2_View_Void.MakeGenericType(class_T_builder);
type_TestClass2_il_method_View_builder.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetCurrentMethod", Type.EmptyTypes));
type_TestClass2_il_method_View_builder.Emit(OpCodes.Newobj, typeof(Dictionary<string, Type>).GetConstructor(Type.EmptyTypes));
type_TestClass2_il_method_View_builder.Emit(OpCodes.Dup);
type_TestClass2_il_method_View_builder.Emit(OpCodes.Ldstr, "T");
type_TestClass2_il_method_View_builder.Emit(OpCodes.Ldtoken, class_T_builder);
type_TestClass2_il_method_View_builder.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new [] { typeof(RuntimeTypeHandle) }));
type_TestClass2_il_method_View_builder.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Type>).GetMethod("Add"));
type_TestClass2_il_method_View_builder.Emit(OpCodes.Dup);
type_TestClass2_il_method_View_builder.Emit(OpCodes.Ldstr, "TEntity");
type_TestClass2_il_method_View_builder.Emit(OpCodes.Ldtoken, class_TEntity_builder);
type_TestClass2_il_method_View_builder.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new [] { typeof(RuntimeTypeHandle) }));
type_TestClass2_il_method_View_builder.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Type>).GetMethod("Add"));
type_TestClass2_il_method_View_builder.Emit(OpCodes.Ldarg_0);
type_TestClass2_il_method_View_builder.Emit(OpCodes.Call, typeof(Program).GetMethod("GetMethod", new [] {typeof(MethodInfo), typeof(Dictionary<string, Type>), typeof(object)}));
type_TestClass2_il_method_View_builder.Emit(OpCodes.Castclass, delegate_type_TestClass2_View_Void_closed);
type_TestClass2_il_method_View_builder.Emit(OpCodes.Ldarg, 0);
type_TestClass2_il_method_View_builder.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(delegate_type_TestClass2_View_Void_closed, delegate_type_TestClass2_View_Void.GetMethod("Invoke")));
type_TestClass2_il_method_View_builder.Emit(OpCodes.Ret);

var type_Program_il_method_Main_builder = type_Program_method_Main_builder.GetILGenerator();
var delegate_type_Program_Main_Void = typeof(Action<>);
var delegate_type_Program_Main_Void_closed = delegate_type_Program_Main_Void.MakeGenericType(typeof(string[]));
type_Program_il_method_Main_builder.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetCurrentMethod", Type.EmptyTypes));
type_Program_il_method_Main_builder.Emit(OpCodes.Ldnull);
type_Program_il_method_Main_builder.Emit(OpCodes.Ldnull);
type_Program_il_method_Main_builder.Emit(OpCodes.Call, typeof(Program).GetMethod("GetMethod", new [] {typeof(MethodInfo), typeof(Dictionary<string, Type>), typeof(object)}));
type_Program_il_method_Main_builder.Emit(OpCodes.Castclass, delegate_type_Program_Main_Void_closed);
type_Program_il_method_Main_builder.Emit(OpCodes.Ldarg, 0);
type_Program_il_method_Main_builder.Emit(OpCodes.Callvirt, delegate_type_Program_Main_Void_closed.GetMethod("Invoke"));
type_Program_il_method_Main_builder.Emit(OpCodes.Ret);


                

            var class_TestClass2 = class_TestClass2_builder.CreateType();
var class_Program = class_Program_builder.CreateType();
class_Program_builder.GetMethod("Main").Invoke(null, new [] {args});

        }
    }
}
