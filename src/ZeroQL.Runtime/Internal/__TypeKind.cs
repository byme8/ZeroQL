// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace

namespace ZeroQL.Internal;

// ReSharper disable once UnusedMember.Global
public enum __TypeKind
{
    Interface = 0,
    Object = 1,
    Union = 2,
    InputObject = 4,
    Enum = 8,
    Scalar = 16, // 0x00000010
    List = 32, // 0x00000020
    NonNull = 64, // 0x00000040
    Directive = 128, // 0x00000080
}