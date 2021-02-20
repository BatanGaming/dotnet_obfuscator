﻿using System;

#nullable enable

namespace CodeGen
{
    [Serializable]
    public class OperandInfo
    {
        public OperandTypeInfo? OperandType { get; set; }
        public int? OperandToken { get; set; }
        public string? OperandName { get; set; }
        public string[]? ParametersTypesNames { get; set; }
    }
}