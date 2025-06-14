/*
 * PROJECT:    Mouri Internal Library Essentials
 * FILE:       Mile.Mobility.Portable.Types.h
 * PURPOSE:    Definition for Mobility Portable Types
 *
 * LICENSE:    The MIT License
 *
 * MAINTAINER: MouriNaruto (Kenji.Mouri@outlook.com)
 */

#ifndef MILE_MOBILITY_PORTABLE_TYPES
#define MILE_MOBILITY_PORTABLE_TYPES

#ifndef CONST
#define CONST const
#endif // !CONST

#ifndef VOID
#define VOID void
#endif // !VOID

#ifndef EXTERN_C
#ifdef __cplusplus
#define EXTERN_C extern "C"
#else
#define EXTERN_C extern
#endif
#endif

#ifdef MILE_MOBILITY_ENABLE_MINIMUM_SAL
#ifndef _In_
#define _In_
#endif // !_In_

#ifndef _In_opt_
#define _In_opt_
#endif // !_In_opt_

#ifndef _Out_
#define _Out_
#endif // !_Out_

#ifndef _Out_opt_
#define _Out_opt_
#endif // !_Out_opt_

#ifndef _Inout_
#define _Inout_
#endif // !_Inout_

#ifndef _Inout_opt_
#define _Inout_opt_
#endif // !_Inout_opt_
#endif // MILE_MOBILITY_ENABLE_MINIMUM_SAL

#ifndef MOAPI
#define MOAPI
#endif // !MOAPI

#ifndef NULL
#ifdef __cplusplus
#define NULL 0
#else
#define NULL ((void*)0)
#endif
#endif

#if defined(__cplusplus) && __cplusplus >= 201103L
#elif defined(_MSVC_LANG) && _MSVC_LANG >= 201103L
#elif defined(__STDC_VERSION__) && __STDC_VERSION__ >= 202311L
#else
#ifndef nullptr
#define nullptr NULL
#endif // !nullptr
#endif

#ifdef _MSC_VER
typedef signed char MO_INT8, *PMO_INT8;
typedef signed short MO_INT16, *PMO_INT16;
typedef signed int MO_INT32, *PMO_INT32;
typedef signed __int64 MO_INT64, *PMO_INT64;

typedef unsigned char MO_UINT8, *PMO_UINT8;
typedef unsigned short MO_UINT16, *PMO_UINT16;
typedef unsigned int MO_UINT32, *PMO_UINT32;
typedef unsigned __int64 MO_UINT64, *PMO_UINT64;
#else
typedef signed char MO_INT8, *PMO_INT8;
typedef signed short MO_INT16, *PMO_INT16;
typedef signed int MO_INT32, *PMO_INT32;
typedef signed long long MO_INT64, *PMO_INT64;

typedef unsigned char MO_UINT8, *PMO_UINT8;
typedef unsigned short MO_UINT16, *PMO_UINT16;
typedef unsigned int MO_UINT32, *PMO_UINT32;
typedef unsigned long long MO_UINT64, *PMO_UINT64;
#endif

#define MO_INT8_MAX 0x7F
#define MO_INT16_MAX 0x7FFF
#define MO_INT32_MAX 0x7FFFFFFF
#define MO_INT64_MAX 0x7FFFFFFFFFFFFFFFLL

#define MO_INT8_MIN (-MO_INT8_MAX - 1)
#define MO_INT16_MIN (-MO_INT16_MAX - 1)
#define MO_INT32_MIN (-MO_INT32_MAX - 1)
#define MO_INT64_MIN (-MO_INT64_MAX - 1)

#define MO_UINT8_MAX 0xFFU
#define MO_UINT16_MAX 0xFFFFU
#define MO_UINT32_MAX 0xFFFFFFFFU
#define MO_UINT64_MAX 0xFFFFFFFFFFFFFFFFULL

#if defined(_WIN64) || defined(_LP64) || defined(__LP64__)
typedef MO_INT64 MO_INTN;
typedef MO_UINT64 MO_UINTN;

#define MO_INTN_MIN MO_INT64_MIN
#define MO_INTN_MAX MO_INT64_MAX
#define MO_UINTN_MAX MO_UINT64_MAX
#else
typedef MO_INT32 MO_INTN;
typedef MO_UINT32 MO_UINTN;

#define MO_INTN_MIN MO_INT32_MIN
#define MO_INTN_MAX MO_INT32_MAX
#define MO_UINTN_MAX MO_UINT32_MAX
#endif

typedef MO_UINT8 MO_BOOL, *PMO_BOOL;
#define MO_FALSE 0
#define MO_TRUE 1

typedef char MO_CHAR, *PMO_CHAR;
typedef CONST char MO_CONSTANT_CHAR, *PMO_CONSTANT_CHAR;
#define MO_CHAR_MIN 0x00
#define MO_CHAR_MAX 0xFF

#ifdef _WCHAR_T_DEFINED
typedef wchar_t MO_WIDE_CHAR, *PMO_WIDE_CHAR;
#else
typedef MO_UINT16 MO_WIDE_CHAR, *PMO_WIDE_CHAR;
#endif
typedef CONST MO_WIDE_CHAR MO_CONSTANT_WIDE_CHAR, *PMO_CONSTANT_WIDE_CHAR;
#define MO_WIDE_CHAR_MIN 0x0000
#define MO_WIDE_CHAR_MAX 0xFFFF

typedef PMO_CHAR MO_STRING, *PMO_STRING;
typedef PMO_WIDE_CHAR MO_WIDE_STRING, *PMO_WIDE_STRING;
typedef PMO_CONSTANT_CHAR MO_CONSTANT_STRING, *PMO_CONSTANT_STRING;
typedef PMO_CONSTANT_WIDE_CHAR MO_CONSTANT_WIDE_STRING;
typedef MO_CONSTANT_WIDE_STRING *PMO_CONSTANT_WIDE_STRING;

typedef VOID *MO_POINTER;
typedef MO_POINTER *PMO_POINTER;
typedef CONST VOID *MO_CONSTANT_POINTER;
typedef MO_CONSTANT_POINTER *PMO_CONSTANT_POINTER;

#ifdef GUID_DEFINED
typedef GUID MO_GUID, *PMO_GUID;
#else
typedef struct _MO_GUID
{
    MO_UINT32 Data1;
    MO_UINT16 Data2;
    MO_UINT16 Data3;
    MO_UINT8 Data4[8];
} MO_GUID, *PMO_GUID;
#endif

#ifndef MO_UNREFERENCED_PARAMETER
#define MO_UNREFERENCED_PARAMETER(P) (P)
#endif // !MO_UNREFERENCED_PARAMETER

#ifndef MO_FIELD_OFFSET
#define MO_FIELD_OFFSET(TYPE, Field) ((MO_UINTN)&(((TYPE*)0)->Field))
#endif // !MO_FIELD_OFFSET

#ifndef MO_FIELD_SIZE
#define MO_FIELD_SIZE(TYPE, Field) (sizeof(((TYPE*)0)->Field))
#endif // !MO_FIELD_SIZE

#ifndef MO_FIELD_SIZE_THROUGH
#define MO_FIELD_SIZE_THROUGH(TYPE, Field) \
    (MO_FIELD_OFFSET(TYPE, Field) + MO_FIELD_SIZE(TYPE, Field))
#endif // !MO_FIELD_SIZE_THROUGH

#ifndef MO_FIELD_CONTAINS
#define MO_FIELD_CONTAINS(Struct, Size, Field) ( \
    (((PMO_UINT8)(&(Struct)->Field)) + sizeof((Struct)->Field)) \
    <= (((PMO_UINT8)(Struct)) + (Size)))
#endif // !MO_FIELD_CONTAINS

#ifndef MO_C_STATIC_ASSERT
#define MO_C_STATIC_ASSERT(Expression) \
    typedef char __MO_C_STATIC_ASSERT__[(Expression) ? 1 : -1]
#endif // !MO_C_STATIC_ASSERT

#ifndef MO_DECLARE_HANDLE
#define MO_DECLARE_HANDLE(Name) \
    struct Name##__{ MO_UINTN Unused; }; \
    typedef struct Name##__ *Name
#endif // !MO_DECLARE_HANDLE

#ifndef MO_DECLSPEC_ALIGN
#ifdef _MSC_VER
#if (_MSC_VER >= 1300) && !defined(MIDL_PASS)
#define MO_DECLSPEC_ALIGN(x) __declspec(align(x))
#else
#define MO_DECLSPEC_ALIGN(x)
#endif
#else
#define MO_DECLSPEC_ALIGN(x) __attribute__ ((aligned(x)))
#endif
#endif // !MO_DECLSPEC_ALIGN

#ifndef MO_ANYSIZE_ARRAY
#define MO_ANYSIZE_ARRAY 1
#endif // !MO_ANYSIZE_ARRAY

#ifndef MO_MAX
#define MO_MAX(Left, Right) (((Left) > (Right)) ? (Left) : (Right))
#endif // !MO_MAX

#ifndef MO_MIN
#define MO_MIN(Left, Right) (((Left) < (Right)) ? (Left) : (Right))
#endif // !MO_MIN

#ifndef MO_ABS
#define MO_ABS(Value) (((Value) < 0) ? (-(Value)) : (Value))
#endif // !MO_ABS

#ifndef MO_ARRAY_SIZE
#define MO_ARRAY_SIZE(Array) (sizeof(Array) / sizeof((Array)[0]))
#endif // !MO_ARRAY_SIZE

#ifndef MO_GET_ALIGNED_SIZE
#define MO_GET_ALIGNED_SIZE(Size, Alignment) \
    (((Size) + (Alignment) - 1) & ~((Alignment) - 1))
#endif // !MO_GET_ALIGNED_SIZE

#ifndef MO_GET_VARIABLE_ADDRESS
#ifdef __cplusplus
#define MO_GET_VARIABLE_ADDRESS(Variable) \
    (&const_cast<MO_CHAR&>(reinterpret_cast<const volatile MO_CHAR&>(Variable)))
#else
#define MO_GET_VARIABLE_ADDRESS(Variable) \
    (&(Variable))
#endif
#endif // !MO_GET_VARIABLE_ADDRESS

#ifdef _MSC_VER /* MSVC */
typedef PMO_CHAR MO_VARIABLE_ARGUMENT_LIST;
#ifndef _MO_VARIABLE_ARGUMENT_ALIGNMENT_ADJUSTMENT
#if defined(_M_ARM_NT) || defined(_M_ARM64) || \
    defined(_M_HYBRID_X86_ARM64) || defined(_M_ARM64EC)
#define _MO_VARIABLE_ARGUMENT_ALIGNMENT_ADJUSTMENT(Marker, TYPE) \
    (((MO_VARIABLE_ARGUMENT_LIST)0 - (Marker)) & (__alignof(TYPE) - 1))
#else
#define _MO_VARIABLE_ARGUMENT_ALIGNMENT_ADJUSTMENT(Marker, TYPE) 0
#endif
#endif // !_MO_VARIABLE_ARGUMENT_ALIGNMENT_ADJUSTMENT
#if defined(_M_AMD64) /* x64 */
EXTERN_C void __cdecl __va_start(MO_VARIABLE_ARGUMENT_LIST*, ...);
#define MO_VARIABLE_ARGUMENT_START(Marker, Parameter) \
    __va_start(&Marker, Parameter)
#define MO_VARIABLE_ARGUMENT_READ(Marker, TYPE) ( \
    ((sizeof(TYPE) > sizeof(MO_UINTN)) || \
    ((sizeof(TYPE) & (sizeof(TYPE) - 1)) != 0)) \
        ? **(TYPE**)((Marker += sizeof(MO_UINTN)) - sizeof(MO_UINTN)) \
        : *(TYPE*)((Marker += sizeof(MO_UINTN)) - sizeof(MO_UINTN)))
#define MO_VARIABLE_ARGUMENT_END(Marker) \
    (Marker = (MO_VARIABLE_ARGUMENT_LIST)0)
#elif defined(_M_ARM_NT) /* ARM32 */
#ifdef __cplusplus
EXTERN_C void __cdecl __va_start(MO_VARIABLE_ARGUMENT_LIST*, ...);
#define MO_VARIABLE_ARGUMENT_START(Marker, Parameter) __va_start( \
    &Marker, \
    MO_GET_VARIABLE_ADDRESS(Parameter), \
    MO_GET_ALIGNED_SIZE(sizeof(Parameter), sizeof(MO_UINTN)), \
    MO_GET_VARIABLE_ADDRESS(Parameter))
#else
#define MO_VARIABLE_ARGUMENT_START(Marker, Parameter) (Marker = \
    ((MO_VARIABLE_ARGUMENT_LIST)MO_GET_VARIABLE_ADDRESS(Parameter)) + \
    MO_GET_ALIGNED_SIZE(sizeof(Parameter), sizeof(MO_UINTN)))
#endif
#define MO_VARIABLE_ARGUMENT_READ(Marker, TYPE) (*(TYPE*)((Marker += \
    MO_GET_ALIGNED_SIZE(sizeof(TYPE), sizeof(MO_UINTN)) + \
    _MO_VARIABLE_ARGUMENT_ALIGNMENT_ADJUSTMENT(Marker, TYPE)) - \
    MO_GET_ALIGNED_SIZE(sizeof(TYPE), sizeof(MO_UINTN))))
#define MO_VARIABLE_ARGUMENT_END(Marker) \
    (Marker = (MO_VARIABLE_ARGUMENT_LIST)0)
#elif defined(_M_ARM64) /* ARM64 */
EXTERN_C void __cdecl __va_start(MO_VARIABLE_ARGUMENT_LIST*, ...);
#define MO_VARIABLE_ARGUMENT_START(Marker, Parameter) __va_start( \
    &Marker, \
    MO_GET_VARIABLE_ADDRESS(Parameter), \
    MO_GET_ALIGNED_SIZE(sizeof(Parameter), sizeof(MO_UINTN)), \
    __alignof(Parameter), \
    MO_GET_VARIABLE_ADDRESS(Parameter))
#define MO_VARIABLE_ARGUMENT_READ(Marker, TYPE) ( \
    (sizeof(TYPE) > (2 * sizeof(__int64))) \
        ? **(TYPE**)((Marker += sizeof(__int64)) - sizeof(__int64)) \
        : *(TYPE*)((Marker += \
            MO_GET_ALIGNED_SIZE(sizeof(TYPE), sizeof(MO_UINTN)) + \
            _MO_VARIABLE_ARGUMENT_ALIGNMENT_ADJUSTMENT(Marker, TYPE)) \
            - MO_GET_ALIGNED_SIZE(sizeof(TYPE), sizeof(MO_UINTN))))
#define MO_VARIABLE_ARGUMENT_END(Marker) \
    (Marker = (MO_VARIABLE_ARGUMENT_LIST)0)
#elif defined(_M_HYBRID_X86_ARM64) /* CHPE */
EXTERN_C void __cdecl __va_start(MO_VARIABLE_ARGUMENT_LIST*, ...);
#define MO_VARIABLE_ARGUMENT_START(Marker, Parameter) __va_start( \
    &Marker, \
    MO_GET_VARIABLE_ADDRESS(Parameter), \
    MO_GET_ALIGNED_SIZE(sizeof(Parameter), sizeof(MO_UINTN)), \
    __alignof(Parameter), \
    MO_GET_VARIABLE_ADDRESS(Parameter))
#define MO_VARIABLE_ARGUMENT_READ(Marker, TYPE) (*(TYPE*)( \
    (Marker += MO_GET_ALIGNED_SIZE(sizeof(TYPE), sizeof(MO_UINTN))) - \
    MO_GET_ALIGNED_SIZE(sizeof(TYPE), sizeof(MO_UINTN))))
#define MO_VARIABLE_ARGUMENT_END(Marker) \
    (Marker = (MO_VARIABLE_ARGUMENT_LIST)0)
#elif defined(_M_ARM64EC) /* ARM64EC */
EXTERN_C void __cdecl __va_start(MO_VARIABLE_ARGUMENT_LIST*, ...);
#define MO_VARIABLE_ARGUMENT_START(Marker, Parameter) __va_start( \
    &Marker, \
    MO_GET_VARIABLE_ADDRESS(Parameter), \
    MO_GET_ALIGNED_SIZE(sizeof(Parameter), sizeof(MO_UINTN)), \
    __alignof(Parameter), \
    MO_GET_VARIABLE_ADDRESS(Parameter))
#define MO_VARIABLE_ARGUMENT_READ(Marker, TYPE) ( \
    ((sizeof(TYPE) > sizeof(MO_UINTN)) || \
    ((sizeof(TYPE) & (sizeof(TYPE) - 1)) != 0)) \
        ? **(TYPE**)((Marker += sizeof(MO_UINTN)) - sizeof(MO_UINTN)) \
        : *(TYPE*)((Marker += \
            sizeof(MO_UINTN) + \
            _MO_VARIABLE_ARGUMENT_ALIGNMENT_ADJUSTMENT(Marker, TYPE)) - \
            sizeof(MO_UINTN)))
#define MO_VARIABLE_ARGUMENT_END(Marker) \
    (Marker = (MO_VARIABLE_ARGUMENT_LIST)0)
#else /* x86 */
#define MO_VARIABLE_ARGUMENT_START(Marker, Parameter) (Marker = \
    ((MO_VARIABLE_ARGUMENT_LIST)MO_GET_VARIABLE_ADDRESS(Parameter)) + \
    MO_GET_ALIGNED_SIZE(sizeof(Parameter), sizeof(MO_UINTN)))
#define MO_VARIABLE_ARGUMENT_READ(Marker, TYPE) (*(TYPE*)( \
    (Marker += MO_GET_ALIGNED_SIZE(sizeof(TYPE), sizeof(MO_UINTN))) - \
    MO_GET_ALIGNED_SIZE(sizeof(TYPE), sizeof(MO_UINTN))))
#define MO_VARIABLE_ARGUMENT_END(Marker) \
    (Marker = (MO_VARIABLE_ARGUMENT_LIST)0)
#endif
#define MO_VARIABLE_ARGUMENT_COPY(Destination, Source) \
    ((VOID)((Destination) = (Source)))
#elif defined(__GNUC__) || defined (__clang__) /* GCC and Clang */
#ifndef MILE_MOBILITY_FORCE_MICROSOFT_ABI
typedef __builtin_ms_va_list MO_VARIABLE_ARGUMENT_LIST;
#define MO_VARIABLE_ARGUMENT_START(Marker, Parameter) \
    __builtin_ms_va_start(Marker, Parameter)
#define MO_VARIABLE_ARGUMENT_READ(Marker, TYPE) ((TYPE)( \
    (sizeof(TYPE) < sizeof(MO_UINTN)) \
        ? __builtin_va_arg(Marker, MO_UINTN) \
        : __builtin_va_arg(Marker, TYPE)))
#define MO_VARIABLE_ARGUMENT_END(Marker) \
    __builtin_ms_va_end(Marker)
#define MO_VARIABLE_ARGUMENT_COPY(Destination, Source) \
    __builtin_ms_va_copy(Destination, Source)
#else
typedef __builtin_va_list MO_VARIABLE_ARGUMENT_LIST;
#define MO_VARIABLE_ARGUMENT_START(Marker, Parameter) \
    __builtin_va_start(Marker, Parameter)
#define MO_VARIABLE_ARGUMENT_READ(Marker, TYPE) ((TYPE)( \
    (sizeof(TYPE) < sizeof(MO_UINTN)) \
        ? __builtin_va_arg(Marker, MO_UINTN) \
        : __builtin_va_arg(Marker, TYPE)))
#define MO_VARIABLE_ARGUMENT_END(Marker) \
    __builtin_va_end(Marker)
#define MO_VARIABLE_ARGUMENT_COPY(Destination, Source) \
    __builtin_va_copy(Destination, Source)
#endif
#else /* Unknown Compilers */
typedef MO_UINT8* MO_VARIABLE_ARGUMENT_LIST;
#define MO_VARIABLE_ARGUMENT_START(Marker, Parameter) (Marker = \
    ((MO_VARIABLE_ARGUMENT_LIST)MO_GET_VARIABLE_ADDRESS(Parameter)) + \
    MO_GET_ALIGNED_SIZE(sizeof(Parameter), sizeof(MO_UINTN)))
#define MO_VARIABLE_ARGUMENT_READ(Marker, TYPE) (*(TYPE*)( \
    (Marker += MO_GET_ALIGNED_SIZE(sizeof(TYPE), sizeof(MO_UINTN))) - \
    MO_GET_ALIGNED_SIZE(sizeof(TYPE), sizeof(MO_UINTN))))
#define MO_VARIABLE_ARGUMENT_END(Marker) \
    (Marker = (MO_VARIABLE_ARGUMENT_LIST)0)
#define MO_VARIABLE_ARGUMENT_COPY(Destination, Source) \
    ((VOID)((Destination) = (Source)))
#endif

#endif // !MILE_MOBILITY_PORTABLE_TYPES
